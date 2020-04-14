using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace FederationGateway.Core.RelyingParties
{
    public class InMemoryRelyingPartyStore : IRelyingPartyStore
    {
        private IEnumerable<RelyingParty> _relyingParties;

        public InMemoryRelyingPartyStore(IEnumerable<RelyingParty> relyingParties)
        {
            _relyingParties = relyingParties;
        }

        public Task<RelyingParty> GetByRealm(string realm)
        {
            return Task.FromResult(_relyingParties.FirstOrDefault(r => r.Realm == realm));
        }

        public Task<IEnumerable<RelyingParty>> GetAll()
        {
            return Task.FromResult(_relyingParties);
        }
    }
}
