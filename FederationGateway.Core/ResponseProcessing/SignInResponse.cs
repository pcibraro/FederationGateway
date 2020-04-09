using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core.ResponseProcessing
{
    public class SignInResponse
    {
        public string AppliesTo { get; set; }

        public Saml2SecurityToken Token { get; set; }
    }
}
