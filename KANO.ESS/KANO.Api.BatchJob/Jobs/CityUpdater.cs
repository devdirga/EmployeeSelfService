using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KANO.Api.BatchJob.Jobs
{
    class CityUpdater : BackgroundService, IJobService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;        

        public CityUpdater(IMongoManager mongo, IConfiguration configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = configuration;            
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {          
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    this.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occured while running service {e}");
                }

                await Task.Delay(5000, stoppingToken);
            }
        }

        public void Run()
        {            
            //var n = 6;
            //var employeeAdapter = new EmployeeAdapter(Configuration);

            //var updateOptions = new UpdateOptions();
            //updateOptions.IsUpsert = true;
            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    var cities = employeeAdapter.GetCities();
            //    foreach (var city in cities) {                                       
            //        this.Db.GetCollection<City>().UpdateMany(
            //            x => x.AXID == city.AXID,
            //            Builders<City>.Update
            //                .Set(d => d.AXID, city.AXID)
            //                .Set(d => d.Description, city.Description)
            //                .Set(d => d.Name, city.Name),
            //            updateOptions
            //        );
            //    }

            //    // Run Every N-Hours
            //    await Task.Delay(n * 3600 * 1000, stoppingToken);
            //}
        }
    }    
}
