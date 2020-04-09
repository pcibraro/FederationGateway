using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using FederationGateway.Core;
using FederationGateway.Core.Messaging.WsTrust;
using FederationGateway.Core.RelyingParties;
using FederationGateway.Core.SessionManagers;
using FederationGateway.Core.ResponseProcessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using System.Net;

namespace FederationGateway.Controllers
{
    public class WsFedController : Controller
    {
        private readonly ILogger<WsFedController> _logger;
        private readonly IRelyingPartyStore _relyingPartyStore;
        private readonly SignInResponseGenerator _responseGenerator;
        private readonly ISignInSessionManager _sessionManager;
        private readonly WsTrustSerializer _serializer;

        public WsFedController(ILogger<WsFedController> logger, 
            IRelyingPartyStore relyingPartyStore,
            ISignInSessionManager sessionManager,
            SignInResponseGenerator responseGenerator,
            WsTrustSerializer serializer)
        {
            _logger = logger;
            _relyingPartyStore = relyingPartyStore;
            _sessionManager = sessionManager;
            _responseGenerator = responseGenerator;
            _serializer = serializer;
        }

        public async Task Login(string qs)
        {
            if (!User.Identity.IsAuthenticated)
            {
                await this.HttpContext.ChallengeAsync(new AuthenticationProperties
                {
                    RedirectUri = Url.Action("Index", "WsFed", null, "https") + "/" + qs
                }) ;
            }
        }

        public async Task<IActionResult> Index()
        {
            if(!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", new { qs = this.Request.QueryString });
            }

            _logger.LogInformation("Received Ws-Fed Request. {0}", this.Request.QueryString.ToUriComponent());
            
            var message = WsFederationMessage.FromQueryString(this.Request.QueryString.ToUriComponent());

            if (message.IsSignInMessage)
            {
                var relyingParty = await _relyingPartyStore.FindRelyingPartyByRealm(message.Wtrealm);

                if (relyingParty == null)
                {
                    return BadRequest($"The realm {message.Wtrealm} is not registered");
                }

                var output = await HandleSignIn(message, relyingParty.ReplyUrl);

                return Content(output, "text/html");
            } 
            else if(message.IsSignOutMessage)
            {
                var output = await HandleSignOut(message);

                return Content(output, "text/html");
            }

            return BadRequest("Invalid Ws-Fed Request Message");
        }

        private async Task<string> HandleSignIn(WsFederationMessage message, string replyUrl)
        {
            var request = new SignInRequest
            {
                User = this.User,
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

            _sessionManager.AddRealm(message.Wtrealm);

            var wsResponse = new WsFederationMessage();
            wsResponse.Wa = "wsignin1.0";
            wsResponse.Wresult = sb.ToString();
            wsResponse.Wctx = message.Wctx;
            wsResponse.IssuerAddress = replyUrl;

            var form = wsResponse.BuildFormPost();

            return form;
        }

        private async Task<string> HandleSignOut(WsFederationMessage message)
        {
            var endpoints = new List<string>();

            var realms = _sessionManager.GetRealms();
            foreach (var realm in realms)
            {
                var endpoint = await _relyingPartyStore.FindRelyingPartyByRealm(realm);
                if (endpoint.LogoutUrl != null)
                {
                    var logoutUrl = endpoint.LogoutUrl;

                    if (!endpoint.LogoutUrl.EndsWith("/"))
                        logoutUrl += "/";

                    logoutUrl += "?wa=wsignoutcleanup1.0";

                    endpoints.Add(logoutUrl);
                }
            }

            _sessionManager.ClearEndpoints();

            await this.HttpContext.SignOutAsync();

            var form = BuildLogoutFormPost(endpoints, message.Wreply);

            return form;
        }
        
        private string BuildLogoutFormPost(IEnumerable<string> endpoints, string replyUrl)
        {
            StringBuilder strBuilder = new StringBuilder();
            strBuilder.Append("<html><head><title>signout</title></head><body>");
            strBuilder.Append("<p>You are now signed out</p>");
            foreach(var endpoint in endpoints)
            {
                strBuilder.Append(string.Format("<iframe style='visibility: hidden; width: 1px; height: 1px' src='{0}'></iframe>",
                    WebUtility.HtmlEncode(endpoint)));
            }

            if(!string.IsNullOrWhiteSpace(replyUrl))
            {
                strBuilder.Append(string.Format("<script type='text/javascript'>window.location = '{0}'</ script>", 
                    WebUtility.HtmlEncode(replyUrl)));
            }

            strBuilder.Append("</body></html>");
            
            return strBuilder.ToString();
        }
    }
}