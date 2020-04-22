using FederationGateway.Core.Configuration;
using FederationGateway.Core.Keys;
using FederationGateway.Core.Messaging.SamlP;
using FederationGateway.Core.Middleware;
using FederationGateway.Core.Profiles;
using FederationGateway.Core.ResponseProcessing;
using FederationGateway.Providers.Keys;
using FederationGateway.Providers.Profiles;
using FederationGateway.Providers.RelyingParties;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FederationGateway.Tests
{
    public class WsFedMiddlewareTests
    {
        private IRelyingPartyStore _relyingPartyStore;
        private IProfileManager _profileManager;
        private IKeyMaterialService _keyManager;
        private ILogger<WsFedMiddleware> _logger;
        private IOptions<FederationGatewayOptions> _options;

        public WsFedMiddlewareTests()
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

            _logger = new NullLogger<WsFedMiddleware>();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            _options = Options.Create(configuration.GetSection("identityServer")
                .Get<FederationGatewayOptions>());
        }

        [Fact]
        public async Task ShouldGenerateWsTrustResponseFromGet()
        {
            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new WsFedMiddleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new Core.Messaging.WsTrust.WsTrustSerializer(),
                _options
                );

            var context = new DefaultHttpContext();

            context.Request.Path = "/wsfed/";
            context.Request.QueryString = new QueryString("?wa=wsignin1.0&wtrealm=urn:test");
            context.Request.Method = "GET";
            context.Response.Body = new MemoryStream();

            context.User = new ClaimsPrincipal(new List<ClaimsIdentity>
                {
                    new ClaimsIdentity(new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, "john foo")
                    }, "federated")
                });

            await middleware.Invoke(context);

            var body = ((MemoryStream)context.Response.Body).ToArray();

            Assert.Equal(200, context.Response.StatusCode);
            Assert.True(body.Length > 0);
        }

        [Fact]
        public async Task ShouldRedirectIfUserNotAuthenticated()
        {

            var serviceProviderMock = new Mock<IServiceProvider>();
            serviceProviderMock
                .Setup(_ => _.GetService(typeof(IAuthenticationService)))
                .Returns(new MyAuthenticationService());

            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new WsFedMiddleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new Core.Messaging.WsTrust.WsTrustSerializer(),
                _options
                );

            var context = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            };

            context.Request.Path = "/wsfed/";
            context.Request.QueryString = new QueryString("?wa=wsignin1.0&wtrealm=urn:test");
            context.Request.Method = "GET";
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            Assert.Equal(301, context.Response.StatusCode);
        }
    }
    
}
