using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core.RelyingParties
{
    public class RelyingParty
    {
        public string Realm { get; set; }

        public string ReplyUrl { get; set; }

        public string LogoutUrl { get; set; }

        public int? TokenLifetimeInMinutes { get; set; }
    }
}
