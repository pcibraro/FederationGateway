using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using FederationGateway.Core.Configuration;
using FederationGateway.Core.Keys;
using FederationGateway.Core.Messaging.Metadata;
using FederationGateway.Core.ResponseProcessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FederationGateway.Controllers
{
    public class MetadataController : Controller
    {
        const string DocumentId = "ebdcbf09-cc15-44a3-93ce-ef2f48418052";

        private readonly ILogger<SignInResponseGenerator> _logger;
        private readonly IKeyMaterialService _keyService;
        private readonly FederationGatewayOptions _options;
        private readonly WsFederationMetadataSerializer _serializer;

        public MetadataController(
            ILogger<SignInResponseGenerator> logger,
            IKeyMaterialService keyService,
            IOptions<FederationGatewayOptions> options,
            WsFederationMetadataSerializer serializer)
        {
            _logger = logger;
            _keyService = keyService;
            _options = options.Value;
            _serializer = serializer;
        }

        public async Task<IActionResult> Index()
        {
            var key = ((await _keyService.GetSigningCredentialsAsync()).Key as X509SecurityKey).Certificate;

            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(new StringWriter(sb)))
            {
                _serializer.Serialize(xmlWriter,
                    key,
                    DocumentId,
                    _options.IssuerName,
                    Url.Action("Index", "Saml20", new { }, "https"),
                    Url.Action("Index", "WsFed", new { }, "https"));
            }

            var xml = sb.ToString();
            var signedXml = SignXml(xml, key);

            return Content(signedXml, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("application/xml"));
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