using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;

namespace KANO.Core.Service.AX
{
    public class WorkFlowTrackingAdapter
    {
        private IConfiguration Configuration;
        private Credential credential;

        public WorkFlowTrackingAdapter(IConfiguration config)
        {
            Configuration = config;
            credential = Tools.AXConfiguration(config);
        }

        public KESSWFServices.KESSWFServiceClient GetClient()
        {
            var Client = new KESSWFServices.KESSWFServiceClient();
            var uri = new UriBuilder(Client.Endpoint.Address.Uri);
            uri.Host = credential.Host;
            uri.Port = credential.Port;
            Client.Endpoint.Address = new System.ServiceModel.EndpointAddress(
                uri.Uri,
                new System.ServiceModel.UpnEndpointIdentity(credential.UserPrincipalName));
            Client.ClientCredentials.Windows.ClientCredential.Domain = credential.Domain;
            Client.ClientCredentials.Windows.ClientCredential.UserName = credential.Username;
            Client.ClientCredentials.Windows.ClientCredential.Password = credential.Password;
            return Client;
        }

        public KESSWFServices.CallContext GetContext()
        {
            var Context = new KESSWFServices.CallContext();
            Context.Company = credential.Company;
            return Context;
        }

        public List<WorkFlow> GetUpdateRequests(string employeeID)
        {
            var workflows = new List<WorkFlow>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try

            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getWFTrackingAsync(Context, employeeID, "");
                foreach (var d in data.Result.response)
                {
                    workflows.Add(this.mapFromAX(d));

                }
            }
            catch (Exception)
            {
                throw;
            }
            finally {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            // Console.WriteLine($"{workflows.Count} request found(s)");
            
            return workflows;
        }

