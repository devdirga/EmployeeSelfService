using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HealthChecks.Network;
using HealthChecks.Network.Core;
using HealthChecks.UI.Client;
using KANO.Core.Lib.Middleware;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Swashbuckle.AspNetCore.Swagger;
using MongoDB.Driver;

namespace KANO.Api.Gateway
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }        

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var mongoDBDefault = Configuration["MongoDB:DefaultConnection"];
            var mongoDBConnections = new Dictionary<string, MongoManagerConnectionInfo>();
            foreach (var con in Configuration.GetSection("MongoDB:ConnectionLists").GetChildren())
            {
                mongoDBConnections.Add(con.Key, new MongoManagerConnectionInfo(con["ConnectionString"]));
            }
            var mongoDBConnection = mongoDBConnections[mongoDBDefault];
            var mongoDBUri = new UriBuilder(mongoDBConnection?.ConnectionString);

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddOcelot(Configuration);
            services.Configure<MongoManagerOptions>(options =>
            {
                options.DefaultConnection = mongoDBDefault;
                options.ConnectionList = mongoDBConnections;
            });

            services.AddMongoManager();
            services.AddMongoChangeStream();


            // Health check
            var healthCheckBuilder = services
                .AddHealthChecks()
                .AddPingHealthCheck(
                    setup: (option) =>
                    {
                        option.AddHost(mongoDBUri.Host, 1000);
                    },
                    name: "mongodb network",
                    failureStatus: HealthStatus.Unhealthy
                )
                .AddMongoDb(
                    mongodbConnectionString: mongoDBConnection?.ConnectionString,
                    name: "mongodb",
                    failureStatus: HealthStatus.Unhealthy
                );

            // AX Connectivity
            foreach (var con in Configuration.GetSection("AX:ConnectionLists").GetChildren())
            {
                var credential = con.Get<Credential>();
                healthCheckBuilder.AddPingHealthCheck(
                    setup: (option) =>
                    {
                        option.AddHost(credential.Host, 1000);
                    },
                    name: $"AX network {con.Key.ToLower()}",
                    failureStatus: HealthStatus.Unhealthy
                );
            }
            

            healthCheckBuilder.AddSmtpHealthCheck(
                    setup: (option) =>
                    {
                        var username = Configuration["SMTP:SMTP_UserName_Default"];
                        var password = Configuration["SMTP:SMTP_Password_Default"];
                        if (username != "")
                        {
                            option.LoginWith(username, password);
                        }

                        option.Host = Configuration["SMTP:SMTP_Host_Default"];
                        option.Port = Convert.ToInt32(Configuration["SMTP:SMTP_Port_Default"]);
                        var isSSL = Convert.ToBoolean(Configuration["SMTP:SMTP_UseSSL_Default"]);
                        if (isSSL)
                        {
                            option.ConnectionType = SmtpConnectionType.SSL;
                        }
                    },
                    name: "smptp",
                    failureStatus: HealthStatus.Unhealthy
                );                

            var listUrl = new Dictionary<string, bool>();
            foreach (var con in Configuration.GetSection("ReRoutes").GetChildren()) {
                var ocelot = con.Get<OcelotConfiguration>();                
                if (ocelot.DownstreamScheme != "http" && ocelot.DownstreamScheme != "https") continue;

                var upstreamToken = ocelot.UpstreamPathTemplate.Split("/");
                if (ocelot.UpstreamPathTemplate.StartsWith("/")) {
                    upstreamToken = ocelot.UpstreamPathTemplate.Substring(1).Split("/");
                }
                var upstream = string.Join(' ',upstreamToken).Replace("{all}", "");
                
                var tmp = false;
                if (listUrl.TryGetValue(upstream, out tmp)) continue;
                
                if (ocelot.DownstreamHostAndPorts.Count > 0) {
                    listUrl.Add(upstream, true);

                    // Host and port prep 
                    var hostAndPort = ocelot.DownstreamHostAndPorts.ElementAt(0);                    
                    var hostAndPortString = $"{hostAndPort.Host}";
                    if (hostAndPort.Port > 0) {
                        hostAndPortString = $"{hostAndPort.Host}:{hostAndPort.Port}";
                    }

                    // Downstream prep
                    var downstream = ocelot.DownstreamPathTemplate.Replace("{all}", "");
                    if (!downstream.StartsWith("/")) {
                        downstream = "/" + downstream;
                    }
                    if (!downstream.EndsWith("/")) {
                        downstream = downstream + "/";
                    }

                    var url = new Uri($"{ocelot.DownstreamScheme}://{hostAndPortString}{downstream}ping");                    

                    healthCheckBuilder.AddUrlGroup(
                        uri: url,
                        name: $"{upstream} ({hostAndPort.Port})",
                        failureStatus: HealthStatus.Unhealthy
                        );
                }

            }            
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public async void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Health check
            app.UseHealthChecks("/hc", new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });           

            //app.UseHttpsRedirection();
            app.UseMvc();
            app.UseWebSockets();
            await app.UseOcelot();
        }
    }
}
