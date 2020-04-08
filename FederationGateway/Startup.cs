using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using FederationGateway.Core.Keys;
using FederationGateway.Core.Messaging.Metadata;
using FederationGateway.Core.Messaging.WsTrust;
using FederationGateway.Core.Profiles;
using FederationGateway.Core.RelyingParties;
using FederationGateway.Core.SessionManagers;
using FederationGateway.Core.ResponseProcessing;
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


            services.AddSingleton<WsFederationMetadataSerializer>();
            services.AddSingleton<WsTrustSerializer>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IKeyMaterialService>(new DefaultKeyMaterialService(certificate));
            services.AddSingleton<IRelyingPartyStore>(new InMemoryRelyingPartyStore(Config.RelyingParties));
            services.AddSingleton<IProfileManager>(new DefaultProfileManager());
            services.AddScoped<ISignInSessionManager>(s => 
                new DefaultSignInSessionManager(s.GetService<IHttpContextAccessor>(), "IdentityServer"));
            services.AddSingleton<SignInResponseGenerator>();
            services.Configure<FederationGatewayOptions>(options => Configuration.GetSection("IdentityServer").Bind(options));

            services.AddControllersWithViews();
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

            app.UseMiddleware<P3PHeaderMiddleware>();

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                MinimumSameSitePolicy = SameSiteMode.None,
            });

            app.UseAuthentication();

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