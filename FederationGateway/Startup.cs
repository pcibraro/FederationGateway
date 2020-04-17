using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FederationGateway.Core.Profiles;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.WsFederation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using FederationGateway.Core.Configuration;
using FederationGateway.Core.Middleware;
using System.Web;
using FederationGateway.Core.Messaging.SamlP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using FederationGateway.Providers.RelyingParties;

namespace FederationGateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var certificate = new X509Certificate2(Path.Combine(Directory.GetCurrentDirectory(), "federationgateway.pfx"), "identityserver");

            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = WsFederationDefaults.AuthenticationScheme;
            })
            .AddWsFederation(options =>
            {
                options.Wtrealm = Configuration["WsFederation:realm"];
                options.MetadataAddress = Configuration["WsFederation:metadata"];
                options.CallbackPath = "/external/wsfed";
                options.AllowUnsolicitedLogins = true;
            })
            .AddCookie(options =>
            {
                AddCookieOptions(options, "federationgateway");
            });

            var authentication = services
              .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddJwtBearer("Bearer", c =>
              {
                  c.Authority = Configuration["Bearer:Authority"];
                  c.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                  {
                      ValidAudiences = Configuration["Bearer:Audience"].Split(";")
                  };
              });

            services.AddFederationGateway(
                new DefaultProfileManager(),
                new InMemoryRelyingPartyStore(Config.RelyingParties),
                certificate,
                options => Configuration.GetSection("IdentityServer").Bind(options));

            services.AddControllers().AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();

            var options = new FederationGatewayOptions();
            Configuration.GetSection("IdentityServer").Bind(options);
            app.UseFederationGateway(options);

            app.UseCors(cfg =>
            {
                cfg.AllowAnyOrigin();
                cfg.AllowAnyMethod();
                cfg.AllowAnyHeader();
            });

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private static void AddCookieOptions(CookieAuthenticationOptions options, string name)
        {
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None;
            options.Cookie.Name = name;
            options.Events.OnValidatePrincipal = (context) =>
            {
                if (DateTime.UtcNow > context.Properties.ExpiresUtc)
                {
                    context.ShouldRenew = true;
                }

                return Task.CompletedTask;
            };
        }
    }
}
