using FederationGateway.Core.RelyingParties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FederationGateway
{
    public static class Config
    {
        public static IEnumerable<RelyingParty> RelyingParties
        {
            get
            {
                return new List<RelyingParty>
                {
                    new RelyingParty
                    {
                        Realm = "urn:test",
                        ReplyUrl = "https://localhost",
                        LogoutUrl = "https://localhost"
                    }
                };
            }
        }
    }
}
