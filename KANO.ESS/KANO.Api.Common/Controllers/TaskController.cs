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
using Microsoft.AspNetCore.Authorization;
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
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase DB;
        private readonly IConfiguration Configuration;
        //private readonly Leave _leave;
        //private readonly TimeAttendance _timeAttendance;
        private readonly WorkFlowAssignment _assignment;
        //private readonly String ErrMessage = "Unable to get tasks for";
        private readonly String success = "success";

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public TaskController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _assignment = new WorkFlowAssignment(DB, Configuration);
       }

        [HttpGet("{employeeID}")]
        public IActionResult Get(string employeeID)
        {
            try { return ApiResult<List<WorkFlowAssignment>>.Ok((new WorkFlowAssignment(DB, Configuration).GetS(employeeID)).OrderByDescending(x => x.SubmitDateTime).ToList()); }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message ); }
        }

        [HttpPost("range/{employeeID}")]
        public IActionResult GetRange(string employeeID, [FromBody] ParamTaskFilter p)
        {
            List<WorkFlowAssignment> res = new List<WorkFlowAssignment>();
            try {
                if (p.Range == null)
                    res = new WorkFlowAssignment(DB, Configuration).GetS(employeeID);
                else
                {
                    var defaultStart = new DateTime(1992, 12, 07);
                    var defaultFinish = DateTime.Now;
                    if (p.Range.Start.Year == 1 && p.Range.Finish.Year == 1)
                        p.Range = new DateRange(defaultStart, defaultFinish);
                    else if (p.Range.Start.Year == 1 && p.Range.Finish.Year != 1)
                        p.Range = new DateRange(defaultStart, p.Range.Finish);
                    else if (p.Range.Start.Year != 1 && p.Range.Finish.Year == 1)
                        p.Range = new DateRange(p.Range.Start, defaultFinish);
                    List<Task<TaskRequest<List<WorkFlowAssignment>>>> tasks = new List<Task<TaskRequest<List<WorkFlowAssignment>>>> {
                        Task.Run(() => {
                            return TaskRequest<List<WorkFlowAssignment>>.Create("AX", 
                                new WorkFlowAssignment(DB, Configuration).GetSRange(employeeID, p.Range));
                        }),
                        Task.Run(() => {
                            List<WorkFlowAssignment> surveys = new List<WorkFlowAssignment>();
                            foreach (var survey in (new Survey(DB, Configuration).GetRange(employeeID, p.Range)))
                                surveys.Add(MapSurveyToAX(survey));
                            return TaskRequest<List<WorkFlowAssignment>>.Create("DB", surveys);
                        })
                    };
                    var t = Task.WhenAll(tasks);
                    try { t.Wait(); }
                    catch (Exception e) { throw e; }
                    if (t.Status == TaskStatus.RanToCompletion)
                        foreach (var r in t.Result)
                            res.AddRange(r.Result);
                }
                return ApiResult<List<WorkFlowAssignment>>.Ok(res.OrderByDescending(x => x.SubmitDateTime).ToList());
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }            
        }

        [HttpGet("assignee/{axid}")]
        public IActionResult GetAssignee(string axid)
        {
            try { return ApiResult<List<Employee>>.Ok(new WorkFlowTrackingAdapter(Configuration).GetAssignee(Convert.ToInt64(axid)));}
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpGet("active/{employeeID}")]
        public IActionResult GetActive(string employeeID)
        {
            List<WorkFlowAssignment> res = new List<WorkFlowAssignment>();
            try {
                List<Task<TaskRequest<List<WorkFlowAssignment>>>> tasks = new List<Task<TaskRequest<List<WorkFlowAssignment>>>> {
                    Task.Run(() => {
                        return TaskRequest<List<WorkFlowAssignment>>.Create("AX", new WorkFlowAssignment(DB, Configuration).GetS(employeeID, true));
                    }),
                    Task.Run(() => {
                        List<WorkFlowAssignment> surveys = new List<WorkFlowAssignment>();
                        foreach (var survey in (new Survey(DB, Configuration).GetOne(employeeID)))
                            surveys.Add(MapSurveyToAX(survey));
                        return TaskRequest<List<WorkFlowAssignment>>.Create("DB", surveys);
                    })
                };

                var t = Task.WhenAll(tasks);
                try { t.Wait(); }
                catch (Exception e) { throw e; }

                if (t.Status == TaskStatus.RanToCompletion)
                    foreach (var r in t.Result)
                        res.AddRange(r.Result);

                return ApiResult<List<WorkFlowAssignment>>.Ok(res.OrderByDescending(x => x.SubmitDateTime).ToList());
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }            
        }

        private WorkFlowAssignment MapSurveyToAX(SurveySchedule d) {
            String OdooSurveyId = String.Empty;
            try {
                Survey survey = this.DB.GetCollection<Survey>().Find(x => x.Id == d.SurveyID).FirstOrDefault();
                string[] url1 = survey.SurveyUrl.ToString().Split("start/");
                string[] url2 = url1[1].Split("?");
                OdooSurveyId = url2[0];
            }
            catch (Exception){}
            return new WorkFlowAssignment {
                ActionApprove = NoYes.Yes == NoYes.No,
                ActionCancel = NoYes.Yes == NoYes.No,
                Comment = d.Title,
                ActionDateTime = d.CreatedDate,
                ActionDelegate = NoYes.Yes == NoYes.No,
                ActionDelegateToEmployeeID = d.ParticipantID,
                ActionDelegateToEmployeeName = d.ParticipantID,
                ActionReject = NoYes.Yes == NoYes.No,
                AssignApprove = NoYes.Yes == NoYes.No,
                AssignCancel = NoYes.Yes == NoYes.No,
                AssignDelegate = NoYes.Yes == NoYes.No,
                AssignReject = NoYes.Yes == NoYes.No,
                AssignType = KESSWFServices.KESSWorkflowAssignType.Originator,
                AssignTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowAssignType), (KESSWFServices.KESSWorkflowAssignType)KESSWFServices.KESSWorkflowAssignType.Originator),
                AssignToEmployeeID = d.ParticipantID,
                AssignToEmployeeName = d.ParticipantID,
                RequestType = KESSWFServices.KESSWorkerRequestType.CNTickets,
                RequestTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkerRequestType), (KESSWFServices.KESSWorkerRequestType)KESSWFServices.KESSWorkerRequestType.CNTickets),
                StepTrackingType = KESSWFServices.KESSWorkflowTrackingType.Creation,
                StepTrackingTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowTrackingType), (KESSWFServices.KESSWorkflowTrackingType)KESSWFServices.KESSWorkflowTrackingType.Creation),
                Sequence = 0,
                InstanceId = d.OdooID,
                AXID = long.Parse(d.OdooID) ,
                SubmitEmployeeID = d.ParticipantID,
                SubmitEmployeeName = d.ParticipantID,
                SubmitDateTime = d.CreatedDate,
                TrackingStatus = KESSWFServices.KESSWorkflowTrackingStatus.InReview,
                TrackingStatusDescription = KESSWFServices.KESSWorkflowTrackingStatus.InReview.ToString(),
                WorkflowId = d.OdooID,
                WorkflowType = KESSWFServices.KESSWorkflowType.HRM,
                TaskType = TaskType.Fill,
                WorkflowTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowType), (KESSWFServices.KESSWorkflowType)KESSWFServices.KESSWorkflowType.HRM),
                Title = d.Title,
                OdooSurveyID = OdooSurveyId
            };
        }

        [HttpGet("active/count/{employeeID}")]
        public IActionResult CountActive(string employeeID)
        {
            try {return ApiResult<object>.Ok(null, _assignment.CountActive(employeeID), null);}
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("approve")]
        public IActionResult Approve([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Approve(p.AXID);
                this.UpdateHistory(p, UpdateRequestStatus.Approved);
                new Notification(Configuration, DB).SendApprovals(p.OriginatorEmployeeID, p.InstanceID);
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("approve/invert")]
        public IActionResult ApproveInvert([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Reject(p.AXID);
                this.UpdateHistory(p, UpdateRequestStatus.Approved);
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("reject")]
        public IActionResult Reject([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Reject(p.AXID, p.Notes);
                this.UpdateHistory(p, UpdateRequestStatus.Rejected);
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("reject/invert")]
        public IActionResult RejectInvert([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Approve(p.AXID, p.Notes);
                this.UpdateHistory(p, UpdateRequestStatus.Rejected);
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("delegate")]
        public IActionResult Delegate([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Delegate(p.DelegateToEmployeeID, p.AXID);
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("cancel")]
        public IActionResult Cancel([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Cancel(p.AXID);
                this.UpdateHistory(p, UpdateRequestStatus.Cancelled);
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        private void UpdateHistory(ParamTask t, UpdateRequestStatus s)
        {
            var ur = this.DB.GetCollection<UpdateRequest>()
                              .Find(x => x.AXRequestID == t.InstanceID && x.EmployeeID == t.OriginatorEmployeeID)
                              .FirstOrDefault();
            if (ur != null)
            {
                ur.AddHistory(t.ActionEmployeeID, t.ActionEmployeeName, s, t.Notes);
                DB.Save(ur);
            }
        }

        [HttpGet("agenda/{employeeID}")]
        public IActionResult GetAgenda(string employeeID)
        {
            try { return ApiResult<List<WorkFlowAssignment>>.Ok((new WorkFlowAssignment(DB, Configuration).GetS(employeeID)).OrderByDescending(x => x.SubmitDateTime).ToList());}
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [Authorize]
        [HttpGet("m/{employeeID}")]
        public IActionResult MGet(string employeeID)
        {
            try { return ApiResult<List<WorkFlowAssignment>>.Ok(new WorkFlowAssignment(DB, Configuration).GetS(employeeID).OrderByDescending(x => x.SubmitDateTime).ToList()); }
            catch (Exception e){return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }

        [Authorize]
        [HttpPost("mrange/{employeeID}")]
        public IActionResult MRange(string employeeID, [FromBody] ParamTaskFilter p)
        {
            List<WorkFlowAssignment> result = new List<WorkFlowAssignment>();
            try
            {
                if (p.Range == null)
                    return ApiResult<List<WorkFlowAssignment>>.Ok(new WorkFlowAssignment(DB, Configuration).GetS(employeeID).OrderByDescending(x => x.SubmitDateTime).ToList());
                else
                {
                    var defaultStart = new DateTime(1992, 12, 07);
                    var defaultFinish = DateTime.Now;
                    if (p.Range.Start.Year == 1 && p.Range.Finish.Year == 1) p.Range = new DateRange(defaultStart, defaultFinish);
                    else if (p.Range.Start.Year == 1 && p.Range.Finish.Year != 1) p.Range = new DateRange(defaultStart, p.Range.Finish);
                    else if (p.Range.Start.Year != 1 && p.Range.Finish.Year == 1) p.Range = new DateRange(p.Range.Start, defaultFinish);
                    List<Task<TaskRequest<List<WorkFlowAssignment>>>> tasks = new List<Task<TaskRequest<List<WorkFlowAssignment>>>> {
                        Task.Run(() => {
                            return TaskRequest<List<WorkFlowAssignment>>.Create("AX", new WorkFlowAssignment(DB, Configuration).GetSRange(employeeID, p.Range));
                        }),
                        Task.Run(() => {
                            List<WorkFlowAssignment> workflowdb = new List<WorkFlowAssignment>();
                            foreach (var data in new Survey(DB, Configuration).GetRange(employeeID, p.Range)){
                                workflowdb.Add(new WorkFlowAssignment {
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
                                });
                            }
                            return TaskRequest<List<WorkFlowAssignment>>.Create("DB", workflowdb);
                        })
                    };
                    var t = Task.WhenAll(tasks);
                    try { t.Wait(); }
                    catch (Exception e) { throw e; }
                    if (t.Status == TaskStatus.RanToCompletion)
                    {
                        foreach (var r in t.Result)
                        {
                            result.AddRange(r.Result);
                        }
                    }
                    return ApiResult<List<WorkFlowAssignment>>.Ok(result.OrderByDescending(x => x.SubmitDateTime).ToList());
                }
            }
            catch (Exception e){return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }

        [Authorize]
        [HttpPost("mactive/{employeeID}")]
        public IActionResult MGetActive(string employeeID, [FromBody] ParamTaskFilter p)
        {
            List<WorkFlowAssignment> res = new List<WorkFlowAssignment>();
            DateTime end = DateTime.Now;
            DateTime start = end.AddDays(-30);
            DateRange drange = new DateRange(start, end);
            try {
                List<Task<TaskRequest<List<WorkFlowAssignment>>>> tasks = new List<Task<TaskRequest<List<WorkFlowAssignment>>>> {
                    Task.Run(() => {
                        return TaskRequest<List<WorkFlowAssignment>>.Create("AX", new WorkFlowAssignment(DB, Configuration).GetSRange(p.Username, drange, true));
                    }),
                    Task.Run(() => {
                        List<WorkFlowAssignment> workflowdb = new List<WorkFlowAssignment>();
                        foreach (var data in new Survey(DB, Configuration).Get(p.Username))
                            workflowdb.Add(new WorkFlowAssignment {
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
                                AssignTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowAssignType), KESSWFServices.KESSWorkflowAssignType.Originator),
                                AssignToEmployeeID = data.ParticipantID,
                                AssignToEmployeeName = data.ParticipantID,
                                RequestType = KESSWFServices.KESSWorkerRequestType.CNTickets,
                                RequestTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkerRequestType), KESSWFServices.KESSWorkerRequestType.CNTickets),
                                StepTrackingType = KESSWFServices.KESSWorkflowTrackingType.Creation,
                                StepTrackingTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowTrackingType), KESSWFServices.KESSWorkflowTrackingType.Creation),
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
                                WorkflowTypeDescription = Enum.GetName(typeof(KESSWFServices.KESSWorkflowType), KESSWFServices.KESSWorkflowType.HRM),
                                Title = data.Title,
                            });
                        return TaskRequest<List<WorkFlowAssignment>>.Create("DB", workflowdb);
                    })
                };

                var t = Task.WhenAll(tasks);
                try { t.Wait(); }
                catch (Exception e) { throw e; }

                if (t.Status == TaskStatus.RanToCompletion)
                    foreach (var r in t.Result)
                        if (r.Label == "AX")
                            res.AddRange(r.Result);

                return ApiResult<List<WorkFlowAssignment>>.Ok(
                    res.OrderByDescending(x => x.SubmitDateTime).ToList(), res.Count);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [Authorize]
        [HttpGet("active/mcount/{employeeID}")]
        public IActionResult MCountActive(string employeeID)
        {
            try { return ApiResult<object>.Ok(null, _assignment.MCountActive(employeeID), null); }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [Authorize]
        [HttpGet("massignee/{axid}")]
        public IActionResult MAssignee(string axid)
        {
            try { return ApiResult<List<Employee>>.Ok(new WorkFlowTrackingAdapter(Configuration).GetAssignee(Convert.ToInt64(axid)));}
            catch (Exception e){return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }

        [Authorize]
        [HttpPost("mapprove")]
        public IActionResult MApprove([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Approve(p.AXID);
                UpdateHistory(p, UpdateRequestStatus.Approved);
                new Notification(Configuration, DB).SendNotification(p.ActionEmployeeID, p.InstanceID, "approved");
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e) {return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }

        [Authorize]
        [HttpPost("mreject")]
        public IActionResult MReject([FromBody] ParamTask p)
        {
            try
            {
                new WorkFlowTrackingAdapter(Configuration).Reject(p.AXID, p.Notes);
                UpdateHistory(p, UpdateRequestStatus.Rejected);
                new Notification(Configuration, DB).SendNotification(p.ActionEmployeeID, p.InstanceID, "rejected");
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e){return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }

        [Authorize]
        [HttpPost("mdelegate")]
        public IActionResult MDelegate([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Delegate(p.DelegateToEmployeeID, p.AXID);
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e){return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }

        [Authorize]
        [HttpPost("mcancel")]
        public IActionResult MCancel([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Cancel(p.AXID);
                UpdateHistory(p, UpdateRequestStatus.Cancelled);
                new Notification(Configuration, DB).SendNotification(p.ActionEmployeeID, p.InstanceID, "canceled");
                return ApiResult<bool>.Ok(success);
            }
            catch (Exception e){return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }
    }
}