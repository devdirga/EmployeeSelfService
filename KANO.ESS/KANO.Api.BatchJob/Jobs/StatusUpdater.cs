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
    class StatusUpdater : BackgroundService, IJobService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;

        private Employee _employee;
        private Certificate _certificate;
        private Family _family;
        private Document _document;
        private DocumentRequest _documentRequest;
        private Leave _leave;
        private TimeAttendance _timeAttendance;
        private Travel _travel;
        private MedicalBenefit _benefit;
        private Retirement _retirement;
        private Recruitment _recruitment;
        private TicketRequest _ticket;
        private VoucherRequestDetail _voucher;

        public StatusUpdater(IMongoManager mongo, IConfiguration configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = configuration;

            _employee = new Employee(DB, Configuration);
            _certificate = new Certificate(DB, Configuration);
            _family = new Family(DB, Configuration);
            _document = new Document(DB, Configuration);
            _documentRequest = new DocumentRequest(DB, Configuration);
            _leave = new Leave(DB, Configuration);
            _timeAttendance = new TimeAttendance(DB, Configuration);
            _travel = new Travel(DB, Configuration);
            _benefit = new MedicalBenefit(DB, Configuration);
            _retirement = new Retirement(DB, Configuration);
            _recruitment = new Recruitment(DB, Configuration);
            _ticket = new TicketRequest(DB, Configuration);
            _voucher = new VoucherRequestDetail(DB, Configuration);
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
            var tasks = new List<Task>();
            var requests = DB.GetCollection<UpdateRequest>()
                .Find(x => x.Status == UpdateRequestStatus.InReview)
                .SortByDescending(x => x.CreatedDate).ToList();

            if (requests.Count > 0)
            {
                Console.WriteLine($"{DateTime.Now} Crunching {requests.Count} request(s)");
            }

            var workflowTracker = new WorkFlowTrackingAdapter(Configuration);
            var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
            Parallel.ForEach(requests, options, request =>
            {
                UpdateRequestStatus status = UpdateRequestStatus.InReview;
                if (!string.IsNullOrWhiteSpace(request.AXRequestID))
                {
                    var axCurrentStatus = workflowTracker.GetCurrentSatus(request.EmployeeID, request.AXRequestID);
                    switch (request.Module)
                    {
                        case UpdateRequestModule.EMPLOYEE_RESUME:
                            _employee.StatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.EMPLOYEE_CERTIFICATE:
                            _certificate.SatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.EMPLOYEE_FAMILY:

                            _family.SatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.TRAVEL_REQUEST:

                            _travel.SatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.MEDICAL_BENEFIT:
                            _benefit.SatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.EMPLOYEE_DOCUMENT:
                            //status = employeeAdapter.CheckRequest(request.AXRequestID);                                
                            //document.StatusUpdater(request, status);
                            break;
                        case UpdateRequestModule.DOCUMENT_REQUEST:
                            //status = employeeAdapter.CheckRequest(request.AXRequestID);
                            //documentRequest.StatusUpdater(request, status);
                            break;
                        case UpdateRequestModule.LEAVE:                            
                            _leave.StatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.RETIREMENT_REQUEST:                            
                            _retirement.StatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.UPDATE_TIMEATTENDANCE:
                            _timeAttendance.StatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.RECRUITMENT_REQUEST:
                            _recruitment.StatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.COMPLAINT:
                            _ticket.StatusUpdater(request, axCurrentStatus);
                            break;
                        case UpdateRequestModule.CANTEEN_VOUCHER:
                            _voucher.StatusUpdater(request, axCurrentStatus);
                            break;
                    }
                }
            });            
        }
    }
}
