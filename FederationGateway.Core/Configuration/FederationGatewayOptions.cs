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
    }
}
