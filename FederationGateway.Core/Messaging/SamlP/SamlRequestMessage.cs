using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.IO.Compression;

namespace FederationGateway.Core.Messaging.SamlP
{
    public class SamlRequestMessage
    {
        public string Issuer { get; private set; }

        public bool IsSignInMessage { get; private set; }

        public bool IsSignOutMessage { get; private set; }

        public string Id { get; private set; }

        public SamlRequestMessage(string id, string issuer, bool isSignIn)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (issuer == null) throw new ArgumentNullException(nameof(issuer));

            this.Id = id;
            this.Issuer = issuer;
            this.IsSignInMessage = isSignIn;
            this.IsSignOutMessage = !isSignIn;
        }


        public static SamlRequestMessage CreateFromEncodedRequest(string encodedRequest)
        {
            if (string.IsNullOrWhiteSpace(encodedRequest)) throw new ArgumentNullException(nameof(encodedRequest));

            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedRequest));

            return ParseFromDecodedRequest(decoded);
        }

        public static SamlRequestMessage CreateFromCompressedRequest(string compressedRequest)
        {
            if (string.IsNullOrWhiteSpace(compressedRequest)) throw new ArgumentNullException(nameof(compressedRequest));

            string decoded = null;

            using (var ms = new MemoryStream(Convert.FromBase64String(compressedRequest)))
            using (var gzip = new DeflateStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(gzip, Encoding.UTF8))
            {
                decoded = sr.ReadToEnd();
            }

            return ParseFromDecodedRequest(decoded);
        }

        public static string CompressRequest(string request)
        {
            if (string.IsNullOrWhiteSpace(request)) throw new ArgumentNullException(nameof(request));

            var inputStream = new MemoryStream(Convert.FromBase64String(request));
            
            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
            {
                inputStream.CopyTo(compressor);
                compressor.Close();

                return Convert.ToBase64String(compressStream.ToArray());
            }
        }

        private static SamlRequestMessage ParseFromDecodedRequest(string decodedRequest)
        {
            var document = XElement.Parse(decodedRequest);

            var isLogout = document.Name.LocalName.Equals("LogoutRequest", StringComparison.InvariantCultureIgnoreCase);
            var issuer = document.Descendants(XName.Get("Issuer", "urn:oasis:names:tc:SAML:2.0:assertion")).FirstOrDefault();
            var id = document.Attribute("ID").Value;

            var message = new SamlRequestMessage(id, issuer?.Value, !isLogout);

            return message;
        }


    }
}
