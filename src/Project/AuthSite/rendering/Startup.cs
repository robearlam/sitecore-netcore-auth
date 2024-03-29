using System;
using System.Collections.Generic;
using System.Globalization;
using Feature.Auth.Rendering.Extensions;
using IdentityModel;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Project.AuthSite.Rendering.Configuration;
using Project.AuthSite.Rendering.Models;
using Sitecore.AspNet.ExperienceEditor;
using Sitecore.AspNet.RenderingEngine.Extensions;
using Sitecore.AspNet.RenderingEngine.Localization;
using Sitecore.AspNet.Tracking;
using Sitecore.LayoutService.Client.Extensions;
using Sitecore.LayoutService.Client.Newtonsoft.Extensions;
using Sitecore.LayoutService.Client.Request;

namespace Project.AuthSite.Rendering
{
    public class Startup
    {
        private static readonly string _defaultLanguage = "en";

        public Startup(IConfiguration configuration)
        {
            // Example of using ASP.NET Core configuration binding for various Sitecore Rendering Engine settings.
            // Values can originate in appsettings.json, from environment variables, and more.
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-3.1
            Configuration = configuration.GetSection(SitecoreOptions.Key).Get<SitecoreOptions>();
        }

        private SitecoreOptions Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            //UnComment this to help with Debugging, but don't go live with this enabled!!!
            //IdentityModelEventSource.ShowPII = true;

            services
                .AddRouting()
                // You must enable ASP.NET Core localization to utilize localized Sitecore content.
                .AddLocalization()
                .AddMvc()
                // At this time the Layout Service Client requires Json.NET due to limitations in System.Text.Json.
                .AddNewtonsoftJson(o => o.SerializerSettings.SetDefaults());

            // Register the Sitecore Layout Service Client, which will be invoked by the Sitecore Rendering Engine.
            services.AddSitecoreLayoutService()
                // Set default parameters for the Layout Service Client from our bound configuration object.
                .WithDefaultRequestOptions(request =>
                {
                    request
                        .SiteName(Configuration.DefaultSiteName)
                        .ApiKey(Configuration.ApiKey);
                })
                .AddHttpHandler("default", Configuration.LayoutServiceUri)
                .AsDefaultHandler();

            // Register the Sitecore Rendering Engine services.
            services.AddSitecoreRenderingEngine(options =>
                {
                    //Register your components here
                    options
                        .AddModelBoundView<ContentBlockModel>("ContentBlock")
                        .AddFeatureAuth()
                        .AddDefaultPartialView("_ComponentNotFound");
                })
                // Includes forwarding of Scheme as X-Forwarded-Proto to the Layout Service, so that
                // Sitecore Media and other links have the correct scheme.
                .ForwardHeaders()
                //handle the forwarding of authentication cookies
                .ForwardAuthCookies()
                // Enable forwarding of relevant headers and client IP for Sitecore Tracking and Personalization.
                .WithTracking()
                // Enable support for the Experience Editor.
                .WithExperienceEditor(options =>
                {
                    // Experience Editor integration needs to know the external URL of your rendering host,
                    // if behind HTTPS termination or another proxy (like Traefik).
                    if (Configuration.RenderingHostUri != null)
                    {
                        options.ApplicationUrl = Configuration.RenderingHostUri;
                    }
                });

            // Enable support for robot detection.
            services.AddSitecoreVisitorIdentification(options =>
            {
                // Usually the SitecoreInstanceUri is same host as the Layout Service, but it can be any Sitecore CD/CM
                // instance which shares same AspNet session with Layout Service. This address should be accessible
                // from the Rendering Host and will be used to proxy robot detection scripts.
                options.SitecoreInstanceUri = Configuration.InstanceUri;
            });

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "oidc";
                })
                .AddCookie(options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                    options.Cookie.Name = "sitecore_userticket";
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.ClientId = "AuthSite";
                    options.Authority = Configuration.IDServerAuthority;
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.RequireHttpsMetadata = false;
                    options.ResponseType = "code token";

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("sitecore.profile");
                    options.Scope.Add("offline_access");
                    options.Scope.Add("sitecore.profile.api");

                    options.SaveTokens = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = JwtClaimTypes.Name,
                        RoleClaimType = JwtClaimTypes.Role,
                    };
                });

            services.AddHttpClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // When running behind HTTPS termination, set the request scheme according to forwarded protocol headers.
            // Also set the Request IP, so that it can be passed on to the Sitecore Layout Service for tracking and personalization.
            app.UseForwardedHeaders(ConfigureForwarding(env));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // The Experience Editor endpoint should not be enabled in production DMZ.
            // See the SDK documentation for details.
            if (Configuration.EnableExperienceEditor)
            {
                // Enable the Sitecore Experience Editor POST endpoint.
                app.UseSitecoreExperienceEditor();
            }

            // Standard ASP.NET Core routing and static file support.
            app.UseRouting();
            app.UseStaticFiles();

            app.UseAuthentication();
            app.UseAuthorization();

            // Enable ASP.NET Core Localization, which is required for Sitecore content localization.
            app.UseRequestLocalization(options =>
            {
                // If you add languages in Sitecore which this site / Rendering Host should support, add them here.
                var supportedCultures = new List<CultureInfo> {new CultureInfo(_defaultLanguage)};
                options.DefaultRequestCulture = new RequestCulture(_defaultLanguage, _defaultLanguage);
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;

                // Allow culture to be resolved via standard Sitecore URL prefix and query string (sc_lang).
                options.UseSitecoreRequestLocalization();
            });

            // Enable proxying of Sitecore robot detection scripts
            app.UseSitecoreVisitorIdentification();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    "error",
                    "error",
                    new {controller = "Default", action = "Error"}
                );

                endpoints.MapControllerRoute(
                    "secure",
                    "secure",
                    new { controller = "Secure", action = "Index" }
                );

                // Enables the default Sitecore URL pattern with a language prefix.
                endpoints.MapSitecoreLocalizedRoute("sitecore", "Index", "Default");

                // Fall back to language-less routing as well, and use the default culture (en).
                endpoints.MapFallbackToController("Index", "Default");
            });
        }

        private ForwardedHeadersOptions ConfigureForwarding(IWebHostEnvironment env)
        {
            var options = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            };
            if (env.IsDevelopment())
            {
                // Allow forwarding of headers from Traefik in development
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            }
            // ReSharper disable once RedundantIfElseBlock
            else
            {
                // TODO: You should configure forwarding options here appropriately based on your test/production environments.
                // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-3.1
            }

            return options;
        }
    }
}
