using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KANO.Api.Administration
{
    public class Startup
    {
        private IHostingEnvironment CurrentEnvironment{ get; set; } 

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
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

            //app.UseHttpsRedirection();
            app.UseMvc();

            //Initialize Page and group Collections
            var mongoOptions = new MongoManagerOptions();
            mongoOptions.DefaultConnection = Configuration["MongoDB:DefaultConnection"];
            mongoOptions.ConnectionList = new Dictionary<string, MongoManagerConnectionInfo>();
            foreach (var con in Configuration.GetSection("MongoDB:ConnectionLists").GetChildren())
            {
                mongoOptions.ConnectionList.Add(con.Key, new MongoManagerConnectionInfo(con["ConnectionString"]));
            }
            var mongo = new MongoManager(mongoOptions);
            var db = mongo.Database();
            
            string envName = CurrentEnvironment.EnvironmentName;
            var pages = Page.Init(db, envName == "Development");
            Group.Init(db, pages);

            // initialize development user
            if(envName == "Development"){
                User.Init(db);
            }
        }
    }
}
