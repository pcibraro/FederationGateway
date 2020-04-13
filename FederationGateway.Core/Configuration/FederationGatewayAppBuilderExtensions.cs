using FederationGateway.Core.Middleware;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Text;

namespace FederationGateway.Core.Configuration
{
    public static class FederationGatewayAppBuilderExtensions
    {
        public static IApplicationBuilder UseFederationGateway(this IApplicationBuilder app)
        {
            app.UseMiddleware<P3PHeaderMiddleware>();
            app.UseMiddleware<MetadataMiddleware>();
            app.UseMiddleware<WsFedMiddleware>();
            app.UseMiddleware<Saml20Middleware>();

            return app;
        }
    }
}
