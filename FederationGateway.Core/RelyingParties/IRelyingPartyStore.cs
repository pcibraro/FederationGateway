using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FederationGateway.Core.RelyingParties
{
    public interface IRelyingPartyStore
    {
        Task<RelyingParty> FindRelyingPartyByRealm(string realm);
    }
}
