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

        public string MetadataEndpoint { get; set; }

        public string WsFedCookieName { get; set; }

        public string WsFedEndpoint { get; set; }

        public string Saml20CookieName { get; set; }

        public string Saml20Endpoint { get; set; }

        public FederationGatewayOptions()
        {
            MetadataEndpoint = "/metadata";

            WsFedCookieName = "WsFedEndpoints";
            WsFedEndpoint = "/wsfed";

            Saml20CookieName = "Saml20Endpoints";
            Saml20Endpoint = "/saml20";
        }
    }
}
