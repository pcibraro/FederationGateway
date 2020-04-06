using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core.Messaging.WsTrust
{
    public class WsTrustLifetime
    {
        public DateTime Created { get; set; }

        public DateTime Expires { get; set; }
    }
}
