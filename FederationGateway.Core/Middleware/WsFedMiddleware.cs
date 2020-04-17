using FederationGateway.Core.Messaging.WsTrust;
using FederationGateway.Core.ResponseProcessing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.WsFederation;
using System.Net;
using System.Xml;
using System.IO;
using System.Linq;
using FederationGateway.Core.Configuration;
using Microsoft.Extensions.Options;
using FederationGateway.Providers.RelyingParties;
using FederationGateway.Providers.Profiles;

namespace FederationGateway.Core.Middleware
{
    public class WsFedMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WsFedMiddleware> _logger;
        private readonly IRelyingPartyStore _relyingPartyStore;
        private readonly SignInResponseGenerator _responseGenerator;
        private readonly WsTrustSerializer _serializer;
        private readonly FederationGatewayOptions _options;

        public WsFedMiddleware(RequestDelegate next,
            ILogger<WsFedMiddleware> logger,
            IRelyingPartyStore relyingPartyStore,
            SignInResponseGenerator responseGenerator,
            WsTrustSerializer serializer,
            IOptions<FederationGatewayOptions> options)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));
            if (relyingPartyStore == null) throw new ArgumentNullException(nameof(relyingPartyStore));
            if (responseGenerator == null) throw new ArgumentNullException(nameof(responseGenerator));
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (options == null) throw new ArgumentNullException(nameof(options));

            _next = next;
            _logger = logger;
            _relyingPartyStore = relyingPartyStore;
            _responseGenerator = responseGenerator;
            _serializer = serializer;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            var segment = _options.WsFedEndpoint;

            if (context.Request.Path.StartsWithSegments(new PathString(segment), StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation("Received WsFed Request. {0}", context.Request.QueryString.ToUriComponent());

                if (!context.User.Identity.IsAuthenticated)
                {
                    _logger.LogInformation("User is not authenticated. Redirecting to authentication provider");

                    var qs = context.Request.QueryString;
                    var url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{segment}/{context.Request.QueryString}";

                    await context.ChallengeAsync(new AuthenticationProperties
                    {
                        RedirectUri = url
                    });

                    return;
                }

                var message = WsFederationMessage.FromQueryString(context.Request.QueryString.ToUriComponent());

                if (message.IsSignInMessage)
                {
                    var relyingParty = await _relyingPartyStore.GetByRealm(message.Wtrealm);

                    if (relyingParty == null)
                    {
                        _logger.LogWarning("No relying party found for realm {0}", message.Wtrealm);

                        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                        await context.Response.WriteAsync($"The realm { message.Wtrealm} is not registered");
                    }

                    _logger.LogWarning("Processing WsFed Sign In for realm {0}", message.Wtrealm);

                    var output = await HandleSignIn(message, context, relyingParty.ReplyUrl);

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(output);

                    return;
                }
                else if (message.IsSignOutMessage)
                {
                    _logger.LogWarning("Processing WsFed Sign Out");

                    var output = await HandleSignOut(message, context);

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(output);

                    return;
                }

                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync($"Invalid Ws-Fed Request Message");
                
                return;
            }

            await _next(context);
        }

        private async Task<string> HandleSignIn(WsFederationMessage message, HttpContext context, string replyUrl)
        {
            var handler = CreateSessionHandler();

            var request = new SignInRequest
            {
                User = context.User,
                Realm = message.Wtrealm,
                Parameters = message.Parameters
            };

            var response = await _responseGenerator.GenerateSignInResponse(request);

            var wsTrustResponse = new WsTrustRequestSecurityTokenResponse
            {
                AppliesTo = new Uri(request.Realm),
                LifeTime = new WsTrustLifetime
                {
                    Created = response.Token.ValidFrom,
                    Expires = response.Token.ValidTo
                },
                RequestedSecurityToken = response.Token
            };

            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(new StringWriter(sb)))
            {
                _serializer.Serialize(xmlWriter, wsTrustResponse);
            }

            _logger.LogInformation("Adding realm in session cookie {0}", message.Wtrealm);

            handler.AddRealm(context, message.Wtrealm);

            var wsResponse = new WsFederationMessage();
            wsResponse.Wa = "wsignin1.0";
            wsResponse.Wresult = sb.ToString();
            wsResponse.Wctx = message.Wctx;
            wsResponse.IssuerAddress = replyUrl;

            var form = wsResponse.BuildFormPost();

            return form;
        }

        private async Task<string> HandleSignOut(WsFederationMessage message, HttpContext context)
        {
            var handler = CreateSessionHandler();
            
            var endpoints = new List<string>();

            var realms = handler.GetRealms(context);
            foreach (var realm in realms)
            {
                _logger.LogInformation("Processing WsFed Signout for {0}", realm);

                var endpoint = await _relyingPartyStore.GetByRealm(realm);
                if (endpoint.LogoutUrl != null)
                {
                    var logoutUrl = endpoint.LogoutUrl;

                    if (!endpoint.LogoutUrl.EndsWith("/"))
                        logoutUrl += "/";

                    logoutUrl += "?wa=wsignoutcleanup1.0";

                    endpoints.Add(logoutUrl);
                }
            }

            handler.ClearEndpoints(context);

            await context.SignOutAsync();

            var form = BuildLogoutFormPost(endpoints, message.Wreply);

            return form;
        }

        private string BuildLogoutFormPost(IEnumerable<string> endpoints, string replyUrl)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("<html><head><title>signout</title></head><body>");
            strBuilder.Append("<p>You are now signed out</p>");
            foreach (var endpoint in endpoints)
            {
                strBuilder.Append(string.Format("<iframe style='visibility: hidden; width: 1px; height: 1px' src='{0}'></iframe>",
                    WebUtility.HtmlEncode(endpoint)));
            }

            if (!string.IsNullOrWhiteSpace(replyUrl))
            {
                strBuilder.Append(string.Format("<script type='text/javascript'>window.location = '{0}'</ script>",
                    WebUtility.HtmlEncode(replyUrl)));
            }

            strBuilder.Append("</body></html>");

            return strBuilder.ToString();
        }

        protected virtual SessionCookieHandler CreateSessionHandler()
        {
            var cookieName = _options.WsFedCookieName;

            return new SessionCookieHandler(cookieName);
        }
    }
}
