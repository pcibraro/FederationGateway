using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core.Messaging.WsTrust
{
    public class WsTrustRequestSecurityTokenResponse
    {
        public WsTrustLifetime LifeTime { get; set; }

        public Uri AppliesTo { get; set; }

        public Saml2SecurityToken RequestedSecurityToken { get; set; }
    }
}
