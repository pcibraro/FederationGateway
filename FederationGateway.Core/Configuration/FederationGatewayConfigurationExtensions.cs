using FederationGateway.Core.Keys;
using FederationGateway.Core.Messaging.Metadata;
using FederationGateway.Core.Messaging.SamlP;
using FederationGateway.Core.Messaging.WsTrust;
using FederationGateway.Core.Profiles;
using FederationGateway.Core.RelyingParties;
using FederationGateway.Core.ResponseProcessing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace FederationGateway.Core.Configuration
{
    public static class FederationGatewayConfigurationExtensions
    {
        public static IServiceCollection AddInMemoryFederationGateway(this IServiceCollection services, 
            IEnumerable<RelyingParty> relyingParties,
            IProfileManager profileManager,
            X509Certificate2 issuerCert,
            Action<FederationGatewayOptions> options)
        {
            services.AddCommonServices(profileManager, issuerCert, options);
            services.AddSingleton<IRelyingPartyStore>(new InMemoryRelyingPartyStore(relyingParties));

            return services;
        }

        private static IServiceCollection AddCommonServices(this IServiceCollection services,
            IProfileManager profileManager,
            X509Certificate2 issuerCert,
            Action<FederationGatewayOptions> options)
        {
            services.AddSingleton<WsFederationMetadataSerializer>();
            services.AddSingleton<WsTrustSerializer>();
            services.AddSingleton<SamlResponseSerializer>();
            services.AddSingleton<IKeyMaterialService>(new DefaultKeyMaterialService(issuerCert));
            services.AddSingleton<IProfileManager>(profileManager);
            services.AddSingleton<SignInResponseGenerator>();
            services.Configure<FederationGatewayOptions>(options);

            return services;
        }
   }
}
