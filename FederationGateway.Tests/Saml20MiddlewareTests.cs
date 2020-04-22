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
    public class Saml20MiddlewareTests
    {
        private IRelyingPartyStore _relyingPartyStore;
        private IProfileManager _profileManager;
        private IKeyMaterialService _keyManager;
        private ILogger<Saml20Middleware> _logger;
        private IOptions<FederationGatewayOptions> _options;

        public Saml20MiddlewareTests()
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

            _logger = new NullLogger<Saml20Middleware>();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", false)
                .Build();

            _options = Options.Create(configuration.GetSection("identityServer")
                .Get<FederationGatewayOptions>());
        }

        [Fact]
        public async Task ShouldGenerateSAMLResponseFromGet()
        {
            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new Saml20Middleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new SamlResponseSerializer(),
                _options
                );

            var context = new DefaultHttpContext();

            context.Request.Path = "/saml20/";
            context.Request.QueryString = new QueryString("?SAMLRequest=fZJPT8MwDMXvSHyHKPeu3QAJorVogBCT%2BFNthQO3LHXbsDQucTrg25N2IMEBri%2FPfj87np%2B%2Ft4btwJFGm%2FLpJOEMrMJS2zrlj8V1dMrPs8ODOcnWdGLR%2B8au4LUH8ixUWhLjQ8p7ZwVK0iSsbIGEV2K9uLsVs0kiOoceFRrOllcp13W1NWjMdoP4YjaNBm2bra3aWpmy1bKralVvG8XZ0zfWbMBaEvWwtOSl9UFKZkmUHEfJWTE9Eicn4mj2zFn%2BlXSh7X6C%2F7A2exOJm6LIo%2FxhXYwNdroEdx%2FcKa8RawMThe0Qn0sivQtyJQ0BZwsicD4AXqKlvgW3BrfTCh5XtylvvO9IxLFBJU2D5GMfdsazcZFinMX92OD%2FpPI7iWeDbeg0j380yr6%2BZ6BeXuVotPpgC2Pw7dKB9AHZuz4QX6Nrpf87azqZjoouo2q0it5SB0pXGkrO4myf%2BvsOwnV8Ag%3D%3D");
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
        public async Task ShouldGenerateSAMLResponseFromPOST()
        {
            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new Saml20Middleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new SamlResponseSerializer(),
                _options
                );

            var context = new DefaultHttpContext();

            var requestBody = new Dictionary<string, StringValues>();
            requestBody.Add("SAMLRequest", new StringValues("PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4NCjxzYW1scDpBdXRoblJlcXVlc3QgeG1sbnM6c2FtbHA9InVybjpvYXNpczpuYW1lczp0YzpTQU1MOjIuMDpwcm90b2NvbCIgSUQ9ImlnZmtsb2xsa2Jvb2psYmhpZWluaGtuZm1nY2xkbWlhcGZnY2draGMiIFZlcnNpb249IjIuMCIgSXNzdWVJbnN0YW50PSIyMDIwLTA0LTA5VDEzOjU1OjMyWiIgUHJvdG9jb2xCaW5kaW5nPSJ1cm46b2FzaXM6bmFtZXM6dGM6U0FNTDoyLjA6YmluZGluZ3M6SFRUUC1QT1NUIiBQcm92aWRlck5hbWU9Imdvb2dsZS5jb20iIElzUGFzc2l2ZT0iZmFsc2UiIEFzc2VydGlvbkNvbnN1bWVyU2VydmljZVVSTD0iaHR0cHM6Ly9sb2NhbGhvc3QvdGVzdCI + PHNhbWw6SXNzdWVyIHhtbG5zOnNhbWw9InVybjpvYXNpczpuYW1lczp0YzpTQU1MOjIuMDphc3NlcnRpb24iPnVybjp0ZXN0PC9zYW1sOklzc3Vlcj48c2FtbHA6TmFtZUlEUG9saWN5IEFsbG93Q3JlYXRlPSJ0cnVlIiBGb3JtYXQ9InVybjpvYXNpczpuYW1lczp0YzpTQU1MOjEuMTpuYW1laWQtZm9ybWF0OnVuc3BlY2lmaWVkIiAvPjwvc2FtbHA6QXV0aG5SZXF1ZXN0Pg0K"));

            context.Request.Form =new FormCollection(requestBody);
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.Path = "/saml20/";
            context.Request.Method = "POST";
           
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
        public async Task ShouldGenerateSAMLResponseFromIDPInitiated()
        {
            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new Saml20Middleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new SamlResponseSerializer(),
                _options
                );

            var context = new DefaultHttpContext();

            context.Request.Path = "/saml20/idpinitiated";
            context.Request.QueryString = new QueryString("?realm=urn:test");
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

            var middleware = new Saml20Middleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new SamlResponseSerializer(),
                _options
                );

            var context = new DefaultHttpContext
            {
                RequestServices = serviceProviderMock.Object
            };

            context.Request.Path = "/saml20/";
            context.Request.QueryString = new QueryString("?SAMLRequest=fZJPT8MwDMXvSHyHKPeu3QAJorVogBCT%2BFNthQO3LHXbsDQucTrg25N2IMEBri%2FPfj87np%2B%2Ft4btwJFGm%2FLpJOEMrMJS2zrlj8V1dMrPs8ODOcnWdGLR%2B8au4LUH8ixUWhLjQ8p7ZwVK0iSsbIGEV2K9uLsVs0kiOoceFRrOllcp13W1NWjMdoP4YjaNBm2bra3aWpmy1bKralVvG8XZ0zfWbMBaEvWwtOSl9UFKZkmUHEfJWTE9Eicn4mj2zFn%2BlXSh7X6C%2F7A2exOJm6LIo%2FxhXYwNdroEdx%2FcKa8RawMThe0Qn0sivQtyJQ0BZwsicD4AXqKlvgW3BrfTCh5XtylvvO9IxLFBJU2D5GMfdsazcZFinMX92OD%2FpPI7iWeDbeg0j380yr6%2BZ6BeXuVotPpgC2Pw7dKB9AHZuz4QX6Nrpf87azqZjoouo2q0it5SB0pXGkrO4myf%2BvsOwnV8Ag%3D%3D");
            context.Request.Method = "GET";
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            Assert.Equal(301, context.Response.StatusCode);
        }

        [Fact]
        public async Task ShouldReturnBadRequestWhenInvalidSAMLRequestInGET()
        {
            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new Saml20Middleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new SamlResponseSerializer(),
                _options
                );

            var context = new DefaultHttpContext();

            context.Request.Path = "/saml20/";
            context.Request.Method = "GET";
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
        }

        [Fact]
        public async Task ShouldReturnBadRequestWhenInvalidSAMLRequestInPOST()
        {
            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new Saml20Middleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new SamlResponseSerializer(),
                _options
                );

            var context = new DefaultHttpContext();

            context.Request.Path = "/saml20/";
            context.Request.Method = "POST";
            context.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            Assert.Equal(400, context.Response.StatusCode);
        }

        [Fact]
        public async Task ShouldReturnMethodNotAllowed()
        {
            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new Saml20Middleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new SamlResponseSerializer(),
                _options
                );

            var context = new DefaultHttpContext();

            context.Request.Path = "/saml20/";
            context.Request.Method = "PUT";
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            Assert.Equal(405, context.Response.StatusCode);
        }

        [Fact]
        public async Task ShouldReturnBadRequestFromIDPInitiatedWithNoRealm()
        {
            var responseGenerator = new SignInResponseGenerator(new NullLogger<SignInResponseGenerator>(),
                _relyingPartyStore,
                _profileManager,
                _keyManager,
                _options
                );

            var middleware = new Saml20Middleware(
                next: (innerHttpContext) =>
                {
                    return Task.CompletedTask;
                },
                _logger,
                _relyingPartyStore,
                responseGenerator,
                new SamlResponseSerializer(),
                _options
                );

            var context = new DefaultHttpContext();

            context.Request.Path = "/saml20/idpinitiated";
            context.Request.QueryString = new QueryString("");
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

            Assert.Equal(400, context.Response.StatusCode);
        }
    }

    class MyAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string scheme)
        {
            throw new NotImplementedException();
        }

        public Task ChallengeAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            context.Response.StatusCode = 301;
            
            return Task.CompletedTask;
        }

        public Task ForbidAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task SignInAsync(HttpContext context, string scheme, ClaimsPrincipal principal, AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }

        public Task SignOutAsync(HttpContext context, string scheme, AuthenticationProperties properties)
        {
            throw new NotImplementedException();
        }
    }
}
