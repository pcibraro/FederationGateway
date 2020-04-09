using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core.Messaging.SamlP
{
    public class SamlResponseMessage
    {
        public Saml2SecurityToken Token { get; set; }

        public Uri ReplyTo { get; set; }

        public String Issuer { get; set; }

        public string InResponseTo { get; set; }

        public string ResponseType { get; set; }

        public string Id { get; set; }
    }
}
