﻿using System;
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
            this.Id = id;
            this.Issuer = issuer;
            this.IsSignInMessage = isSignIn;
            this.IsSignOutMessage = !isSignIn;
        }

        public static SamlRequestMessage CreateFromEncodedRequest(string encodedRequest)
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedRequest));

            return ParseFromDecodedRequest(decoded);
        }

        public static SamlRequestMessage CreateFromCompressedRequest(string compressedRequest)
        {
            string decoded = null;

            using (var ms = new MemoryStream(Convert.FromBase64String(compressedRequest)))
            using (var gzip = new DeflateStream(ms, CompressionMode.Decompress))
            using (var sr = new StreamReader(gzip, Encoding.UTF8))
            {
                decoded = sr.ReadToEnd();
            }

            return ParseFromDecodedRequest(decoded);
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