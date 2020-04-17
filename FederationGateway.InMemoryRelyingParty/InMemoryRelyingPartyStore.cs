using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace FederationGateway.Providers.RelyingParties
{
    public class InMemoryRelyingPartyStore : IRelyingPartyStore
    {
        private IList<RelyingParty> _relyingParties;

        public InMemoryRelyingPartyStore(IEnumerable<RelyingParty> relyingParties)
        {
            _relyingParties = relyingParties.ToList();
        }

        public Task<RelyingParty> GetByRealm(string realm)
        {
            return Task.FromResult(_relyingParties.FirstOrDefault(r => r.Realm == realm));
        }

        public Task<IEnumerable<RelyingParty>> GetAll()
        {
            return Task.FromResult(_relyingParties.Select(r => r));
        }

        public Task<RelyingParty> GetById(string id)
        {
            return Task.FromResult(_relyingParties.FirstOrDefault(r => r.Id == id));
        }

        public Task<RelyingParty> Update(RelyingParty relyingParty)
        {
            var existing = _relyingParties.FirstOrDefault(r => r.Id == relyingParty.Id);
            if(existing != null)
            {
                existing.Realm = relyingParty.Realm;
                existing.ReplyUrl = relyingParty.ReplyUrl;
                existing.LogoutUrl = relyingParty.LogoutUrl;
                existing.TokenLifetimeInMinutes = relyingParty.TokenLifetimeInMinutes;
            }

            return Task.FromResult(existing);
        }

        public Task<RelyingParty> Create(RelyingParty relyingParty)
        {
            relyingParty.Id = Guid.NewGuid().ToString();

            _relyingParties.Add(relyingParty);

            return Task.FromResult(relyingParty);
        }

        public Task Delete(string id)
        {
            var existing = _relyingParties.FirstOrDefault(r => r.Id == id);
            if (existing != null)
            {
                _relyingParties.Remove(existing);
            }

            return Task.FromResult(new object());
        }
    }
}
