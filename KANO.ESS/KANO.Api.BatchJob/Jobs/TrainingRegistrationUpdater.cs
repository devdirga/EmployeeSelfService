using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KANO.Api.BatchJob.Jobs
{
    class TrainingRegistrationUpdater : BackgroundService, IJobService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Training _travel;
        private TrainingAdapter _trainingAdapter;

        public TrainingRegistrationUpdater(IMongoManager mongo, IConfiguration configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = configuration;
            _travel = new Training(DB, Configuration);
            _trainingAdapter = new TrainingAdapter(Configuration);
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

                await Task.Delay(10000, stoppingToken);
            }
        }

        public void Run()
        {            
            var range = Tools.normalizeFilter(new DateRange(DateTime.Now.AddMonths(-1), DateTime.Now));
            var registrationCount = DB.GetCollection<TrainingRegistration>()
                    //.CountDocuments(x => (x.Training.Schedule.Start >= range.Start && x.Training.Schedule.Start <= range.Finish) || (x.Training.Schedule.Finish >= range.Start && x.Training.Schedule.Finish <= range.Finish));
                    .CountDocuments(x => true);
            
            var i = 0;
            var limit = 50;
            while (i < registrationCount) {
                var registrations = DB.GetCollection<TrainingRegistration>()
                    //.Find(x => (x.Training.Schedule.Start >= range.Start && x.Training.Schedule.Start <= range.Finish) || (x.Training.Schedule.Finish >= range.Start && x.Training.Schedule.Finish <= range.Finish))
                    .Find(x => true)
                    .Skip(i)
                    .Limit(limit)
                    .ToList();

                if (registrations.Count() > 0)
                {
                    Console.WriteLine($"{DateTime.Now} Crunching {registrations.Count()} training registration(s)");
                }

                var workflowTracker = new WorkFlowTrackingAdapter(Configuration);
                var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };

                Parallel.ForEach(registrations, options, registration =>
                {
                    var updatedRegistration = _trainingAdapter.GetRegistration(registration.EmployeeID, registration.TrainingID);
                    registration.SatusUpdater(DB, Configuration, updatedRegistration);
                });

                i += limit;
            }

            
        }
    }    
}
