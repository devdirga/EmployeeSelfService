using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class WorkFlow
    {
        public bool ActionApprove { set; get; }
        public bool ActionCancel { set; get; }
        public bool ActionDelegate { set; get; }
        public bool ActionReject { set; get; }
        public string Comment { set; get; }
        public DateTime ActionDateTime { set; get; }
        public string DelegateToEmployeeID { set; get; }
        public string DelegateToEmployeeName { set; get; }
        public string AssignToEmployeeID { set; get; }
        public string AssignToEmployeeName { set; get; }
        public KESSWFServices.KESSWorkflowAssignType AssignType { set; get; }
        public DateTime CompleteDateTime { set; get; }
        public string InstanceID { set; get; }
        public KESSWFServices.KESSWorkerRequestType RequestType { set; get; }
        public string RequestTypeDescription { set; get; }
        public DateTime StepCompletionDateTime { set; get; }
        public KESSWFServices.KESSWorkflowStepCompletionPolicy StepCompletionPolicy { set; get; }
        public string StepCompletionPolicyDescription { set; get; }
        public string StepName { set; get; }
        public int StepSequence { set; get; }
        public KESSWFServices.KESSWorkflowTrackingType StepTrackingType { set; get; }
        public string  StepTrackingTypeDescription { set; get; }
        public string SubmitByEmployeID { set; get; }
        public string SubmitByEmployeeName { set; get; }
        public DateTime SubmitDateTime { set; get; }
        public KESSWFServices.KESSWorkflowTrackingStatus TrackingStatus { set; get; }
        public string TrackingStatusDescription { set; get; }
        public KESSWFServices.KESSWorkflowType WorkflowType { set; get; }
        public string WorkflowTypeDescription { set; get; }
        public string WorkflowId { set; get; }
    }

    public class TrackingRequest
    {
        public string Title { set; get; }
        public DateTime LastUpdated { set; get; }                
        public string InstanceID {set; get;}
        public string SubmitByEmployeID {set; get;}
        public string SubmitByEmployeeName {set; get;}
        public DateTime SubmitDateTime {set; get;}        
        private KESSWFServices.KESSWorkflowTrackingStatus trackingStatus;
        public KESSWFServices.KESSWorkflowTrackingStatus TrackingStatus {
            set{
                trackingStatus = value;
            }
            get{
                if(this.Inverted){
                    switch (trackingStatus)
                    {
                        case KESSWFServices.KESSWorkflowTrackingStatus.Approved:
                            trackingStatus = KESSWFServices.KESSWorkflowTrackingStatus.Rejected;
                            break;
                        case KESSWFServices.KESSWorkflowTrackingStatus.Rejected:
                            trackingStatus = KESSWFServices.KESSWorkflowTrackingStatus.Approved;
                            break;
                        default:                            
                            break;
                    }
                }
                return trackingStatus;
                
            }
        }
        public string TrackingStatusDescription {set; get;}
        public KESSWFServices.KESSWorkflowType WorkflowType {set; get;}
        public bool Inverted {
            get{
                return Array.IndexOf(WorkFlowAssignment.InvertedWorkflowType, (int) WorkflowType) > -1;
            }
        }
        public string WorkflowTypeDescription {set; get;}
        public string WorkflowId {set; get;}
        public object Data {set; get;}
        public List<WorkFlow> WorkFlows {set; get;}


        public TrackingRequest(){}
        public TrackingRequest(WorkFlow workFlow){            
            this.LastUpdated = workFlow.ActionDateTime;
            if (workFlow.CompleteDateTime.Year != 1) {
                this.LastUpdated = workFlow.CompleteDateTime;
            }
            this.InstanceID = workFlow.InstanceID;                                    
            this.SubmitByEmployeID = workFlow.SubmitByEmployeID;
            this.SubmitByEmployeeName = workFlow.SubmitByEmployeeName;
            this.SubmitDateTime = workFlow.SubmitDateTime;
            this.TrackingStatus = workFlow.TrackingStatus;
            this.TrackingStatusDescription = workFlow.TrackingStatusDescription;
            this.WorkflowType = workFlow.WorkflowType;
            this.WorkflowTypeDescription = workFlow.WorkflowTypeDescription;
            this.WorkflowId = workFlow.WorkflowId;
        }
    }
}
