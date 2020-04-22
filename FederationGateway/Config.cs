using FederationGateway.Providers.RelyingParties;
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
                        Id = Guid.NewGuid().ToString(),
                        Name = "test1",
                        Realm = "urn:test",
                        ReplyUrl = "https://localhost:44384/wsfed",
                        LogoutUrl = "https://localhost:44384/wsfed"
                    }
                };
            }
        }
    }
}
