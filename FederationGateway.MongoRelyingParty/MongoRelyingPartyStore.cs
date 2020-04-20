using FederationGateway.Providers.RelyingParties;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FederationGateway.MongoRelyingParty
{
    public class MongoRelyingPartyStore : IRelyingPartyStore
    {
        private readonly MongoRelyingPartyStoreOptions _options;
        private readonly IMongoCollection<RelyingParty> _relyingParties;

        public MongoRelyingPartyStore(MongoRelyingPartyStoreOptions options)
        {
            _options = options;

            if (!BsonClassMap.IsClassMapRegistered(typeof(RelyingParty)))
            {
                BsonClassMap.RegisterClassMap<RelyingParty>(cm => {
                    cm.AutoMap();
                    cm.GetMemberMap(c => c.Id).SetIgnoreIfDefault(true);
                    cm.SetIdMember(cm.GetMemberMap(c => c.Id));
                });
            }

            var client = new MongoClient(options.ConnectionString);
            var database = client.GetDatabase(options.Database);

            _relyingParties = database.GetCollection<RelyingParty>(options.Collection);
        }

        public async Task<RelyingParty> Create(RelyingParty relyingParty)
        {
            relyingParty.Id = Guid.NewGuid().ToString();

            await _relyingParties.InsertOneAsync(relyingParty);

            return relyingParty;
        }

        public async Task Delete(string id)
        {
            await _relyingParties.DeleteOneAsync(rp => rp.Id == id);
        }

        public async Task<IEnumerable<RelyingParty>> GetAll()
        {
            return (await _relyingParties.FindAsync(rp => true)).ToEnumerable();
        }

        public async Task<RelyingParty> GetById(string id)
        {
            return (await _relyingParties.FindAsync(rp => rp.Id == id)).FirstOrDefault();
        }

        public async Task<RelyingParty> GetByRealm(string realm)
        {
            return (await _relyingParties.FindAsync(rp => rp.Realm.ToLowerInvariant() == realm.ToLowerInvariant())).FirstOrDefault();
        }

        public async Task<RelyingParty> Update(RelyingParty relyingParty)
        {
            await _relyingParties.ReplaceOneAsync(rp => rp.Id == relyingParty.Id, relyingParty);

            return relyingParty;
        }
    }
}
