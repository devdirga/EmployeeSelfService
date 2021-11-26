using KANO.Core.Lib.Extension;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{    
    public class WorkFlowAssignment
    {
        [BsonIgnore]
        [JsonIgnore]
        public static readonly int[] InvertedWorkflowType = { (int) KESSWFServices.KESSWorkflowType.CNT };

        [BsonIgnore]
        [JsonIgnore]
        IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        IConfiguration Configuration;

        public bool ActionApprove { set; get; }
        public bool ActionCancel{set; get;}
        public string Title{set; get;}
        public string Comment{set; get;}
        public DateTime ActionDateTime{set; get;}
        public bool ActionDelegate{set; get;}
        public string ActionDelegateToEmployeeID{set; get;}
        public string ActionDelegateToEmployeeName{set; get;}
        public bool ActionReject{set; get;}
        public bool AssignApprove{set; get;}
        public bool AssignCancel{set; get;}
        public bool AssignDelegate{set; get;}
        public bool AssignReject{set; get;}
        public KESSWFServices.KESSWorkflowAssignType AssignType {set; get;}
        public string AssignTypeDescription {set; get;}
        public int Sequence {set; get;}
        public string ColorGroup {set; get;}
        public KESSWFServices.KESSWorkflowTrackingType StepTrackingType {set; get;}
        public string StepTrackingTypeDescription {set; get;}
        public string AssignToEmployeeID{set; get;}
        public string AssignToEmployeeName{set; get;}
        public KESSWFServices.KESSWorkerRequestType RequestType { set; get; }
        public string RequestTypeDescription { set; get; }
        public TaskType TaskType { set; get; } = TaskType.Approval;
        public KESSWFServices.KESSWorkflowTrackingStatus TrackingStatus { set; get; }
        public string TrackingStatusDescription { set; get; }
        public string InstanceId{set; get;}
        public string SubmitEmployeeID{set; get;}
        public string SubmitEmployeeName{set; get;}
        public DateTime SubmitDateTime{set; get;}
        public long AXID{set; get;}
        public string WorkflowId{set; get;}
        public KESSWFServices.KESSWorkflowType WorkflowType {set; get;}
        public string WorkflowTypeDescription {set; get;}
        public bool Inverted {
            get{
                return Array.IndexOf(InvertedWorkflowType, (int) WorkflowType) > -1;
            }
        }

        public WorkFlowAssignment()
        {
            
        }

        public WorkFlowAssignment(IMongoDatabase db, IConfiguration configuration)
        {
            MongoDB = db;
            Configuration = configuration;
        }

        public List<WorkFlowAssignment> GetS(string employeeID, bool activeOnly = false) {                        
            var adapter = new WorkFlowTrackingAdapter(Configuration);
            var result = adapter.GetAssignment(employeeID, activeOnly);
            return groupWorkflowAssignment(result);
        }

        public List<WorkFlowAssignment> GetSRange(string employeeID, DateRange range, bool activeOnly = false)
        {
            var adapter = new WorkFlowTrackingAdapter(Configuration);               
            var result = adapter.GetAssignmentRange(employeeID, range, activeOnly);
            return groupWorkflowAssignment(result);
        }

        private List<WorkFlowAssignment>  groupWorkflowAssignment(List<WorkFlowAssignment> result) {
            var updateRequest = new UpdateRequest(MongoDB, Configuration);

            var instanceIDs = result.Select(x => x.InstanceId).Distinct();
            var groupedResult = result.GroupBy(x => new { x.InstanceId, x.Sequence })
            .Select(y =>
            {
                return y.First();
            }).ToList();

            var mapTitle = updateRequest.GetTitles(instanceIDs);
            foreach (var r in groupedResult)
            {
                var title = "";
                if (!mapTitle.TryGetValue(r.InstanceId, out title))
                {
                    r.Title = updateRequest.GenerateTitle(r.WorkflowType);
                    continue;
                }
                r.Title = title;
            }

            return groupedResult;
        }

        public int CountActive(string employeeID)
        {            
            var adapter = new WorkFlowTrackingAdapter(Configuration);
            var result = adapter.GetAssignment(employeeID, true);
            return result.Count();           
        }
       
    }

    public class ParamTask {
        public string InstanceID { set; get; }
        public string OriginatorEmployeeID { set; get; }
        public string ActionEmployeeID { set; get; }
        public string ActionEmployeeName { set; get; }
        public string DelegateToEmployeeID { set; get; }
        public long AXID { set; get; }
        public string Notes { set; get; }
    }

    public enum TaskType : int
    {
        Approval= 0,
        Fill = 1
    }
}
