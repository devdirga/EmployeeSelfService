using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Extension
{    
    public static class AppWebHost
    {
        public const string APP_CONFIG = "appsettings.json";
        public const string SHARED_CONFIG = "sharedsettings.json";

        public static IWebHostBuilder CreateBuilder(string[] args)
        {
            Console.WriteLine($"PID : {Process.GetCurrentProcess().Id}");
            return WebHost.CreateDefaultBuilder(args)                
                .UseKestrel()
                .UseIISIntegration()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((host, config) =>
                {
                    var env = host.HostingEnvironment;

                    // find the shared folder in the parent folder
                    var sharedFolder = Path.Combine(env.ContentRootPath, "..");

                    //load the SharedSettings first, so that appsettings.json overrwrites it
                    config
                        .AddJsonFile(Path.Combine(sharedFolder, SHARED_CONFIG), false, true)
                        .AddJsonFile(APP_CONFIG, true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

                    config.AddEnvironmentVariables();
                });
        }

    }
}
