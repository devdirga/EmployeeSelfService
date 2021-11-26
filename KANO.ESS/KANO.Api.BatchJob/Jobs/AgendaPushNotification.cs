using Aspose.Words.Lists;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KANO.Api.BatchJob.Jobs
{
    class AgendaPushNotification : BackgroundService, IJobService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private ComplaintAdapter _adapter;

        public AgendaPushNotification(IMongoManager mongo, IConfiguration configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = configuration;
            _adapter = new ComplaintAdapter(Configuration);
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
            var dateRange = new DateRange(DateTime.Now, DateTime.Now.Add(TimeSpan.FromDays(7)));
            var agendas = _adapter.GetAgenda(dateRange);
            
            var AXIDs = new List<long>();
            foreach (var agenda in agendas) {
                AXIDs.Add(agenda.AXID);
            }

            var agendaHasMap = new Dictionary<long, Agenda>();
            var existingAgendas = DB.GetCollection<Agenda>().Find(x=> AXIDs.Contains(x.AXID)).ToList();
            foreach (var existingAgenda in existingAgendas) {
                agendaHasMap[existingAgenda.AXID] = existingAgenda;
            }
            

            //if (requests.Count > 0)
            //{
            //    Console.WriteLine($"{DateTime.Now} Crunching {requests.Count} travel request(s)");
            //}

            var workflowTracker = new WorkFlowTrackingAdapter(Configuration);
            var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
            var allEmployeeIDs = DB.GetCollection<User>().Find(x=>true).Project(x=>x.Username).ToList();

            Parallel.ForEach(agendas, options, agenda => {
                Agenda existingAgenda = null;
                var upsert = false;
                var notificationReceiver = new List<string>();
                if (agendaHasMap.TryGetValue(agenda.AXID, out existingAgenda))
                {
                    if (agenda.Hash != existingAgenda.Hash)
                    {
                        upsert = true;
                        var recipients = agenda.EmployeeRecipients.Except(existingAgenda.EmployeeRecipients);
                        notificationReceiver.AddRange(recipients);
                    }
                }
                else
                {
                    upsert = true;
                    if (agenda.AgendaFor == KESSCNTServices.KESSAgendaForType.All)
                    {
                        notificationReceiver.AddRange(allEmployeeIDs);
                    }
                    else
                    {
                        notificationReceiver.AddRange(agenda.EmployeeRecipients);
                    }
                }

                if (upsert) {
                    DB.Save(agenda);
                }

                foreach (var receiver in notificationReceiver) {
                    if (!string.IsNullOrWhiteSpace(receiver))
                    {
                        new Notification(Configuration, DB).Create(
                                    receiver,
                                    $"{agenda.Name}",
                                    Notification.DEFAULT_SENDER,
                                    NotificationModule.TRAVEL,
                                    NotificationAction.APPROVE,
                                    NotificationType.Info
                                ).Send();
                    }
                }
                              
            });
        }
    }    
}
