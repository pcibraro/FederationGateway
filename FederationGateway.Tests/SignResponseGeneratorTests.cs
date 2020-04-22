using FederationGateway.Core.Configuration;
using FederationGateway.Core.Keys;
using FederationGateway.Core.Messaging.WsTrust;
using FederationGateway.Core.Profiles;
using FederationGateway.Core.ResponseProcessing;
using FederationGateway.Providers.Keys;
using FederationGateway.Providers.Profiles;
using FederationGateway.Providers.RelyingParties;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace FederationGateway.Tests
{
    public class SignResponseGeneratorTests
    {
        private IRelyingPartyStore _relyingPartyStore;
        private IProfileManager _profileManager;
        private IKeyMaterialService _keyManager;
        private ILogger<SignInResponseGenerator> _logger;
        private IOptions<FederationGatewayOptions> _options;

        public SignResponseGeneratorTests()
        {
            var certificate = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "federationgateway.pfx"), "identityserver");

            _keyManager = new DefaultKeyMaterialService(certificate);

            _relyingPartyStore = new InMemoryRelyingPartyStore(new List<RelyingParty>
            {
                new RelyingParty
                {
                    Realm = "urn:test",
                    ReplyUrl = "https://localhost",
                    LogoutUrl = "https://localhost"
                }
            });

            _profileManager = new DefaultProfileManager();

            _logger = new NullLogger<SignInResponseGenerator>();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            _options = Options.Create(configuration.GetSection("identityServer")
                .Get<FederationGatewayOptions>());
        }

        [Fact]
        public async Task ShouldGenerateToken()
        {
            var responseGenerator = new SignInResponseGenerator(_logger,
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var response = await responseGenerator.GenerateSignInResponse(new SignInRequest
            {
                Realm = "urn:test",
                User = new ClaimsPrincipal(new List<ClaimsIdentity>
                {
                    new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, "john foo")
                    }, "federated")
                }),
                Parameters = new Dictionary<string, string>()
                {
                }            
            });

            Assert.NotNull(response.Token);
        }

        [Fact]
        public async Task ShouldSerializeToken()
        {
            var responseGenerator = new SignInResponseGenerator(_logger,
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var response = await responseGenerator.GenerateSignInResponse(new SignInRequest
            {
                Realm = "urn:test",
                User = new ClaimsPrincipal(new List<ClaimsIdentity>
                {
                    new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, "john foo")
                    })
                }),
                Parameters = new Dictionary<string, string>()
                {
                }
            });

            var sb = new StringBuilder();
            var xmlWriter = XmlWriter.Create(new StringWriter(sb), new XmlWriterSettings { Encoding = Encoding.UTF8 });
            var serializer = new WsTrustSerializer();

            var wsTrust = new WsTrustRequestSecurityTokenResponse();
            wsTrust.LifeTime = new WsTrustLifetime
            {
                Expires = DateTime.Now.AddHours(8),
                Created = DateTime.Now
            };
            wsTrust.AppliesTo = new Uri("urn:test");
            wsTrust.RequestedSecurityToken = (Saml2SecurityToken)response.Token;

            serializer.Serialize(xmlWriter, wsTrust);

            xmlWriter.Flush();
            
            Assert.True(sb.ToString().Length > 0);
        }

    }
}
