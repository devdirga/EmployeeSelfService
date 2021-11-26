using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Notifications")]
    [BsonIgnoreExtraElements]
    public class Notification : IMongoPreSave<Notification>
    {        
        [BsonIgnore]
        [JsonIgnore]
        private IConfiguration Configuration;
        [BsonIgnore]
        [JsonIgnore]
        private IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        public const string DEFAULT_SENDER = "system";

        [BsonId]
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public NotificationType Type { get; set; } = NotificationType.Info;
        public string Sender { get; set; }
        public string Receiver { get; set; }
        public string Module { get; set; }        
        public string Message { get; set; }
        public string Notes { get; set; }
        public bool Read { get; set; } = false;
        public List<string> Actions { get; set; } = new List<string>();
        public bool IsTask { get; set; } = false;
        public UpdateRequestStatus Status { get; set; } = UpdateRequestStatus.InReview;
        public InternalInformation Meta { get; set; } = new InternalInformation();

        public Notification(){}

        public Notification(IConfiguration config, IMongoDatabase db) {
            Configuration = config;
            MongoDB = db;
        }

        public Notification Create(
            string receiver, 
            string message, 
            string sender = DEFAULT_SENDER, 
            string module = NotificationModule.DEFAULT, 
            string action = NotificationAction.NONE, 
            NotificationType notificationType = NotificationType.Info
        ){
            this.Type = notificationType;
            this.Module = module;
            this.Message = message;
            this.Sender = sender;
            this.Receiver = receiver;
            this.Actions.Add(action);
            return this;
        }

        public Notification Create(
            string receiver,
            string message,
            string sender = DEFAULT_SENDER,
            string module = NotificationModule.DEFAULT,
            NotificationType notificationType = NotificationType.Info
        )
        {
            this.Type = notificationType;
            this.Module = module;
            this.Message = message;
            this.Sender = sender;
            this.Receiver = receiver;
            return this;
        }        

        public Notification Create(
            string receiver, 
            string message, 
            string module = NotificationModule.DEFAULT, 
            string action = NotificationAction.NONE
        ){            
            return Create(receiver, message, DEFAULT_SENDER, module, action, NotificationType.Info);
        }

        public Notification Create(
            string receiver,
            string message,
            string module = NotificationModule.DEFAULT,
            NotificationType notificationType = NotificationType.Info
        )
        {
            return Create(receiver, message, DEFAULT_SENDER, module, notificationType);
        }        

        public Notification Create(
            string receiver,
            string message,
            string module = NotificationModule.DEFAULT
        )
        {
            return Create(receiver, message, DEFAULT_SENDER, module, NotificationType.Info);
        }

        public Notification Create(string receiver, string message)
        {
            return Create(receiver, message, DEFAULT_SENDER, NotificationModule.DEFAULT, NotificationAction.NONE, NotificationType.Info);
        }

        public static NotificationType MapUpdateRequestStatus(UpdateRequestStatus status) {
            NotificationType notificationType;
            switch (status)
            {
                case UpdateRequestStatus.InReview:
                    notificationType = NotificationType.Info;
                    break;
                case UpdateRequestStatus.Rejected:
                    notificationType = NotificationType.Error;
                    break;
                case UpdateRequestStatus.Approved:
                    notificationType = NotificationType.Success;
                    break;
                case UpdateRequestStatus.Cancelled:
                    notificationType = NotificationType.Warning;
                    break;
                default:
                    notificationType = NotificationType.Info;
                    break;
            }

            return notificationType;
        }

        public static NotificationType MapUpdateRequestStatusTravel(KESSTEServices.KESSTrvExpTravelReqStatus status)
        {
            NotificationType notificationType;
            switch (status)
            {
                case KESSTEServices.KESSTrvExpTravelReqStatus.Created:
                    notificationType = NotificationType.Info;
                    break;
                case KESSTEServices.KESSTrvExpTravelReqStatus.Canceled:
                    notificationType = NotificationType.Error;
                    break;
                case KESSTEServices.KESSTrvExpTravelReqStatus.Verified:
                    notificationType = NotificationType.Success;
                    break;
                case KESSTEServices.KESSTrvExpTravelReqStatus.Revision:
                    notificationType = NotificationType.Warning;
                    break;
                case KESSTEServices.KESSTrvExpTravelReqStatus.Closed:
                    notificationType = NotificationType.Warning;
                    break;
                default:
                    notificationType = NotificationType.Info;
                    break;
            }

            return notificationType;
        }

        public void SendApprovals(string employeeID, string instanceID)
        {
            return;
            var updateRequest = new UpdateRequest(MongoDB, Configuration);
            var approvals = updateRequest.GetNextApproval(employeeID, instanceID);

            if (approvals != null && approvals.Any())
            {
                var options = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
                Parallel.ForEach(approvals, options, approval =>
                {
                    var description = "Approval request";
                    switch (approval.WorkflowType)
                    {                        
                        case KESSWFServices.KESSWorkflowType.TM:
                            description = "Absence recomendation approval request";
                            break;
                        case KESSWFServices.KESSWorkflowType.LM:
                            description = "Leave approval request";
                            break;
                        case KESSWFServices.KESSWorkflowType.TEReq:
                            description = "Travel approval request";
                            break;
                        case KESSWFServices.KESSWorkflowType.CR:
                            description = "Claim approval request";
                            break;                        
                        case KESSWFServices.KESSWorkflowType.MPP:
                            description = "Retirement approval request";
                            break;
                        case KESSWFServices.KESSWorkflowType.CNT:                            
                        case KESSWFServices.KESSWorkflowType.HRM:
                        default:
                            break;
                    }

                    description = description + $" from {approval.SubmitByEmployeeName}";

                    var client = new Client(Configuration);
                    var notification = this.Create(
                        approval.AssignToEmployeeID, // Receiver
                        description, // Message
                        approval.SubmitByEmployeID,
                        NotificationModule.REQUEST_APPROVAL, // Module
                        NotificationType.Info);
                    var request = new Request($"api/notification/send", Method.POST);

                    request.AddJsonParameter(notification);
                    var response = client.Execute(request);
                });
            }            
        }

        public Notification AddAction(string action) {
            this.Actions.Add(action);
            return this;
        }

        public Notification AddNotes(string notes)
        {
            this.Notes = notes;
            return this;
        }

        public bool Send()
        {            
            if (string.IsNullOrWhiteSpace(this.Receiver)) {
                return false;
            }

            MongoDB.Save<Notification>(this);

            var client = new Client(Configuration);
            var request = new Request($"api/notification/send", Method.POST);
            
            request.AddJsonParameter(this);

            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            return true;
        }

        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id)) {
                this.Id = ObjectId.GenerateNewId().ToString();
                this.Timestamp = DateTime.Now;
            }
            
        }
    }    

    public enum NotificationType : int
    {
        Info = 0,
        Success = 1,
        Warning = 2,
        Error = 3,        
    }

    public class  NotificationModule
    {
        public const string DEFAULT = "default";
        public const string AUTH = "auth";
        public const string DASHBOARD = "dashboard";
        public const string EMPLOYEE = "employee";
        public const string CERTIFICATE = "certificate";
        public const string FAMILY = "family";
        public const string TIME_MANAGEMENT = "time_management";
        public const string PAYROLL = "payroll";
        public const string LEAVE = "leave";
        public const string BENEFIT = "benefit";
        public const string TRAVEL = "travel";
        public const string SURVEY = "survey";
        public const string AGENDA = "agenda";
        public const string TRAINING_REGISTRATION = "training_registration";
        public const string RECRUITMENT_REQUEST = "recruitment_request";
        public const string APPLICATION = "application";
        public const string COMPLAINT = "complaint";
        public const string RETIREMENT = "retirement";
        public const string CANTEEN = "canteen";
        public const string REQUEST_APPROVAL = "request_approval";
    }

    public class NotificationAction
    {
        public const string NONE = "none";
        public const string OPEN_EMPLOYEE_PROFILE = "open_employee_profile";
        public const string OPEN_EMPLOYEE_FAMILY = "open_employee_family";
        public const string OPEN_EMPLOYEE_CERTIFICATE = "open_employee_certificate";
        public const string OPEN_EMPLOYEE_DOCUMENT = "open_employee_document";
        public const string OPEN_DOCUMENT_REQUEST = "open_document_request";        
        public const string OPEN_LEAVE = "open_leave";        
        public const string OPEN_TIME_MANAGEMENT = "open_time_management";        
        public const string APPROVE = "approve";        
        public const string REJECT = "reject";
        public const string DELEGATION = "delegation";
    }


}
