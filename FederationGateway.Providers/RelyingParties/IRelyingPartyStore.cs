using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FederationGateway.Providers.RelyingParties
{
    public interface IRelyingPartyStore
    {
        Task<RelyingParty> GetByRealm(string realm);

        Task<IEnumerable<RelyingParty>> GetAll();

        Task<RelyingParty> GetById(string id);

        Task<RelyingParty> Update(RelyingParty relyingParty);

        Task<RelyingParty> Create(RelyingParty relyingParty);

        Task Delete(string id);
    }
}
