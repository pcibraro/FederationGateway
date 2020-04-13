using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FederationGateway.Core.Configuration
{
    public class FederationGatewayOptions
    {
        public string IssuerName { get; set; }

        public int DefaultNotOnOrAfterInMinutes { get; set; }

        public int DefaultNotBeforeInMinutes { get; set; }

        public Saml20Options Saml { get; set; }

        public WsFederationOptions WsFed { get; set; }
    }

    public class WsFederationOptions
    {
        public string CookieName { get; set; }

        public string Endpoint { get; set; }
    }

    public class Saml20Options
    {
        public string CookieName { get; set; }

        public string Endpoint { get; set; }
    }


}
