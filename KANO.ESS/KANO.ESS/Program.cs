using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KANO.ESS.WebApp
{
    public class Program
    {
        static IMongoManager mongo;
        public static void Main(string[] args)
        {
            SetLicense();
            CreateWebHostBuilder(args).Build().Run();
        }

        public static void SetLicense()
        {
            //Aspose.Cells.License lic = new Aspose.Cells.License();
            //lic.SetLicense("FunmanCore.License.Aspose.Total.lic");

            string LicensePath = Path.Combine(Directory.GetCurrentDirectory(), "License", "Aspose.Total.lic");
            Aspose.Cells.License lic = new Aspose.Cells.License();
            Aspose.Words.License lWord = new Aspose.Words.License();
            lic.SetLicense(LicensePath);
            lWord.SetLicense(LicensePath);
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            Console.WriteLine($"PID : {Process.GetCurrentProcess().Id}");
            return WebHost.CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((host, config) =>
                {
                    var env = host.HostingEnvironment;

                    // find the shared folder in the parent folder
                    var sharedFolder = Path.Combine(env.ContentRootPath, "..");

                    //load the SharedSettings first, so that appsettings.json overrwrites it
                    config
                        .AddJsonFile(Path.Combine(sharedFolder, AppWebHost.SHARED_CONFIG), false, true)
                        .AddJsonFile(AppWebHost.APP_CONFIG, true)
                        .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);
                    config.AddEnvironmentVariables();
                })
                .UseKestrel()
                .UseIISIntegration()
                .UseStartup<Startup>();
        }
    }
}
