using FederationGateway.Core.Keys;
using FederationGateway.Core.Messaging.Metadata;
using FederationGateway.Core.Messaging.SamlP;
using FederationGateway.Core.Messaging.WsTrust;
using FederationGateway.Core.Profiles;
using FederationGateway.Core.ResponseProcessing;
using FederationGateway.Providers.Keys;
using FederationGateway.Providers.Profiles;
using FederationGateway.Providers.RelyingParties;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FederationGateway.Core.Configuration
{
    public static class FederationGatewayConfigurationExtensions
    {
        public static IServiceCollection AddFederationGateway(this IServiceCollection services,
            IProfileManager profileManager,
            IRelyingPartyStore relyingPartyStore,
            X509Certificate2 issuerCert,
            Action<FederationGatewayOptions> options)
        {
            services.AddSingleton<WsFederationMetadataSerializer>();
            services.AddSingleton<WsTrustSerializer>();
            services.AddSingleton<SamlResponseSerializer>();
            services.AddSingleton<IKeyMaterialService>(new DefaultKeyMaterialService(issuerCert));
            services.AddSingleton<IProfileManager>(profileManager);
            services.AddSingleton<IRelyingPartyStore>(relyingPartyStore);
            services.AddSingleton<SignInResponseGenerator>();
            services.Configure<FederationGatewayOptions>(options);

            return services;
        }
   }
}
