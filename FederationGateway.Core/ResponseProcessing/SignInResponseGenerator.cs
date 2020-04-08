﻿using FederationGateway.Core;
using FederationGateway.Core.Profiles;
using FederationGateway.Core.RelyingParties;
using FederationGateway.Core.Keys;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FederationGateway.Core.Messaging.WsTrust;
using System.Text;
using System.Xml;
using System.IO;
using FederationGateway.Core.Configuration;

namespace FederationGateway.Core.ResponseProcessing
{
    public class SignInResponseGenerator
    {
        private readonly ILogger<SignInResponseGenerator> _logger;
        private readonly IRelyingPartyStore _relyingPartyStore;
        private readonly IProfileManager _profileManager;
        private readonly IKeyMaterialService _keyService;
        private readonly FederationGatewayOptions _options;

        public SignInResponseGenerator(ILogger<SignInResponseGenerator> logger, 
            IRelyingPartyStore relyingPartyStore, 
            IProfileManager profileManager,
            IKeyMaterialService keyService,
            IOptions<FederationGatewayOptions> options
            )
        {
            _logger = logger;
            _relyingPartyStore = relyingPartyStore;
            _profileManager = profileManager;
            _keyService = keyService;
            _options = options.Value;
        }

        public async Task<WsTrustRequestSecurityTokenResponse> GenerateWsSignInResponse(SignInRequest request)
        {
            _logger.LogDebug("Creating WS-Federation signin response");

            var rp = await _relyingPartyStore.FindRelyingPartyByRealm(request.Realm);

            // create profile
            var outgoingSubject = await CreateUserProfileAsync(request);

            // create token for user
            var token = await CreateSecurityTokenAsync(request, rp, outgoingSubject);

            var response = new WsTrustRequestSecurityTokenResponse
            {
                AppliesTo = new Uri(request.Realm),
                LifeTime = new WsTrustLifetime
                {
                    Created = token.ValidFrom,
                    Expires = token.ValidTo
                },
                RequestedSecurityToken = (Saml2SecurityToken)token
            };

            return response;
        }

        protected async Task<ClaimsIdentity> CreateUserProfileAsync(SignInRequest request)
        {
            var profile = await _profileManager.GetProfileAsync(request);

            return profile;
        }

        private async Task<SecurityToken> CreateSecurityTokenAsync(SignInRequest request, RelyingParty rp, ClaimsIdentity outgoingSubject)
        {
            var now = DateTime.Now;

            var outgoingNameId = outgoingSubject.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if(outgoingNameId == null)
            {
                _logger.LogError("The user profile does not have a name id");

                throw new SignInException("The user profile does not have a name id");
            }

            var issuer = new Saml2NameIdentifier(_options.IssuerName);

            var nameId = new Saml2NameIdentifier(outgoingNameId.Value);

            var subjectConfirmationData = new Saml2SubjectConfirmationData();
            subjectConfirmationData.NotOnOrAfter = now.AddMinutes(
                rp.TokenLifetimeInMinutes.GetValueOrDefault(_options.DefaultNotOnOrAfterInMinutes));

            if (request.Parameters.ContainsKey("Recipient"))
            {
                subjectConfirmationData.Recipient = new Uri(request.Parameters["Recipient"]);
            }
            else
            {
                subjectConfirmationData.Recipient = new Uri(rp.ReplyUrl);
            }
                
            var subjectConfirmation = new Saml2SubjectConfirmation(new Uri("urn:oasis:names:tc:SAML:2.0:cm:bearer"),
                    subjectConfirmationData);

            subjectConfirmation.NameIdentifier = nameId;
            
            var subject = new Saml2Subject(subjectConfirmation);

            var conditions = new Saml2Conditions(new Saml2AudienceRestriction[]
            {
                new Saml2AudienceRestriction(request.Realm)
            });

            conditions.NotOnOrAfter = now.AddMinutes(
                rp.TokenLifetimeInMinutes.GetValueOrDefault(_options.DefaultNotOnOrAfterInMinutes));
            conditions.NotBefore = now.Subtract(TimeSpan.FromMinutes(_options.DefaultNotBeforeInMinutes));

            var authContext = new Saml2AuthenticationContext(new Uri("urn:oasis:names:tc:SAML:2.0:ac:classes:PasswordProtectedTransport"));

            var authStatement = new Saml2AuthenticationStatement(authContext, now);
            authStatement.SessionIndex = (request.Parameters.ContainsKey("SessionIndex")) ? request.Parameters["SessionIndex"] : null;

            var attributeStament = new Saml2AttributeStatement();
            foreach(var claim in outgoingSubject.Claims)
            {
                attributeStament.Attributes.Add(new Saml2Attribute(claim.Type, claim.Value));
            }

            var assertion = new Saml2Assertion(issuer);
            assertion.Id = new Saml2Id();
            assertion.Subject = subject;
            assertion.Conditions = conditions;
            assertion.Statements.Add(attributeStament);
            assertion.Statements.Add(authStatement);
            assertion.IssueInstant = now;
            
            assertion.SigningCredentials = await _keyService.GetSigningCredentialsAsync();
            
            var token = new Saml2SecurityToken(assertion);
            token.SigningKey = assertion.SigningCredentials.Key;
            
            return token;
        }

        private SignInResult CreateResponse(SignInRequest request, RelyingParty rp, SecurityToken token)
        {
            var result = new SignInResult
            {
                Token = token,
                ReplyUrl = rp.ReplyUrl,
                Realm = rp.Realm
            };

            return result;
        }
    }
}