using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using KANO.Core.Lib.Extension;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KANO.Api.Gateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            Console.WriteLine($"PID : {Process.GetCurrentProcess().Id}");
            return WebHost.CreateDefaultBuilder(args)
               .ConfigureAppConfiguration((host, config) => {
                   var env = host.HostingEnvironment;

                   // find the shared folder in the parent folder
                   var sharedFolder = Path.Combine(env.ContentRootPath, "..");                   

                   config.SetBasePath(host.HostingEnvironment.ContentRootPath)
                    .AddJsonFile(AppWebHost.APP_CONFIG, true, true)
                    .AddJsonFile(Path.Combine(sharedFolder, AppWebHost.SHARED_CONFIG), true, true)
                    .AddJsonFile("route.json", true, true)
                    .AddEnvironmentVariables();
               })
               .UseKestrel()
               //.UseIISIntegration()
               .UseStartup<Startup>();
        }                        

    }
}
