using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using FederationGateway.Core.Configuration;
using FederationGateway.Core.Messaging.SamlP;
using FederationGateway.Core.RelyingParties;
using FederationGateway.Core.ResponseProcessing;
using FederationGateway.Core.SessionManagers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FederationGateway.Controllers
{
    public class Saml20Controller : Controller
    {
        private readonly ILogger<Saml20Controller> _logger;
        private readonly IRelyingPartyStore _relyingPartyStore;
        private readonly SignInResponseGenerator _responseGenerator;
        private readonly ISignInSessionManager _sessionManager;
        private readonly SamlResponseSerializer _serializer;
        private readonly IOptions<FederationGatewayOptions> _options;

        public Saml20Controller(ILogger<Saml20Controller> logger,
            IRelyingPartyStore relyingPartyStore,
            ISignInSessionManager sessionManager,
            SignInResponseGenerator responseGenerator,
            SamlResponseSerializer serializer,
            IOptions<FederationGatewayOptions> options)
        {
            _logger = logger;
            _relyingPartyStore = relyingPartyStore;
            _sessionManager = sessionManager;
            _responseGenerator = responseGenerator;
            _serializer = serializer;
            _options = options;
        }

        public async Task<IActionResult> Index()
        {
            if(Request.Method != "POST" || Request.Method != "GET")
            {
                return StatusCode((int)HttpStatusCode.MethodNotAllowed);
            }

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", new { qs = this.Request.QueryString });
            }

            if(!this.Request.Query.ContainsKey("SAMLRequest"))
            {
                return BadRequest("Invalid SAMLRequest message");
            }

            _logger.LogInformation("Received SAML 2.0 Request. {0}", this.Request.QueryString.ToUriComponent());

            var samlRequest = this.Request.Query["SAMLRequest"];

            SamlRequestMessage message = null;

            if (Request.Method == "GET")
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
                return BadRequest($"{message.Issuer} is not registered");
            }
            
            var parameters = new Dictionary<string, string>(
                    Request.Query.Select(q => new KeyValuePair<string, string>(q.Key, q.Value[0])));

            if (message.IsSignInMessage)
            {
                var output = await HandleSignIn(message, 
                    _options.Value.IssuerName, 
                    parameters, 
                    relyingParty.ReplyUrl);

                return Content(output, "text/html");
            }
            else
            {
                var output = HandleSignOut(message,
                    _options.Value.IssuerName,
                    parameters,
                    relyingParty.LogoutUrl);

                return Content(output, "text/html");
            }
        }

        private async Task<string> HandleSignIn(SamlRequestMessage message, 
            string issuer, 
            IDictionary<string, string> parameters, 
            string replyUrl)
        {
            var request = new SignInRequest
            {
                User = this.User,
                Realm = message.Issuer,
                Parameters = parameters
            };

            var response = await _responseGenerator.GenerateSignInResponse(request);

            _sessionManager.AddRealm(message.Issuer);

            var samlResponse = new SamlResponseMessage();
            samlResponse.Token = response.Token;
            samlResponse.Id = Guid.NewGuid().ToString();
            samlResponse.InResponseTo = message.Id;
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

        private string HandleSignOut(SamlRequestMessage message,
            string issuer,
            IDictionary<string, string> parameters,
            string logoutUrl)
        {
            _sessionManager.ClearEndpoints();

            var samlResponse = new SamlResponseMessage();
            samlResponse.Id = Guid.NewGuid().ToString();
            samlResponse.InResponseTo = message.Id;
            samlResponse.Issuer = issuer;
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
            
            if(!string.IsNullOrWhiteSpace(relayState))
                strBuilder.Append(string.Format("<input type='hidden' name='RelayState' id='RelayState' value='{0}' />", relayState));

            strBuilder.Append("<script type='text/javascript'>document.forms[0].submit();</ script>");

            strBuilder.Append("</body></html>");

            return strBuilder.ToString();
        }
    }
}