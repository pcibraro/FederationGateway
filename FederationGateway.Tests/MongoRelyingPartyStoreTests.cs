using FederationGateway.MongoRelyingParty;
using FederationGateway.Providers.RelyingParties;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FederationGateway.Tests
{
    public class MongoRelyingPartyStoreTests
    {
        private readonly MongoRelyingPartyStore _store;

        public MongoRelyingPartyStoreTests()
        {
            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false)
               .Build();

            var options = new MongoRelyingPartyStoreOptions();
            configuration.GetSection("mongoStore").Bind(options);

            _store = new MongoRelyingPartyStore(options);
        }

        [Fact]
        public async Task ShouldInsertRelyingParty()
        {
            var updated = await _store.Create(new RelyingParty
            {
                Name = "test",
                Realm = "urn:Test",
                ReplyUrl = "http://localhost",
                LogoutUrl = "http://localhost",
                TokenLifetimeInMinutes = 60
            });

            Assert.NotNull(updated.Id);
        }

        [Fact]
        public async Task ShouldUpdateAndGetRelyingParty()
        {
            var inserted = await _store.Create(new RelyingParty
            {
                Name = "test",
                Realm = "urn:Test",
                ReplyUrl = "http://localhost",
                LogoutUrl = "http://localhost",
                TokenLifetimeInMinutes = 60
            });

            inserted.ReplyUrl = "http://localhost/endponint";
            inserted.Realm = "urn:changed";
            inserted.Name = "changed";
            inserted.LogoutUrl = "http://localhost/logout";
            inserted.TokenLifetimeInMinutes = 90;

            await _store.Update(inserted);

            var updated = await _store.GetById(inserted.Id);

            Assert.Equal("http://localhost/endponint", updated.ReplyUrl);
            Assert.Equal("urn:changed", updated.Realm);
            Assert.Equal("changed", updated.Name);
            Assert.Equal("http://localhost/logout", updated.LogoutUrl);
            Assert.Equal(90, updated.TokenLifetimeInMinutes);
        }

        [Fact]
        public async Task ShouldGetRelyingPartyByRealm()
        {
            var inserted = await _store.Create(new RelyingParty
            {
                Name = "test",
                Realm = "urn:Test",
                ReplyUrl = "http://localhost",
                LogoutUrl = "http://localhost",
                TokenLifetimeInMinutes = 60
            });

            var rp = await _store.GetByRealm("urn:Test");

            Assert.Equal(inserted.ReplyUrl, rp.ReplyUrl);
            Assert.Equal(inserted.Realm, rp.Realm);
            Assert.Equal(inserted.Name, rp.Name);
            Assert.Equal(inserted.LogoutUrl, rp.LogoutUrl);
            Assert.Equal(inserted.TokenLifetimeInMinutes, rp.TokenLifetimeInMinutes);
        }
    }
}
