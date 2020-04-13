using FederationGateway.Core.Configuration;
using FederationGateway.Core.Messaging.SamlP;
using FederationGateway.Core.RelyingParties;
using FederationGateway.Core.ResponseProcessing;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Xml;
using System.IO;

namespace FederationGateway.Core.Middleware
{
    public class Saml20Middleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<Saml20Middleware> _logger;
        private readonly IRelyingPartyStore _relyingPartyStore;
        private readonly SignInResponseGenerator _responseGenerator;
        private readonly SamlResponseSerializer _serializer;
        private readonly FederationGatewayOptions _options;

        public Saml20Middleware(RequestDelegate next,
            ILogger<Saml20Middleware> logger,
            IRelyingPartyStore relyingPartyStore,
            SignInResponseGenerator responseGenerator,
            SamlResponseSerializer serializer,
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
            var segment = (string.IsNullOrWhiteSpace(_options?.Saml?.Endpoint)) ? "/Saml20" : "/" + _options?.Saml?.Endpoint;

            if (context.Request.Path.StartsWithSegments(new PathString(segment), StringComparison.InvariantCultureIgnoreCase))
            {
                if (!(
                context.Request.Method.Equals("POST", StringComparison.InvariantCultureIgnoreCase) ||
                context.Request.Method.Equals("GET", StringComparison.InvariantCultureIgnoreCase))
                )
                {
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;

                    return;
                }

                if (!context.User.Identity.IsAuthenticated)
                {
                    var qs = context.Request.QueryString;
                    var url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.PathBase}{segment}/{context.Request.QueryString}";

                    await context.ChallengeAsync(new AuthenticationProperties
                    {
                        RedirectUri = url
                    });

                    return;
                }

                if (!context.Request.Query.ContainsKey("SAMLRequest"))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync($"Invalid SAMLRequest Request Message");

                    return;
                }

                _logger.LogInformation("Received SAML 2.0 Request. {0}", context.Request.QueryString.ToUriComponent());

                var samlRequest = context.Request.Query["SAMLRequest"];

                SamlRequestMessage message = null;

                if (context.Request.Method.Equals("GET", StringComparison.InvariantCultureIgnoreCase))
                {
                    message = SamlRequestMessage.CreateFromCompressedRequest(samlRequest);
                }
                else
                {
                    message = SamlRequestMessage.CreateFromEncodedRequest(samlRequest);
                }

                var relyingParty = await _relyingPartyStore.FindRelyingPartyByRealm(message.Issuer);

                if (relyingParty == null)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    await context.Response.WriteAsync($"{message.Issuer} is not registered");

                    return;
                }

                var parameters = context.Request.Query.ToDictionary(q => q.Key, q => q.Value[0]);

                if (message.IsSignInMessage)
                {
                    var output = await HandleSignIn(context,
                        message,
                        _options.IssuerName,
                        parameters,
                        relyingParty.ReplyUrl);

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(output);

                    return;
                }
                else
                {
                    var output = HandleSignOut(context, message,
                        _options.IssuerName,
                        parameters,
                        relyingParty.LogoutUrl);

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(output);

                    return;
                }
            }

            await _next(context);
        }

        private async Task<string> HandleSignIn(HttpContext context, SamlRequestMessage message,
            string issuer,
            IDictionary<string, string> parameters,
            string replyUrl)
        {
            var handler = CreateSessionHandler();

            var request = new SignInRequest
            {
                User = context.User,
                Realm = message.Issuer,
                Parameters = parameters
            };

            var response = await _responseGenerator.GenerateSignInResponse(request);

            handler.AddRealm(context, message.Issuer);

            var samlResponse = new SamlResponseMessage();
            samlResponse.Token = response.Token;
            samlResponse.Id = Guid.NewGuid().ToString();
            samlResponse.InResponseTo = message.Id;
            samlResponse.ReplyTo = new Uri(replyUrl);
            samlResponse.Issuer = issuer;
            samlResponse.ResponseType = "Response";

            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(new StringWriter(sb)))
            {
                _serializer.Serialize(xmlWriter, samlResponse);
            }

            var relayState = (parameters.ContainsKey("RelayState") ? parameters["RelayState"] : "");

            var form = BuildSignInFormPost(replyUrl, sb.ToString(), relayState);

            return form;
        }

        private string HandleSignOut(HttpContext context, SamlRequestMessage message,
            string issuer,
            IDictionary<string, string> parameters,
            string logoutUrl)
        {
            var handler = CreateSessionHandler();

            handler.ClearEndpoints(context);

            var samlResponse = new SamlResponseMessage();
            samlResponse.Id = Guid.NewGuid().ToString();
            samlResponse.InResponseTo = message.Id;
            samlResponse.Issuer = issuer;
            samlResponse.ReplyTo = new Uri(logoutUrl);
            samlResponse.ResponseType = "LogoutResponse";

            var sb = new StringBuilder();
            using (var xmlWriter = XmlWriter.Create(new StringWriter(sb)))
            {
                _serializer.Serialize(xmlWriter, samlResponse);
            }

            var form = BuildSignInFormPost(logoutUrl, sb.ToString(), null);

            return form;
        }

        private string BuildSignInFormPost(string replyTo, string samlResponse, string relayState)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("<html><head><title>saml sign in</title></head><body>");

            strBuilder.Append(string.Format("<form id='form1' action='{0}' method='post'>", replyTo));
            strBuilder.Append(string.Format("<input type='hidden' name='SAMLResponse' id='SAMLResponse' value='{0}' />", samlResponse));

            if (!string.IsNullOrWhiteSpace(relayState))
                strBuilder.Append(string.Format("<input type='hidden' name='RelayState' id='RelayState' value='{0}' />", relayState));

            strBuilder.Append("<script type='text/javascript'>document.forms[0].submit();</script>");

            strBuilder.Append("</body></html>");

            return strBuilder.ToString();
        }

        protected virtual SessionCookieHandler CreateSessionHandler()
        {
            var cookieName = (string.IsNullOrWhiteSpace(_options?.Saml?.CookieName)) ? "Saml20Endpoints" : 
                _options?.Saml?.CookieName;

            return new SessionCookieHandler(cookieName);
        }
    }
}
