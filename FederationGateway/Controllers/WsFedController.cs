﻿using System;
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
using FederationGateway.Models;
using FederationGateway.Core.ResponseProcessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.WsFederation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;

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

        public async Task Login()
        {
            if (!User.Identity.IsAuthenticated)
            {
                await this.HttpContext.ChallengeAsync();
            }
        }

        public async Task<IActionResult> Index()
        {
            if(!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            _logger.LogInformation("Received Ws-Fed Request. {0}", this.Request.QueryString.ToUriComponent());
           
            var message = WsFederationMessage.FromQueryString(this.Request.QueryString.ToUriComponent());

            if(message.IsSignInMessage)
            {
                var relyingParty = await _relyingPartyStore.FindRelyingPartyByRealm(message.Wtrealm);

                if(relyingParty == null)
                {
                    return BadRequest($"The realm {message.Wtrealm} is not registered");
                }

                var request = new SignInRequest
                {
                    User = this.User,
                    Realm = message.Wtrealm,
                    Parameters = message.Parameters
                };

                var response = await _responseGenerator.GenerateWsSignInResponse(request);

                var sb = new StringBuilder();
                using (var xmlWriter = XmlWriter.Create(new StringWriter(sb)))
                {
                    _serializer.Serialize(xmlWriter, response); 
                }

                var model = new WsFedSignInResponseModel
                {
                    Action = relyingParty.ReplyUrl,
                    WResult = sb.ToString(),
                    Wctx = message.Wctx
                };

                _sessionManager.AddRealm(message.Wtrealm);

                return View(model);
            } 
            else if(message.IsSignOutMessage)
            {
                var endpoints = new List<string>();

                var realms = _sessionManager.GetRealms();
                foreach(var realm in realms)
                {
                    var endpoint = await _relyingPartyStore.FindRelyingPartyByRealm(realm);
                    if(endpoint.LogoutUrl != null)
                        endpoints.Add(endpoint.LogoutUrl);
                }

                var model = new WsFedSignOutResponseModel
                {
                    LogoutUrls = endpoints
                };

                _sessionManager.ClearEndpoints();

                await this.HttpContext.SignOutAsync();

                return View("Logout", model);
            }

            return BadRequest("Invalid Ws-Fed Request Message");
        }
    }
}