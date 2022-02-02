using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("UpdateRequest")]
    [BsonIgnoreExtraElements]
    public class UpdateRequest : BaseT, IMongoPreSave<UpdateRequest>
    {
        [BsonIgnore]
        [JsonIgnore]
        IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        IConfiguration Configuration;

        [BsonId]
        public string Id { get; set; }
        public string AXRequestID { get; set; }
        public string EmployeeID { get; set; }
        // Notes for updating
        public string Notes { get; set; }
        public string Description { get; set; }
        public string Module { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public UpdateRequestStatus Status { get; set; } = UpdateRequestStatus.InReview;
        public List<UpdateRequestHistory> Histories { get; set; } = new List<UpdateRequestHistory>();

        public UpdateRequest() { }

        public UpdateRequest(IMongoDatabase db) {
            MongoDB = db;
        }

        public UpdateRequest(IMongoDatabase db, IConfiguration configuration)
        {
            MongoDB = db;
            Configuration = configuration;
        }

        public void Create(string AXRequestID, string employeeID, string module) {
            this.Id = ObjectId.GenerateNewId().ToString();
            this.Module = module;
            this.EmployeeID = employeeID;
            this.AXRequestID = AXRequestID;
        }

        public void Create(string AXRequestID, string employeeID, string module, string description)
        {
            this.Create(AXRequestID, employeeID, module);
            this.Description = description;
        }

        public void Create(string AXRequestID, string employeeID, string module, string description, string notes)
        {
            this.Create(AXRequestID, employeeID, module, description);
            this.Notes = notes;
        }

        public UpdateRequest Current(string employeeID, string module)
        {
            return MongoDB.GetCollection<UpdateRequest>().Find(x => x.EmployeeID == employeeID && x.Module == module).SortByDescending(x => x.LastUpdate).FirstOrDefault();
        }

        public void AddHistory(string employeeID, string employeeName, UpdateRequestStatus status, string notes = "") {
            this.Histories.Add(new UpdateRequestHistory {
                Id = ObjectId.GenerateNewId().ToString(),
                RecordDate = DateTime.Now,
                Notes = notes,
                EmployeeID = employeeID,
                EmployeeName = employeeName,
                Status = status,

            });
        }

        public UpdateRequest Get(string employeeID, string axRequestID) {
            var result = MongoDB.GetCollection<UpdateRequest>()
                .Find(x => x.EmployeeID == employeeID && x.AXRequestID==axRequestID);

            return result.FirstOrDefault();
        }

        public List<UpdateRequest> GetS(string employeeID, int limit = 10, int offset = 0)
        {
            var result = MongoDB.GetCollection<UpdateRequest>()
                .Find(x => x.EmployeeID == employeeID)
                .Limit(limit)
                .Skip(offset)
                .SortByDescending(x => x.CreatedDate);

            if (result.Any()) {
                return result.ToList();
            }

            return new List<UpdateRequest>();
        }

        public long GetTotal(string employeeID)
        {
            return MongoDB.GetCollection<UpdateRequest>()
                .CountDocuments(x => x.EmployeeID == employeeID);
        }

        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id))
            {
                this.Id = ObjectId.GenerateNewId().ToString();

            }

            if (this.CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.LastUpdate = DateTime.Now;
        }

        public string GenerateTitle(KESSWFServices.KESSWorkflowType workflowType) {
            var title = "";
            switch (workflowType)
            {
                case KESSWFServices.KESSWorkflowType.HCM:
                    title = "Request Update Resume";
                    break;
                case KESSWFServices.KESSWorkflowType.TM:
                    title = "Absence Recomendation";
                    break;
                case KESSWFServices.KESSWorkflowType.LM:
                    title = "Leave Request";
                    break;
                case KESSWFServices.KESSWorkflowType.TEReq:
                    title = "SPPD Travel request";
                    break;
                default:
                    title = "Request";
                    break;
            }
            return title;
        }

        public Dictionary<string, string> GetTitles(IEnumerable<string> instanceIDs) {
            var result = new Dictionary<string, string>();

            var updateRequests = MongoDB.GetCollection<UpdateRequest>()
                .Find(x => instanceIDs.Contains(x.AXRequestID))
                .Project<UpdateRequest>("{Description:1, AXRequestID:1}")
                .ToList();

            foreach (var ur in updateRequests) {
                result[ur.AXRequestID] = ur.Description;
            }            

            return result;
        }

        public List<TrackingRequest> GetDetail(string employeeID, string instanceID = "") {
            var adapter = new WorkFlowTrackingAdapter(Configuration);
            var result = new List<WorkFlow>();
            
            if (!string.IsNullOrWhiteSpace(instanceID))
                result = adapter.GetUpdateRequestsByInstanceID(employeeID,instanceID);
            else
                result = adapter.GetUpdateRequests(employeeID);

            return groupUpdateRequest(result);            

        }

        public List<TrackingRequest> GetDetailRange(string employeeID, DateRange range) {
            var adapter = new WorkFlowTrackingAdapter(Configuration);                    
            var workflows = adapter.GetUpdateRequestsRange(employeeID, range);
            return groupUpdateRequest(workflows);            
        }

        private List<TrackingRequest> groupUpdateRequest(List<WorkFlow> workflows){            
            // Group By instance ID
            //result.RemoveAll(x => x.AssignType == KESSWFServices.KESSWorkflowAssignType.Originator);            

            var trackingRequests = workflows.GroupBy(g1 => new
            {
                g1.InstanceID,                
            }).Select(gw1 =>
            {
                // Group By Sequence and StepTrackingTpe
                var groupedWorkflow = gw1
                    .ToList()
                    .GroupBy(g2=> new{
                        g2.StepSequence,
                        g2.StepTrackingType,
                        g2.StepName,
                    }).Select(gw2=> {                        
                        var gw2Result = gw2.ToList().Find(x=>x.ActionApprove || x.ActionCancel || x.ActionDelegate || x.ActionReject);
                        if (gw2Result == null) {
                            var gw2NonOriginator = gw2.ToList().Find(x => x.AssignType != KESSWFServices.KESSWorkflowAssignType.Originator);
                            if (gw2NonOriginator == null)
                            {
                                gw2Result = gw2.First();
                            }
                            else 
                            {
                                gw2Result = gw2NonOriginator;
                            }
                            
                        }

                        return gw2Result;
                    });


                var defaultWorkflow = groupedWorkflow                    
                    .OrderByDescending(x=>x.CompleteDateTime)
                    .ToList()
                    .FindLast(x => x.TrackingStatus > 0);

                if (defaultWorkflow == null)
                {
                    defaultWorkflow = gw1.First();
                }

                return new TrackingRequest(defaultWorkflow)
                {
                    WorkFlows = groupedWorkflow.ToList()
                };
            }).ToList();

            // Getting update request title
            var instanceIDs = trackingRequests.Select(x => x.InstanceID).Distinct();
            var mapTitle = this.GetTitles(instanceIDs);
            foreach (var tr in trackingRequests) {
                var title = "";
                if (!mapTitle.TryGetValue(tr.InstanceID, out title)) {
                    tr.Title = GenerateTitle(tr.WorkflowType);
                    continue;
                }
                tr.Title = title;
            }
            
            return trackingRequests.OrderByDescending(x => x.SubmitDateTime).ToList();
        }

        public List<WorkFlow> GetNextApproval(string employeeID, string instanceID)
        {
            var adapter = new WorkFlowTrackingAdapter(Configuration);
            var result = adapter.GetUpdateRequest(employeeID, instanceID);

            var adapterWF = new WorkFlowTrackingAdapter(Configuration);
            List<WorkFlow> resultwf = new List<WorkFlow>();

            var options = new ParallelOptions() { MaxDegreeOfParallelism = 20 };
            Parallel.ForEach(result, options, workflow =>
            {
                var wfassigments = adapterWF.GetAssignment(workflow.AssignToEmployeeID, true).Find(x => x.InstanceId == workflow.InstanceID);
                if (wfassigments != null)
                {
                    if ((wfassigments.AssignApprove || wfassigments.AssignReject) &&
                            (wfassigments.ActionApprove == false ||
                            wfassigments.ActionCancel == false ||
                            wfassigments.ActionReject == false ||
                            wfassigments.ActionDelegate == false))
                    {

                        resultwf.Add(workflow);

                    }
                }
            });
            
            return resultwf;

            //return result.FindAll(x => x.StepTrackingType == KESSWFServices.KESSWorkflowTrackingType.Approval && (x.ActionApprove || x.ActionReject));
        }

        public UpdateRequest GetByInstanceID(string axRequestID)
        {
            var result = MongoDB.GetCollection<UpdateRequest>()
                .Find(x => x.AXRequestID == axRequestID);
            return result.FirstOrDefault();
        }
    }

    public class UpdateRequestHistory {
        public string Id { get; set; }
        public DateTime RecordDate { get; set; } = DateTime.Now;
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string Notes { get; set; }
        public string Data { get; set; }
        public UpdateRequestStatus Status { get; set; } = UpdateRequestStatus.InReview;        
    }
    
    public enum UpdateRequestStatus : int
    {
        InReview = 0, Approved = 1, Cancelled = 2, Rejected = 3
    }

    public class UpdateRequestModule {
        public const string EMPLOYEE_RESUME = "employee_resume";
        public const string EMPLOYEE_DOCUMENT = "employee_document";
        public const string EMPLOYEE_DOCUMENT_REQUEST = "employee_document_request";
        public const string EMPLOYEE_CERTIFICATE = "employee_certificate";
        public const string EMPLOYEE_FAMILY = "employee_family";
        public const string DOCUMENT_REQUEST = "document_request";
        public const string LEAVE = "leave";
        public const string LOAN_REQUEST = "loan_request";
        public const string ABSENCE_RECOMMENDATION = "absence_recommendation";
        public const string UPDATE_TIMEATTENDANCE = "update_timeattendance";
        public const string MEDICAL_BENEFIT = "medical_benefit";
        public const string TRAVEL_REQUEST = "travel_request";
        public const string TRAINING_REGISTRATION = "training_registration";
        public const string TRAINING_REQUEST = "training_request";
        public const string RETIREMENT_REQUEST = "retirement_request";
        public const string RECRUITMENT_REQUEST = "recruitment_request";
        public const string APPLICATION_REQUEST = "application_request";
        public const string COMPLAINT = "complaint";
        public const string CANTEEN_VOUCHER = "canteen_voucher";
    }

    public class UpdateRequestNotes
    {
        public const string NOTE_UPDATE_TIMEATTENDANCE = "Update Time Attendance";

    }

    public class DeleteForm {
        public string Id { set; get; }
        public string EmployeeID { set; get; }
        public string Reason { set; get; }
    }
}
