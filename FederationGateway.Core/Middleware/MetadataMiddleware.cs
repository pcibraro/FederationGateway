using FederationGateway.Core.Configuration;
using FederationGateway.Core.Keys;
using FederationGateway.Core.Messaging.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FederationGateway.Core.Middleware
{
    public class MetadataMiddleware
    {
        const string DocumentId = "ebdcbf09-cc15-44a3-93ce-ef2f48418052";

        private readonly RequestDelegate _next;
        private readonly ILogger<MetadataMiddleware> _logger;
        private readonly IKeyMaterialService _keyService;
        private readonly FederationGatewayOptions _options;
        private readonly WsFederationMetadataSerializer _serializer;

        public MetadataMiddleware(RequestDelegate next,
            ILogger<MetadataMiddleware> logger,
            IKeyMaterialService keyService,
            IOptions<FederationGatewayOptions> options,
            WsFederationMetadataSerializer serializer)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (keyService == null) throw new ArgumentNullException(nameof(keyService));
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));

            _next = next;
            _logger = logger;
            _keyService = keyService;
            _options = options.Value;
            _serializer = serializer;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(_options.MetadataEndpoint, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation("Received metadata request");

                var samlSegment = _options.Saml20Endpoint;
                var wsFedSegment = _options.WsFedEndpoint;

                var samlUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{samlSegment}/";
                var wsFedUrl = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{wsFedSegment}/";

                _logger.LogInformation($"Using Saml Url {samlUrl} in Metadata request");
                _logger.LogInformation($"Using WsFed Url {wsFedUrl} in Metadata request");

                var key = ((await _keyService.GetSigningCredentialsAsync()).Key as X509SecurityKey).Certificate;

                _logger.LogInformation($"Using Certificate Thumbprint {key.Thumbprint} in Metadata request");

                var sb = new StringBuilder();
                using (var xmlWriter = XmlWriter.Create(new StringWriter(sb)))
                {
                    _serializer.Serialize(xmlWriter,
                        key,
                        DocumentId,
                        _options.IssuerName,
                        samlUrl,
                        wsFedUrl);
                }

                var xml = sb.ToString();
                var signedXml = SignXml(xml, key);

                context.Response.ContentType = "application/xml";
                await context.Response.WriteAsync(signedXml);

                _logger.LogInformation("Metadata generated successfully");

                return;
            }

            await _next(context);
        }

        private static string SignXml(string document, X509Certificate2 certificate)
        {
            // Create a new XML document.
            XmlDocument doc = new XmlDocument();

            // Format the document to ignore white spaces.
            doc.PreserveWhitespace = false;

            // Load the passed XML file using it's name.
            doc.LoadXml(document);

            // Create a SignedXml object.
            SignedXml signedXml = new SignedXml(doc);

            // Add the key to the SignedXml document. 
            signedXml.SigningKey = certificate.PrivateKey;
            signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
            // Create a reference to be signed.
            Reference reference = new Reference();
            reference.Uri = "#" + DocumentId;

            // Add an enveloped transformation to the reference.
            XmlDsigEnvelopedSignatureTransform env = new XmlDsigEnvelopedSignatureTransform();
            reference.AddTransform(env);
            reference.AddTransform(new XmlDsigExcC14NTransform());

            // Add the reference to the SignedXml object.
            signedXml.AddReference(reference);

            // Add an RSAKeyValue KeyInfo (optional; helps recipient find key to validate).
            KeyInfo keyInfo = new KeyInfo();
            keyInfo.AddClause(new KeyInfoX509Data(certificate));
            signedXml.KeyInfo = keyInfo;

            // Compute the signature.
            signedXml.ComputeSignature();

            // Get the XML representation of the signature and save 
            // it to an XmlElement object.
            XmlElement xmlDigitalSignature = signedXml.GetXml();

            // Append the element to the XML document.
            doc.DocumentElement.AppendChild(doc.ImportNode(xmlDigitalSignature, true));


            if (doc.FirstChild is XmlDeclaration)
            {
                doc.RemoveChild(doc.FirstChild);
            }

            return doc.OuterXml;
        }
    }
    }