        public List<WorkFlow> GetUpdateRequestsRange(string employeeID, DateRange dateRange)
        {
            var range = Tools.normalizeFilter(dateRange);
            var workflows = new List<WorkFlow>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {                
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getWFTrackingFilterDateAsync(Context, employeeID, range.Start, range.Finish);
                foreach (var d in data.Result.response)
                {
                    workflows.Add(this.mapFromAX(d));

                }
            }
            catch (Exception)
            {
                throw;
            }
            finally {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            // Console.WriteLine($"{workflows.Count} request found(s)");
            
            return workflows;
        }

        public List<WorkFlow> GetUpdateRequest(string employeeID, string instanceID)
        {
            var workflows = new List<WorkFlow>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getWFTrackingAsync(Context, employeeID, instanceID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    workflows.Add(new WorkFlow
                    {
                        ActionApprove = NoYes.Yes == (NoYes)d.ActionApprove,
                        ActionCancel = NoYes.Yes == (NoYes)d.ActionCancel,
                        Comment = d.ActionComment,
                        ActionDateTime = d.ActionDateTime,
                        ActionDelegate = NoYes.Yes == (NoYes)d.ActionDelegate,
                        DelegateToEmployeeID = d.ActionDelegateToEmplId,
                        DelegateToEmployeeName = d.ActionDelegateToEmplName,
                        ActionReject = NoYes.Yes == (NoYes)d.ActionReject,
                        AssignToEmployeeID = d.AssignToEmplId,
                        AssignToEmployeeName = d.AssignToEmplName,
                        AssignType = d.AssignType,
                        CompleteDateTime = d.CompleteDateTime,
                        InstanceID = d.InstanceId,
                        RequestType = d.RequestType,
                        RequestTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkerRequestType), (KESSWFServices.KESSWorkerRequestType)d.RequestType),
                        StepCompletionDateTime = d.StepCompletionDateTime,
                        StepCompletionPolicy = d.StepCompletionPolicy,
                        StepCompletionPolicyDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowStepCompletionPolicy), (KESSWFServices.KESSWorkflowStepCompletionPolicy)d.StepCompletionPolicy),
                        StepName = d.StepName,
                        StepSequence = d.StepSequence,
                        StepTrackingType = d.StepTrackingType,
                        StepTrackingTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowTrackingType), (KESSWFServices.KESSWorkflowTrackingType)d.StepTrackingType),
                        SubmitByEmployeID = d.SubmitByEmplId,
                        SubmitByEmployeeName = d.SubmitByEmplName,
                        SubmitDateTime = d.SubmitDateTime,
                        TrackingStatus = d.TrackingStatus,
                        TrackingStatusDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowTrackingStatus), (KESSWFServices.KESSWorkflowTrackingStatus)d.TrackingStatus),
                        WorkflowType = d.WorkflowType,
                        WorkflowTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowType), (KESSWFServices.KESSWorkflowType)d.WorkflowType),
                        WorkflowId = d.WorkflowId,
                    });

                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            return workflows;
        }

        public UpdateRequestStatus GetCurrentSatus(string employeeID, string instanceID)
        {
            KESSWFServices.KESSWFServiceGetWFTrackingResponse data;
            var workflows = new List<WorkFlow>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                data = Client.getWFTrackingAsync(Context, employeeID, instanceID).GetAwaiter().GetResult();                
            }
            catch (Exception)
            {

                throw;
            }
            finally 
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }

            if (data.response.Length > 0) {
                return (UpdateRequestStatus)data.response[0].TrackingStatus;
            }

            return UpdateRequestStatus.InReview;
        }

        public List<WorkFlow> GetUpdateRequestsByInstanceID(string employeeID, string instanceID)
        {           
            var workflows = new List<WorkFlow>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getWFTrackingAsync(Context, employeeID, instanceID).GetAwaiter().GetResult().response;
                foreach (var d in data)
                {
                    workflows.Add(this.mapFromAX(d));

                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }            

            return workflows;
        }

        public List<WorkFlowAssignment> GetAssignment(string employeeID, bool activeOnly = false)
        {            
            var workflowAssignment = new List<WorkFlowAssignment>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getWFTrackingAssignAsync(Context, employeeID, activeOnly);
                foreach (var d in data.Result.response)
                {
                    if (d.AssignToEmplId == d.SubmitByEmplId) continue;
                    workflowAssignment.Add(this.mapFromAX(d));
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            

            return workflowAssignment;
        }

        public List<WorkFlowAssignment> GetAssignmentRange(string employeeID, DateRange dateRange, bool activeOnly = false) {
            var range = Tools.normalizeFilter(dateRange);
            var workflowAssignment = new List<WorkFlowAssignment>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try {
                var data = Client.getWFTrackingAssignFilterDateAsync(Context, employeeID, range.Start, range.Finish, activeOnly);
                var res = data.Result.response;
                foreach (var d in res) {
                    if (d.AssignToEmplId == d.SubmitByEmplId && (d.AssignApprove == KESSWFServices.NoYes.Yes || d.AssignReject == KESSWFServices.NoYes.Yes)) continue;
                    workflowAssignment.Add(this.mapFromAX(d));
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }


            return workflowAssignment;
        }

        public List<WorkFlowAssignment> GetMAssignmentRange(string employeeID, DateRange dateRange, bool activeOnly = false)
        {
            var range = Tools.normalizeFilter(dateRange);
            var workflowAssignment = new List<WorkFlowAssignment>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                var data = Client.getWFTrackingAssignFilterDateAsync(Context, employeeID, range.Start, range.Finish, activeOnly);
                foreach (var d in data.Result.response)
                    if (d.AssignToEmplId == employeeID 
                        && d.AssignCancel == KESSWFServices.NoYes.No 
                        && d.AssignReject == KESSWFServices.NoYes.Yes 
                        && d.AssignDelegate == KESSWFServices.NoYes.No  
                        && d.AssignType != KESSWFServices.KESSWorkflowAssignType.Originator)
                        workflowAssignment.Add(this.mapFromAX(d));

            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            return workflowAssignment;
        }

        public List<Employee> GetAssignee(long AXID)
        {   
            var employements = new List<Employee>();
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                //Client.OpenAsync().GetAwaiter();
                var data = Client.getWFTrackingAssignDelegateEmplAsync(Context, AXID);
                foreach (var d in data.Result.response)
                {

                    employements.Add(new Employee
                    {
                        AXID = d.RecId,
                        EmployeeName = d.EmplName,
                        EmployeeID = d.EmplId,
                    });
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally 
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            

            return employements;
        }

        private WorkFlow mapFromAX(KESSWFServices.WFTracking data) {
            return new WorkFlow
            {
                ActionApprove = NoYes.Yes == (NoYes)data.ActionApprove,
                ActionCancel = NoYes.Yes == (NoYes)data.ActionCancel,
                Comment = data.ActionComment,
                ActionDateTime = data.ActionDateTime,
                ActionDelegate = NoYes.Yes == (NoYes)data.ActionDelegate,
                DelegateToEmployeeID = data.ActionDelegateToEmplId,
                DelegateToEmployeeName = data.ActionDelegateToEmplName,
                ActionReject = NoYes.Yes == (NoYes)data.ActionReject,
                AssignToEmployeeID = data.AssignToEmplId,
                AssignToEmployeeName = data.AssignToEmplName,
                AssignType = data.AssignType,
                CompleteDateTime = data.CompleteDateTime,
                InstanceID = data.InstanceId,
                RequestType = data.RequestType,
                RequestTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkerRequestType), (KESSWFServices.KESSWorkerRequestType)data.RequestType),
                StepCompletionDateTime = data.StepCompletionDateTime,
                StepCompletionPolicy = data.StepCompletionPolicy,
                StepCompletionPolicyDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowStepCompletionPolicy), (KESSWFServices.KESSWorkflowStepCompletionPolicy)data.StepCompletionPolicy),
                StepName = data.StepName,
                StepSequence = data.StepSequence,
                StepTrackingType = data.StepTrackingType,
                StepTrackingTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowTrackingType), (KESSWFServices.KESSWorkflowTrackingType)data.StepTrackingType),
                SubmitByEmployeID = data.SubmitByEmplId,
                SubmitByEmployeeName = data.SubmitByEmplName,
                SubmitDateTime = data.SubmitDateTime,
                TrackingStatus = data.TrackingStatus,
                TrackingStatusDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowTrackingStatus), (KESSWFServices.KESSWorkflowTrackingStatus)data.TrackingStatus),
                WorkflowType = data.WorkflowType,
                WorkflowTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowType), (KESSWFServices.KESSWorkflowType)data.WorkflowType),
                WorkflowId = data.WorkflowId,
            };

        }

        private WorkFlowAssignment mapFromAX(KESSWFServices.WFTrackingAssign data)
        {
            
            return new WorkFlowAssignment
            {
                ActionApprove = NoYes.Yes == (NoYes)data.ActionApprove,
                ActionCancel = NoYes.Yes == (NoYes)data.ActionCancel,
                Comment = data.ActionComment,
                ActionDateTime = data.ActionDateTime,
                ActionDelegate = NoYes.Yes == (NoYes)data.ActionDelegate,
                ActionDelegateToEmployeeID = data.ActionDelegateToEmplId,
                ActionDelegateToEmployeeName = data.ActionDelegateToEmplName,
                ActionReject = NoYes.Yes == (NoYes)data.ActionReject,
                AssignApprove = NoYes.Yes == (NoYes)data.AssignApprove,
                AssignCancel = NoYes.Yes == (NoYes)data.AssignCancel,
                AssignDelegate = NoYes.Yes == (NoYes)data.AssignDelegate,
                AssignReject = NoYes.Yes == (NoYes)data.AssignReject,
                AssignType = data.AssignType,
                AssignTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowAssignType), (KESSWFServices.KESSWorkflowAssignType)data.AssignType),
                AssignToEmployeeID = data.AssignToEmplId,
                AssignToEmployeeName = data.AssignToEmplName,
                RequestType = data.RequestType,
                RequestTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkerRequestType), (KESSWFServices.KESSWorkerRequestType)data.RequestType),
                StepTrackingType = data.StepTrackingType,
                StepTrackingTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowTrackingType), (KESSWFServices.KESSWorkflowTrackingType)data.StepTrackingType),
                Sequence = data.Sequence,
                InstanceId = data.InstanceId,
                AXID = data.RecId,
                SubmitEmployeeID = data.SubmitByEmplId,
                SubmitEmployeeName = data.SubmitByEmplName,
                SubmitDateTime = data.SubmitDateTime,
                TrackingStatus = data.TrackingStatus,
                TrackingStatusDescription = data.TrackingStatus.ToString(),
                WorkflowId = data.WorkflowId,
                WorkflowType = data.WorkflowType,
                WorkflowTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowType), (KESSWFServices.KESSWorkflowType)data.WorkflowType),
            };

        }

        public string Do(KESSWFServices.WFTrackingAction action) {
            KESSWFServices.KESSWFServiceUpdWFTrackingActionResponse result;
            var Client = this.GetClient();
            var Context = this.GetContext();
            try
            {
                result = Client.updWFTrackingActionAsync(Context, action).GetAwaiter().GetResult();                
            }
            catch (Exception e)
            {
                throw e;
            }
            finally 
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            return result.response;
        }

        public void Approve(long AXID, string reason=""){
            var action = new KESSWFServices.WFTrackingAction();
            action.ActionApprove = KESSWFServices.NoYes.Yes;
            action.ActionCancel = KESSWFServices.NoYes.No;
            action.ActionComment = reason;
            action.ActionDelegate = KESSWFServices.NoYes.No;
            action.ActionDelegateToEmplId = "";
            action.ActionReject = KESSWFServices.NoYes.No;
            action.RecId = AXID;

            this.Do(action);
        }

        public void Reject(long AXID, string reason="")
        {
            var action = new KESSWFServices.WFTrackingAction();
            action.ActionApprove = KESSWFServices.NoYes.No;
            action.ActionCancel = KESSWFServices.NoYes.No;
            action.ActionComment = reason;
            action.ActionDelegate = KESSWFServices.NoYes.No;
            action.ActionDelegateToEmplId = "";
            action.ActionReject = KESSWFServices.NoYes.Yes;
            action.RecId = AXID;

            this.Do(action);
        }

        public void Cancel(long AXID)
        {
            var action = new KESSWFServices.WFTrackingAction();
            action.ActionApprove = KESSWFServices.NoYes.No;
            action.ActionCancel = KESSWFServices.NoYes.Yes;
            action.ActionComment = "";
            action.ActionDelegate = KESSWFServices.NoYes.No;
            action.ActionDelegateToEmplId = "";
            action.ActionReject = KESSWFServices.NoYes.No;
            action.RecId = AXID;

            this.Do(action);
        }

        public void Delegate(string employeeID, long AXID)
        {
            var action = new KESSWFServices.WFTrackingAction();
            action.ActionApprove = KESSWFServices.NoYes.No;
            action.ActionCancel = KESSWFServices.NoYes.No;
            action.ActionComment = "";
            action.ActionDelegate = KESSWFServices.NoYes.Yes;
            action.ActionDelegateToEmplId = employeeID;
            action.ActionReject = KESSWFServices.NoYes.No;
            action.RecId = AXID;

            this.Do(action);
        }

        public int MGetAssignmentCount(string employeeID, bool activeOnly = false)
        {
            var Client = this.GetClient();
            var Context = this.GetContext();
            int count = 0;
            try
            {
                Task<KESSWFServices.KESSWFServiceGetWFTrackingAssignResponse> data = Client.getWFTrackingAssignAsync(Context, employeeID, activeOnly);
                List<KESSWFServices.WFTrackingAssign> c = new List<KESSWFServices.WFTrackingAssign>(data.Result.response);
                count = c.FindAll(a => a.AssignToEmplId != a.SubmitByEmplId).Count;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (Client.InnerChannel.State != System.ServiceModel.CommunicationState.Faulted) Client.CloseAsync().Wait();
            }
            return count;
        }

    }
}
