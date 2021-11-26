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
    class TravelUpdater : BackgroundService, IJobService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Travel _travel;
        private TravelAdapter _travelAdapter;

        public TravelUpdater(IMongoManager mongo, IConfiguration configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = configuration;
            _travel = new Travel(DB, Configuration);
            _travelAdapter = new TravelAdapter(Configuration);
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
            var requests = DB.GetCollection<Travel>()
                .Find(x => x.AXRequestID == "" || x.AXRequestID == null)
                .ToList();

            if (requests.Count > 0)
            {
                Console.WriteLine($"{DateTime.Now} Crunching {requests.Count} travel request(s)");
            }

            var workflowTracker = new WorkFlowTrackingAdapter(Configuration);
            var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };

            Parallel.ForEach(requests, options, request => {
                var travelAX = _travelAdapter.GetTravelByRecId(request.AXID);
                
                if (request.TravelRequestStatus != travelAX.TravelRequestStatus) {

                    string status = "";
                    switch (travelAX.TravelRequestStatus)
                    {
                        case KESSTEServices.KESSTrvExpTravelReqStatus.Created:
                        case KESSTEServices.KESSTrvExpTravelReqStatus.Revision:
                            break;
                        case KESSTEServices.KESSTrvExpTravelReqStatus.Canceled:
                        case KESSTEServices.KESSTrvExpTravelReqStatus.Verified:
                        case KESSTEServices.KESSTrvExpTravelReqStatus.Closed:
                            status = travelAX.TravelRequestStatus.ToString();
                            break;                        
                        default:
                            break;
                    }

                    if (!string.IsNullOrWhiteSpace(status))
                    {
                        new Notification(Configuration, DB).Create(
                                request.EmployeeID,
                                $"Travel request is {status}",
                                Notification.DEFAULT_SENDER,
                                NotificationModule.TRAVEL,
                                NotificationAction.APPROVE,
                                Notification.MapUpdateRequestStatusTravel(request.TravelRequestStatus)
                            ).Send();
                    }
                    else if (travelAX.TravelRequestStatus == KESSTEServices.KESSTrvExpTravelReqStatus.Revision) {
                        new Notification(Configuration, DB).Create(
                                request.EmployeeID,
                                $"Your travel request ({travelAX.TravelID}) need to be revised",
                                Notification.DEFAULT_SENDER,
                                NotificationModule.TRAVEL,
                                NotificationAction.APPROVE,
                                Notification.MapUpdateRequestStatusTravel(request.TravelRequestStatus)
                            ).Send();
                    }

                    if (string.IsNullOrWhiteSpace(request.AXRequestID) && !string.IsNullOrWhiteSpace(travelAX.AXRequestID)) {
                        travelAX.CreateUpdateRequest(travelAX.AXRequestID, this.DB, this.Configuration);
                    }

                    travelAX.Id = request.Id;
                    DB.Save(travelAX);
                }else if(string.IsNullOrWhiteSpace(request.AXRequestID) && !string.IsNullOrWhiteSpace(travelAX.AXRequestID)) {
                    travelAX.CreateUpdateRequest(travelAX.AXRequestID,this.DB, this.Configuration);
                    
                    travelAX.Id = request.Id;
                    DB.Save(travelAX);
                }                
            });
        }
    }    
}
