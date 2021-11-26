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
    class ApplicationUpdater : BackgroundService, IJobService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Application _Application;
        private RecruitmentAdapter _RecruitmentAdapter;

        public ApplicationUpdater(IMongoManager mongo, IConfiguration configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = configuration;
            _Application = new Application(DB, Configuration);
            _RecruitmentAdapter = new RecruitmentAdapter(Configuration);
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
            var requests = DB.GetCollection<Application>()
                .Find(x =>  x.ApplicationStatus != KESSHRMServices.HRMApplicationStatus.Employed &&
                            x.ApplicationStatus != KESSHRMServices.HRMApplicationStatus.Rejection &&
                            x.ApplicationStatus != KESSHRMServices.HRMApplicationStatus.Withdraw)
                .ToList();

            if (requests.Count > 0)
            {
                Console.WriteLine($"{DateTime.Now} Crunching {requests.Count} Application request(s)");
            }

            var workflowTracker = new WorkFlowTrackingAdapter(Configuration);
            var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };

            Parallel.ForEach(requests, options, request => {
                var ApplicationAX = _RecruitmentAdapter.GetApplication(request.EmployeeID, request.RecruitmentID);
                
                if (request.ApplicationStatus != ApplicationAX.ApplicationStatus) {

                    string status = "";
                    NotificationType notificationType = NotificationType.Info;
                    switch (ApplicationAX.ApplicationStatus)
                    {
                        case KESSHRMServices.HRMApplicationStatus.Received:
                        case KESSHRMServices.HRMApplicationStatus.Confirmed:
                            status = $"Your application for recruitment {request.RecruitmentID} is {ApplicationAX.ApplicationActionDescription}";
                            break;
                        case KESSHRMServices.HRMApplicationStatus.Interview:
                            //status = $"You are invited to interview for recruitment {request.RecruitmentID}";
                            break;
                        case KESSHRMServices.HRMApplicationStatus.TesAdministrasi:
                        case KESSHRMServices.HRMApplicationStatus.TesPotensiAkademik:
                        case KESSHRMServices.HRMApplicationStatus.TesPsikologi:
                        case KESSHRMServices.HRMApplicationStatus.MedicalCheckUp:
                            break;
                        case KESSHRMServices.HRMApplicationStatus.Rejection:
                            status = $"Your application for recruitment {request.RecruitmentID} is Rejected";
                            notificationType = NotificationType.Error;
                            break;
                        case KESSHRMServices.HRMApplicationStatus.Withdraw:
                            status = $"You have withdrawn from recruitment {request.RecruitmentID}";
                            notificationType = NotificationType.Warning;
                            break;                           
                        case KESSHRMServices.HRMApplicationStatus.Employed:
                            status = $"Congratulation you are employed for recruitment {request.RecruitmentID}";
                            notificationType = NotificationType.Success;
                            break;
                        default:
                            break;
                    }

                    if (!string.IsNullOrWhiteSpace(status))
                    {
                        new Notification(Configuration, DB).Create(
                                request.EmployeeID,
                                status,
                                Notification.DEFAULT_SENDER,
                                NotificationModule.APPLICATION,
                                NotificationAction.NONE,
                                notificationType
                            ).Send();
                    }
                    
                    request.AXID = ApplicationAX.AXID;
                    request.ApplicationStatus = ApplicationAX.ApplicationStatus;
                    request.AddHistory(request.ApplicationStatus);
                    DB.Save(request);
                }                
            });
        }
    }    
}
