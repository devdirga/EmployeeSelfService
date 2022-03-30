using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KANO.Api.Notification.Hubs;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json.Serialization;
using KANO.Api.Notification.Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace KANO.Api.Notification
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
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.Configure<MongoManagerOptions>(options =>
            {
                var list = new Dictionary<string, MongoManagerConnectionInfo>();
                foreach (var con in Configuration.GetSection("MongoDB:ConnectionLists").GetChildren())
                {
                    list.Add(con.Key, new MongoManagerConnectionInfo(con["ConnectionString"]));
                }

                options.DefaultConnection = Configuration["MongoDB:DefaultConnection"];
                options.ConnectionList = list;
            });

            services.AddMongoManager();
            services.AddMongoChangeStream();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(cfg =>
            {
                cfg.RequireHttpsMetadata = false;
                cfg.SaveToken = true;
                //cfg.SecurityTokenValidators.Clear();
                cfg.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidIssuer = Configuration["Tokens:Issuer"],
                    ValidAudience = Configuration["Tokens:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["Tokens:Key"])),
                    RequireExpirationTime = false,
                    ClockSkew = TimeSpan.Zero
                };
            });


            services.Configure<JsonHubProtocolOptions>(options =>
            {
                options.PayloadSerializerSettings.ContractResolver = new DefaultContractResolver();
            });

            services.Configure<WebSocketOptions>(options =>
            {
                options.ReceiveBufferSize = 1024 * 1024;
            });

            services.AddSignalR(o=> {
                o.EnableDetailedErrors = true;
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
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

            app.UseAuthentication();

            // custom jwt auth middleware
            app.UseMiddleware<JwtMiddleware>();

            //app.UseHttpsRedirection();
            app.UseCors(builder => builder
                .AllowAnyHeader()
                .AllowAnyMethod()
                .SetIsOriginAllowed((host) => true)
                .AllowCredentials()
            );

            app.UseSignalR(routes =>
            {
                var hub = Configuration["Request:NotificationHub"];
                if (string.IsNullOrWhiteSpace(hub)) {
                    hub = "/api/notification-hub";
                    Console.WriteLine($"Unable to find notification hub config, setting as default : {hub}");
                }

                routes.MapHub<NotificationHub>(hub, options=> {
                    options.TransportMaxBufferSize = 1024 * 1024;
                    options.ApplicationMaxBufferSize = 1024 * 1024;
                });
            });
            app.UseMvc();
        }
    }
}
