using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace KANO.Api.Common.Controllers
{
    [Route("api/common/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Leave _leave;
        private TimeAttendance _timeAttendance;
        private WorkFlowAssignment _assignment;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public TaskController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _assignment = new WorkFlowAssignment(DB, Configuration);
       }

        [HttpGet("{employeeID}")]
        public async Task<IActionResult> Get(string employeeID)
        {
            List<WorkFlowAssignment> result;
            try
            {
                var workFlowAssignment = new WorkFlowAssignment(DB, Configuration);
                result = workFlowAssignment.GetS(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get tasks for {employeeID} :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<WorkFlowAssignment>>.Ok(result.OrderByDescending(x => x.SubmitDateTime).ToList());
        }

        [HttpPost("range/{employeeID}")]
        public async Task<IActionResult> GetRange(string employeeID, [FromBody] ParamTaskFilter param)
        {

            List<WorkFlowAssignment> result = new List<WorkFlowAssignment>();
            try
            {
                var workFlowAssignment = new WorkFlowAssignment(DB, Configuration);

                if (param.Range == null)
                    result = workFlowAssignment.GetS(employeeID);
                else
                {

                    var defaultStart = new DateTime(1992, 12, 07);
                    var defaultFinish = DateTime.Now;
                    if (param.Range.Start.Year == 1 && param.Range.Finish.Year == 1)
                        param.Range = new DateRange(defaultStart, defaultFinish);
                    else if (param.Range.Start.Year == 1 && param.Range.Finish.Year != 1)
                        param.Range = new DateRange(defaultStart, param.Range.Finish);
                    else if (param.Range.Start.Year != 1 && param.Range.Finish.Year == 1)
                        param.Range = new DateRange(param.Range.Start, defaultFinish);

                    List<Task<TaskRequest<List<WorkFlowAssignment>>>> tasks = new List<Task<TaskRequest<List<WorkFlowAssignment>>>> {
                        Task.Run(() => {
                            var wfAssignment = new WorkFlowAssignment(DB, Configuration);
                            result = wfAssignment.GetSRange(employeeID, param.Range);
                            return TaskRequest<List<WorkFlowAssignment>>.Create("AX", result);
                        }),
                        Task.Run(() => {
                            List<SurveySchedule> sv = new Survey(DB, Configuration).GetRange(employeeID, param.Range);
                            List<WorkFlowAssignment> workflowdb = new List<WorkFlowAssignment>();
                            foreach (var s in sv)
                            {
                                workflowdb.Add(mapSurveyToAX(s));
                            }
                            return TaskRequest<List<WorkFlowAssignment>>.Create("DB", workflowdb);
                        })
                    };
                    
                    var t = Task.WhenAll(tasks);
                    try
                    {
                        t.Wait();
                    }
                    catch (Exception e)
                    {
                        throw e;
                    }

                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        foreach (var r in t.Result)
                        {
                            result.AddRange(r.Result);
                        }
                    }

                }

            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get tasks for {employeeID} :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<WorkFlowAssignment>>.Ok(result.OrderByDescending(x => x.SubmitDateTime).ToList());
        }

        [HttpGet("assignee/{axid}")]
        public async Task<IActionResult> GetAssignee(string axid)
        {
            List<Employee> result;
            try
            {
                var adapter = new WorkFlowTrackingAdapter(Configuration);
                result = adapter.GetAssignee(Convert.ToInt64(axid));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get delegation assignee :\n{Format.ExceptionString(e)}");
            }            

            return ApiResult<List<Employee>>.Ok(result);
        }

        [HttpGet("active/{employeeID}")]
        public async Task<IActionResult> GetActive(string employeeID)
        {
            
            List<WorkFlowAssignment> result = new List<WorkFlowAssignment>();
            try
            {
                List<Task<TaskRequest<List<WorkFlowAssignment>>>> tasks = new List<Task<TaskRequest<List<WorkFlowAssignment>>>> {
                    Task.Run(() => {
                        var workFlowAssignment = new WorkFlowAssignment(DB, Configuration);
                        result = workFlowAssignment.GetS(employeeID, true);
                        return TaskRequest<List<WorkFlowAssignment>>.Create("AX", result);
                    }),
                    Task.Run(() => {
                        List<SurveySchedule> survey = new Survey(DB, Configuration).Get(employeeID);
                        List<WorkFlowAssignment> workflowdb = new List<WorkFlowAssignment>();
                        foreach (var s in survey)
                        {
                            workflowdb.Add(mapSurveyToAX(s));
                        }
                        return TaskRequest<List<WorkFlowAssignment>>.Create("DB", workflowdb);
                    })
                };

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                if (t.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var r in t.Result)
                    {
                        result.AddRange(r.Result);
                    }    
                }

            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get active tasks for {employeeID} :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<WorkFlowAssignment>>.Ok(result.OrderByDescending(x => x.SubmitDateTime).ToList());
        }

        private WorkFlowAssignment mapSurveyToAX(SurveySchedule data)
        {
            return new WorkFlowAssignment
            {
                ActionApprove = NoYes.Yes == NoYes.No,
                ActionCancel = NoYes.Yes == NoYes.No,
                Comment = data.Title,
                ActionDateTime = data.CreatedDate,
                ActionDelegate = NoYes.Yes == NoYes.No,
                ActionDelegateToEmployeeID = data.ParticipantID,
                ActionDelegateToEmployeeName = data.ParticipantID,
                ActionReject = NoYes.Yes == NoYes.No,
                AssignApprove = NoYes.Yes == NoYes.No,
                AssignCancel = NoYes.Yes == NoYes.No,
                AssignDelegate = NoYes.Yes == NoYes.No,
                AssignReject = NoYes.Yes == NoYes.No,
                AssignType = KESSWFServices.KESSWorkflowAssignType.Originator,
                AssignTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowAssignType), (KESSWFServices.KESSWorkflowAssignType)KESSWFServices.KESSWorkflowAssignType.Originator),
                AssignToEmployeeID = data.ParticipantID,
                AssignToEmployeeName = data.ParticipantID,
                RequestType = KESSWFServices.KESSWorkerRequestType.CNTickets,
                RequestTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkerRequestType), (KESSWFServices.KESSWorkerRequestType)KESSWFServices.KESSWorkerRequestType.CNTickets),
                StepTrackingType = KESSWFServices.KESSWorkflowTrackingType.Creation,
                StepTrackingTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowTrackingType), (KESSWFServices.KESSWorkflowTrackingType)KESSWFServices.KESSWorkflowTrackingType.Creation),
                Sequence = 0,
                InstanceId = data.OdooID,
                AXID = long.Parse(data.OdooID) ,
                SubmitEmployeeID = data.ParticipantID,
                SubmitEmployeeName = data.ParticipantID,
                SubmitDateTime = data.CreatedDate,
                TrackingStatus = KESSWFServices.KESSWorkflowTrackingStatus.InReview,
                TrackingStatusDescription = KESSWFServices.KESSWorkflowTrackingStatus.InReview.ToString(),
                WorkflowId = data.OdooID,
                WorkflowType = KESSWFServices.KESSWorkflowType.HRM,
                TaskType = TaskType.Fill,
                WorkflowTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowType), (KESSWFServices.KESSWorkflowType)KESSWFServices.KESSWorkflowType.HRM),
                Title = data.Title,
            };

        }

        [HttpGet("active/count/{employeeID}")]
        public async Task<IActionResult> CountActive(string employeeID)
        {
            try
            {
                var result = _assignment.CountActive(employeeID);
                return ApiResult<object>.Ok(null, result, null);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get active task  :\n{Format.ExceptionString(e)}");
            }            
        }

        [HttpPost("approve")]
        public async Task<IActionResult> Approve([FromBody] ParamTask param)
        {           
            try
            {
                var adapter = new WorkFlowTrackingAdapter(Configuration);
                adapter.Approve(param.AXID);

                // Update
                this.UpdateHistory(param, UpdateRequestStatus.Approved);

                // Send approval notification
                new Notification(Configuration, DB).SendApprovals(param.OriginatorEmployeeID, param.InstanceID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get approve :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<bool>.Ok("Request has been approved");
        }

        [HttpPost("approve/invert")]
        public async Task<IActionResult> ApproveInvert([FromBody] ParamTask param)
        {           
            try
            {
                var adapter = new WorkFlowTrackingAdapter(Configuration);
                adapter.Reject(param.AXID);

                // Update
                this.UpdateHistory(param, UpdateRequestStatus.Approved);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get to approve:\n{Format.ExceptionString(e)}");
            }

            return ApiResult<bool>.Ok("Request has been approved");
        }

        [HttpPost("reject")]
        public async Task<IActionResult> Reject([FromBody] ParamTask param)
        {
            try
            {
                var adapter = new WorkFlowTrackingAdapter(Configuration);
                adapter.Reject(param.AXID, param.Notes);

                // Update                
                this.UpdateHistory(param, UpdateRequestStatus.Rejected);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get reject :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<bool>.Ok("Request has been rejected");
        }

        [HttpPost("reject/invert")]
        public async Task<IActionResult> RejectInvert([FromBody] ParamTask param)
        {
            try
            {
                var adapter = new WorkFlowTrackingAdapter(Configuration);
                adapter.Approve(param.AXID, param.Notes);

                // Update                
                this.UpdateHistory(param, UpdateRequestStatus.Rejected);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get reject :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<bool>.Ok("Request has been rejected");
        }

        [HttpPost("delegate")]
        public async Task<IActionResult> Delegate([FromBody] ParamTask param)
        {
            try
            {
                var adapter = new WorkFlowTrackingAdapter(Configuration);
                adapter.Delegate(param.DelegateToEmployeeID, param.AXID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get delegate :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<bool>.Ok("Request has been delegated");
        }

        [HttpPost("cancel")]
        public async Task<IActionResult> Cancel([FromBody] ParamTask param)
        {           
            try
            {
                var adapter = new WorkFlowTrackingAdapter(Configuration);
                adapter.Cancel(param.AXID);

                // Update
                this.UpdateHistory(param, UpdateRequestStatus.Cancelled);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get cancel :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<bool>.Ok("Request has been canceled");
        }

        private void UpdateHistory(ParamTask task, UpdateRequestStatus status)
        {
            var updateRequest = this.DB.GetCollection<UpdateRequest>()
                              .Find(x => x.AXRequestID == task.InstanceID && x.EmployeeID == task.OriginatorEmployeeID)
                              .FirstOrDefault();

            if (updateRequest != null)
            {
                updateRequest.AddHistory(task.ActionEmployeeID, task.ActionEmployeeName, status, task.Notes);
                //updateRequest.Status = status;
                DB.Save(updateRequest);

                //var updateOptions = new UpdateOptions();
                //updateOptions.IsUpsert = false;
                //switch (updateRequest.Module)
                //{
                //    case UpdateRequestModule.LEAVE:
                //        this.DB.GetCollection<Leave>().UpdateMany(
                //                    x => x.AXRequestID == task.InstanceID,
                //                    Builders<Leave>.Update
                //                        .Set(d => d.Status, status),
                //                    updateOptions
                //                );
                //        break;
                //    case UpdateRequestModule.UPDATE_TIMEATTENDANCE:
                //        this.DB.GetCollection<TimeAttendance>().UpdateMany(
                //                    x => x.AXRequestID == task.InstanceID,
                //                    Builders<TimeAttendance>.Update
                //                        .Set(d => d.Status, status),
                //                    updateOptions
                //                );
                //        break;
                //    default:
                //        break;
                //}
            }
        }

        [HttpGet("agenda/{employeeID}")]
        public async Task<IActionResult> GetAgenda(string employeeID)
        {
            List<WorkFlowAssignment> result;
            try
            {
                //var travel = adapter.GetTravelByStatus(employeeID, (KESSTEServices.KESSTrvExpTravelReqStatus)status);
                var workFlowAssignment = new WorkFlowAssignment(DB, Configuration);
                result = workFlowAssignment.GetS(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get tasks for {employeeID} :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<WorkFlowAssignment>>.Ok(result.OrderByDescending(x => x.SubmitDateTime).ToList());
        }
    }
}