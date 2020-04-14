using FederationGateway.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core.Configuration
{
    public static class FederationGatewayAppBuilderExtensions
    {
        public static IApplicationBuilder UseFederationGateway(this IApplicationBuilder app, FederationGatewayOptions options)
        {
            app.Map(options.MetadataEndpoint, builder => builder.UseMiddleware<MetadataMiddleware>());
            app.Map(options.WsFedEndpoint, builder => builder.UseMiddleware<WsFedMiddleware>());
            app.Map(options.Saml20Endpoint, builder => builder.UseMiddleware<Saml20Middleware>());

            return app;
        }
    }
}
