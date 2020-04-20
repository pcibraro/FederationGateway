//using FederationGateway.Core.Configuration;
//using FederationGateway.Core.Keys;
//using FederationGateway.Core.Middleware;
//using FederationGateway.Core.Profiles;
//using FederationGateway.Core.ResponseProcessing;
//using FederationGateway.Providers.Keys;
//using FederationGateway.Providers.Profiles;
//using FederationGateway.Providers.RelyingParties;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Logging.Abstractions;
//using Microsoft.Extensions.Options;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Security.Cryptography.X509Certificates;
//using System.Text;
//using System.Threading.Tasks;
//using Xunit;

//namespace FederationGateway.Tests
//{
//    public class Saml20MiddlewareTests
//    {
//        private IRelyingPartyStore _relyingPartyStore;
//        private IProfileManager _profileManager;
//        private IKeyMaterialService _keyManager;
//        private ILogger<Saml20Middleware> _logger;
//        private IOptions<FederationGatewayOptions> _options;

//        public Saml20MiddlewareTests()
//        {
//            var certificate = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "federationgateway.pfx"), "identityserver");

//            _keyManager = new DefaultKeyMaterialService(certificate);

//            _relyingPartyStore = new InMemoryRelyingPartyStore(new List<RelyingParty>
//            {
//                new RelyingParty
//                {
//                    Realm = "urn:test",
//                    ReplyUrl = "https://localhost",
//                    LogoutUrl = "https://localhost"
//                }
//            });

//            _profileManager = new DefaultProfileManager();

//            _logger = new NullLogger<Saml20Middleware>();

//            var configuration = new ConfigurationBuilder()
//                .SetBasePath(Directory.GetCurrentDirectory())
//                .AddJsonFile("appsettings.json", false)
//                .Build();

//            _options = Options.Create(configuration.GetSection("identityServer")
//                .Get<FederationGatewayOptions>());
//        }

//        [Fact]
//        public async Task ShouldGenerateSAMLResponse()
//        {
//            var responseGenerator = new Saml20Middleware(null,
//                _logger,
//                _relyingPartyStore,
//                new Saml20Ser,
//                _keyManager,
//                _options
//                );

//            var response = await responseGenerator.GenerateSignInResponse(new SignInRequest
//            {
//                Realm = "urn:test",
//                User = new ClaimsPrincipal(new List<ClaimsIdentity>
//                {
//                    new ClaimsIdentity(new List<Claim>
//                    {
//                        new Claim(ClaimTypes.NameIdentifier, "john foo")
//                    })
//                }),
//                Parameters = new Dictionary<string, string>()
//                {
//                }
//            });

//            Assert.NotNull(response.Token);
//        }
//    }
//}
