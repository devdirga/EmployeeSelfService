using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Flurl;
using KANO.Core.Lib.Auth;
using KANO.Core.Lib.Auth.BasicAuthentication;
using KANO.Core.Lib.Middleware;
using KANO.Core.Lib.Middleware.ServerSideAnalytics;
using KANO.Core.Lib.Middleware.ServerSideAnalytics.Api;
using KANO.Core.Service;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using ProxyKit;

namespace KANO.ESS
{

    public class LayoutInjectConfiguration
    {
        public string GatewayUrl { get; set; }
        public string PushNotificationPublicKey { get; set; }
        public string UploadMaxFileSize { get; set; }
        public string[] UploadAllowedExtension { get; set; }
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IHostingEnvironment Environment { get; }
        public static IConfiguration StaticConfig { get; private set; }
        public static object Tick { get; private set; }
        

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
            StaticConfig = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<LayoutInjectConfiguration>(Configuration.GetSection("Request"));
            services.AddProxy();

            // HTTP Error 500.30 - ANCM In - Process Start Failure
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.None;
                if (Environment.IsProduction())
                {
                    //options.Secure = CookieSecurePolicy.Always;
                }
            });

            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = System.TimeSpan.FromHours(4);
                options.Cookie.HttpOnly = true;
            });

            // areas view
            services.Configure<RazorViewEngineOptions>(options =>
            {
                options.AreaViewLocationFormats.Clear();
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}.cshtml");
                options.AreaViewLocationFormats.Add("/Areas/Views/Shared/{0}.cshtml");
            });

            // for post body
            services.Configure<MvcJsonOptions>(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });           

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddUserSession();                      
            //services.AddScoped<IdentityMiddlewareBuilder>();

            // Register BasicAuth
            services.AddScoped<IUserAuthService, UserAuthService>();
            services
                .AddAuthentication(BasicAuthenticationDefaults.AuthenticationScheme)
                .AddScheme<BasicAuthenticationOptions, BasicAuthenticationHandler>(
                    BasicAuthenticationDefaults.AuthenticationScheme, (a) => { 
                        a.Realm = "ess"; 
                    }
                );
            services.AddSingleton<IPostConfigureOptions<BasicAuthenticationOptions>, BasicAuthenticationPostConfigureOptions>();

            // Register default auth
            var authMode = Configuration["Auth:Mode"];
            string authPolicy = null;
            switch (authMode)
            {
                case "Cookie":
                    authPolicy = CookieAuthenticationDefaults.AuthenticationScheme;
                    services
                        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                        .AddCookie(options =>
                            {
                                options.LoginPath = "/Site/Auth/Login";
                                options.LogoutPath = "/Site/Auth/Logout";
                                options.AccessDeniedPath = "/Site/Auth/Unauthorized";
                                options.SlidingExpiration = true;
                                options.ExpireTimeSpan = TimeSpan.FromHours(4);
                                options.Cookie.Expiration = TimeSpan.FromHours(4);
                            }
                        );
                    break;
                case "Windows":
                    authPolicy = IISDefaults.AuthenticationScheme;
                    services.AddAuthentication(IISDefaults.AuthenticationScheme);
                    break;
                default:
                    throw new Exception("Unknown Auth:Mode. Please configure your appsettings.json");
            }

            // enable auth on all endpoint, and set compatibility lock on v. 2.1
            // 2019-05-24: Adds named authpolicy to support cookie/windows auth combined with basic auth for API calls
            services.AddMvc(options =>
            {
                options.AllowEmptyInputInBodyModelBinding = true;
                options.Filters.Add(
                    new AuthorizeFilter(
                        new AuthorizationPolicyBuilder(
                            authPolicy, BasicAuthenticationDefaults.AuthenticationScheme
                        )
                        .RequireAuthenticatedUser()
                        .Build()
                    )
                );
            }).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            
        }        

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {
            
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                Tick = null;
            }
            else
            {
                app.UseExceptionHandler("/Dashboard/Error");
                Tick = DateTime.Now.Ticks;
                //app.UseHsts();
                app.UseHttpsRedirection();
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseSession();
            app.UseAuthentication();

            // Setting notification hub fowarder
            var gatewayURL = Configuration["Request:GatewayUrl"];            
            var odooURL = Configuration["Odoo:Url"];            
            if (string.IsNullOrWhiteSpace(gatewayURL)) {
                throw new Exception("Unable to find Request:GatewayUrl configuration");
            }

            var notificationHub = Configuration["Request:NotificationHub"];
            if (string.IsNullOrWhiteSpace(notificationHub))
            {
                throw new Exception("Unable to find Request:NotificationHub configuration");
            }

            app.UseWebSockets();
            app.Map("/Notification/Hub", signalrApp =>
            {
                var notificationURL = new UriBuilder(Url.Combine(gatewayURL, notificationHub));
                var httpNotificationURL = notificationURL.Uri.ToString();
                
                notificationURL.Scheme = "ws";
                var wsNotificationURL = notificationURL.Uri.ToString();                

                // SignalR, as part of it's protocol, needs both http and ws traffic
                // to be forwarded to the servers hosting signalr hubs.
                signalrApp.UseWebSocketProxy(context => new Uri(wsNotificationURL));
                signalrApp.RunProxy(context => context
                    .ForwardTo(new Uri(httpNotificationURL))
                    .AddXForwardedHeaders()
                    .Send());
            });

            app.Map("/Site/Diagnostic/GetLog", logApp =>
            {
                var logURL = new UriBuilder(Url.Combine(gatewayURL, "api/common/logger/get"));
                logApp.RunProxy(context => context
                    .ForwardTo(logURL.Uri)
                    .AddXForwardedHeaders()
                    .Send());
            });            

            app.Map("/odoo", odoo =>
            {
                odoo.RunProxy(context => {
                    var sessionID = "";
                    var claim = context.User.FindFirst(CustomClaimTypes.OdooSessionID);
                    if (claim != null) {
                        sessionID = claim.Value;
                    }
                    var proxyReq = context.ForwardTo(odooURL).AddXForwardedHeaders();
                    var headers = proxyReq.UpstreamRequest.Headers;                    
                    var new_cookie = $"session_id={sessionID}";
                    var new_headers = headers.Append(new KeyValuePair<string, IEnumerable<string>>("Cookie", new string[] { new_cookie }));
                    var str_cookies = String.Join("; ", new_headers.Where(n => n.Key == "Cookie").Select(n => String.Join("; ", n.Value)));

                    headers.Remove("Cookie");
                    headers.Add("Cookie", new_cookie);
                    
                    return proxyReq.Send();
                });
            });

            app.Map("/web", odoo =>
            {
                odoo.RunProxy(context => {
                    var sessionID = "";
                    var claim = context.User.FindFirst(CustomClaimTypes.OdooSessionID);
                    if (claim != null) {
                        sessionID = claim.Value;
                    }

                    var ext = Path.GetExtension(context.Request.Path.Value);
                    var fullOdooURL = new UriBuilder(Url.Combine(odooURL, "web"));
                    if (ext.Contains("css") || ext.Contains("js"))
                    {
                        fullOdooURL = new UriBuilder(Url.Combine(odooURL, "web", context.Request.Path));
                    }
                    var proxyReq = context.ForwardTo(fullOdooURL.Uri).AddXForwardedHeaders();
                    var headers = proxyReq.UpstreamRequest.Headers;                    
                    var new_cookie = $"session_id={sessionID}";
                    var new_headers = headers.Append(new KeyValuePair<string, IEnumerable<string>>("Cookie", new string[] { new_cookie }));
                    var str_cookies = String.Join("; ", new_headers.Where(n => n.Key == "Cookie").Select(n => String.Join("; ", n.Value)));

                    headers.Remove("Cookie");
                    headers.Add("Cookie", new_cookie);
                    
                    return proxyReq.Send();
                });
            });

            app.Map("/survey", odoo =>
            {
                odoo.RunProxy(context => {
                    var sessionID = "";
                    var claim = context.User.FindFirst(CustomClaimTypes.OdooSessionID);
                    if (claim != null)
                    {
                        sessionID = claim.Value;
                    }

                    var ext = Path.GetExtension(context.Request.Path.Value);
                    var fullOdooURL = new UriBuilder(Url.Combine(odooURL, "survey"));
                    if (ext.Contains("css") || ext.Contains("js"))
                    {
                        fullOdooURL = new UriBuilder(Url.Combine(odooURL, "survey", context.Request.Path));
                    }
                    var proxyReq = context.ForwardTo(fullOdooURL.Uri).AddXForwardedHeaders();                    
                    return proxyReq.Send();
                });
            });

            if (Tools.GetLogActivation(Configuration))
            {
                app.UseServerSideAnalytics(new ApiAnalyticStore(Configuration))                    
                    .LimitToPath(new string[] { "/ESS","/Site/Auth" });
            }

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{area}/{controller}/{action}",
                    new { area="Site",controller = "Auth", action = "Index"}
                );

                routes.MapRoute(
                    name: "param1",
                    template: "{area}/{controller}/{action}/{token}",
                    new { area = "Site", controller = "Auth", action = "Index", token="" }
                );

                routes.MapRoute(
                    name: "param2",
                    template: "{area}/{controller}/{action}/{source}/{id}",
                    new { area = "Site", controller = "Auth", action = "Index", source = "", id = "" }
                );

                routes.MapRoute(
                    name: "param3",
                    template: "{area}/{controller}/{action}/{source}/{id}/{x}",
                    new { area = "Site", controller = "Auth", action = "Index", source = "", id = "", x="" }
                );

                routes.MapRoute(
                    name: "param4",
                    template: "{area}/{controller}/{action}/{source}/{id}/{x}/{y}",
                    new { area = "Site", controller = "Auth", action = "Index", source = "", id = "", x = "", y="" }
                );
            });            

            // Load up watcher
            // Require Mongo 3.6 and up and replicate set active.
            //var streamChanges = app.ApplicationServices.GetService<IMongoChangeStream>();
            //{

            //    var funnelHub = app.ApplicationServices.GetService<IHubContext<FunnelHub>>();
            //    var db = app.ApplicationServices.GetService<IMongoManager>().Database();
            //    FunnelHub.RegisterWatcher(funnelHub, streamChanges);
            //    FunnelHub.StartWorker(funnelHub, db);
            //}
            //{
            //    var themeHub = app.ApplicationServices.GetService<IHubContext<ThemeOfficeHub>>();
            //    ThemeOfficeHub.RegisterWatcher(themeHub, streamChanges);
            //}
        }
    }
}
