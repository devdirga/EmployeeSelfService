﻿using System;
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
        private readonly Leave _leave;
        private readonly TimeAttendance _timeAttendance;
        private readonly WorkFlowAssignment _assignment;
        private readonly String ErrMessage = "Unable to get tasks for";

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
            try { 
                return ApiResult<List<WorkFlowAssignment>>.Ok(
                    (new WorkFlowAssignment(DB, Configuration).GetS(employeeID)).OrderByDescending(x => x.SubmitDateTime).ToList());
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
        }

        [HttpPost("range/{employeeID}")]
        public IActionResult GetRange(string employeeID, [FromBody] ParamTaskFilter p)
        {
            List<WorkFlowAssignment> result = new List<WorkFlowAssignment>();
            try
            {
                if (p.Range == null)
                    result = new WorkFlowAssignment(DB, Configuration).GetS(employeeID);
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
                            result.AddRange(r.Result);
                }
                return ApiResult<List<WorkFlowAssignment>>.Ok(result.OrderByDescending(x => x.SubmitDateTime).ToList());
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }            
        }

        [HttpGet("assignee/{axid}")]
        public IActionResult GetAssignee(string axid)
        {
            try {
                return ApiResult<List<Employee>>.Ok(
                    new WorkFlowTrackingAdapter(Configuration).GetAssignee(Convert.ToInt64(axid)));
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
        }

        [HttpGet("active/{employeeID}")]
        public IActionResult GetActive(string employeeID)
        {
            List<WorkFlowAssignment> result = new List<WorkFlowAssignment>();
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
                        result.AddRange(r.Result);

                return ApiResult<List<WorkFlowAssignment>>.Ok(result.OrderByDescending(x => x.SubmitDateTime).ToList());
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }            
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
            try {
                return ApiResult<object>.Ok(null, _assignment.CountActive(employeeID), null);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
        }

        [HttpPost("approve")]
        public IActionResult Approve([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Approve(p.AXID);
                this.UpdateHistory(p, UpdateRequestStatus.Approved);
                new Notification(Configuration, DB).SendApprovals(p.OriginatorEmployeeID, p.InstanceID);
                return ApiResult<bool>.Ok("Request has been approved");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
        }

        [HttpPost("approve/invert")]
        public IActionResult ApproveInvert([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Reject(p.AXID);
                this.UpdateHistory(p, UpdateRequestStatus.Approved);
                return ApiResult<bool>.Ok("Request has been approved");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
        }

        [HttpPost("reject")]
        public IActionResult Reject([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Reject(p.AXID, p.Notes);
                this.UpdateHistory(p, UpdateRequestStatus.Rejected);
                return ApiResult<bool>.Ok("Request has been rejected");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
        }

        [HttpPost("reject/invert")]
        public IActionResult RejectInvert([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Approve(p.AXID, p.Notes);
                this.UpdateHistory(p, UpdateRequestStatus.Rejected);
                return ApiResult<bool>.Ok("Request has been rejected");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
        }

        [HttpPost("delegate")]
        public IActionResult Delegate([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Delegate(p.DelegateToEmployeeID, p.AXID);
                return ApiResult<bool>.Ok("Request has been delegated");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
        }

        [HttpPost("cancel")]
        public IActionResult Cancel([FromBody] ParamTask p)
        {
            try {
                new WorkFlowTrackingAdapter(Configuration).Cancel(p.AXID);
                this.UpdateHistory(p, UpdateRequestStatus.Cancelled);
                return ApiResult<bool>.Ok("Request has been canceled");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
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
            try {
                return ApiResult<List<WorkFlowAssignment>>.Ok(
                    (new WorkFlowAssignment(DB, Configuration).GetS(employeeID)).OrderByDescending(x => x.SubmitDateTime).ToList());
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"{e.Message}"); }
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
            try
            {
                return ApiResult<List<WorkFlowAssignment>>.Ok(
              new WorkFlowAssignment(DB, Configuration).GetS(employeeID).OrderByDescending(x => x.SubmitDateTime).ToList());
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"{ErrMessage} {employeeID} : {Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpPost("mrange/{employeeID}")]
        public IActionResult MRange(string employeeID, [FromBody] ParamTaskFilter param)
        {
            List<WorkFlowAssignment> result = new List<WorkFlowAssignment>();
            try
            {
                if (param.Range == null)
                {
                    return ApiResult<List<WorkFlowAssignment>>.Ok(
                        new WorkFlowAssignment(DB, Configuration).GetS(employeeID).OrderByDescending(x => x.SubmitDateTime).ToList());
                }
                else
                {
                    var defaultStart = new DateTime(1992, 12, 07);
                    var defaultFinish = DateTime.Now;
                    if (param.Range.Start.Year == 1 && param.Range.Finish.Year == 1) param.Range = new DateRange(defaultStart, defaultFinish);
                    else if (param.Range.Start.Year == 1 && param.Range.Finish.Year != 1) param.Range = new DateRange(defaultStart, param.Range.Finish);
                    else if (param.Range.Start.Year != 1 && param.Range.Finish.Year == 1) param.Range = new DateRange(param.Range.Start, defaultFinish);

                    List<Task<TaskRequest<List<WorkFlowAssignment>>>> tasks = new List<Task<TaskRequest<List<WorkFlowAssignment>>>> {
                        Task.Run(() => {
                            return TaskRequest<List<WorkFlowAssignment>>.Create("AX", new WorkFlowAssignment(DB, Configuration).GetSRange(employeeID, param.Range));
                        }),
                        Task.Run(() => {
                            List<WorkFlowAssignment> workflowdb = new List<WorkFlowAssignment>();
                            foreach (var data in new Survey(DB, Configuration).GetRange(employeeID, param.Range)){
                                workflowdb.Add(new WorkFlowAssignment
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
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"{ErrMessage} {employeeID} :\n{Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpPost("mactive/{employeeID}")]
        public IActionResult MGetActive(string employeeID, [FromBody] ParamTaskFilter param)
        {
            List<WorkFlowAssignment> res = new List<WorkFlowAssignment>();
            DateTime end = DateTime.Now;
            DateTime start = end.AddDays(-30);
            //DateTime start = param.Range.Start;
            DateRange drange = new DateRange(start, end);
            try
            {
                List<Task<TaskRequest<List<WorkFlowAssignment>>>> tasks = new List<Task<TaskRequest<List<WorkFlowAssignment>>>> {
                    Task.Run(() => {
                        return TaskRequest<List<WorkFlowAssignment>>.Create("AX", new WorkFlowAssignment(DB, Configuration).GetSRange(param.Username, drange, true));
                    }),
                    Task.Run(() => {
                        List<WorkFlowAssignment> workflowdb = new List<WorkFlowAssignment>();
                        foreach (var data in new Survey(DB, Configuration).Get(param.Username)) {
                            workflowdb.Add(new WorkFlowAssignment
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
                            });
                        }
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
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"Unable to get active tasks for {param.Username} :\n{Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpGet("active/mcount/{employeeID}")]
        public IActionResult MCountActive(string employeeID)
        {
            try { return ApiResult<object>.Ok(null, _assignment.MCountActive(employeeID), null); }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"Unable to get active task  :\n{Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpGet("massignee/{axid}")]
        public IActionResult MAssignee(string axid)
        {
            try
            {
                return ApiResult<List<Employee>>.Ok(
              new WorkFlowTrackingAdapter(Configuration).GetAssignee(Convert.ToInt64(axid)));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"Unable to get delegation assignee :\n{Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpPost("mapprove")]
        public IActionResult MApprove([FromBody] ParamTask param)
        {
            try
            {
                Console.WriteLine($"step 1 [MApprove] param.AXID = {param.AXID} ");
                new WorkFlowTrackingAdapter(Configuration).Approve(param.AXID);
                UpdateHistory(param, UpdateRequestStatus.Approved);
                new Notification(Configuration, DB).SendNotification(param.ActionEmployeeID, param.InstanceID, "approved");
                return ApiResult<bool>.Ok("Request has been approved");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"Unable to get approve :\n{Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpPost("mreject")]
        public IActionResult MReject([FromBody] ParamTask param)
        {
            try
            {
                new WorkFlowTrackingAdapter(Configuration).Reject(param.AXID, param.Notes);
                UpdateHistory(param, UpdateRequestStatus.Rejected);
                new Notification(Configuration, DB).SendNotification(param.ActionEmployeeID, param.InstanceID, "rejected");
                return ApiResult<bool>.Ok("Request has been rejected");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"Unable to get reject :\n{Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpPost("mdelegate")]
        public IActionResult MDelegate([FromBody] ParamTask param)
        {
            try
            {
                new WorkFlowTrackingAdapter(Configuration).Delegate(param.DelegateToEmployeeID, param.AXID);
                return ApiResult<bool>.Ok("Request has been delegated");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"Unable to get delegate :\n{Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpPost("mcancel")]
        public IActionResult MCancel([FromBody] ParamTask param)
        {
            try
            {
                new WorkFlowTrackingAdapter(Configuration).Cancel(param.AXID);
                UpdateHistory(param, UpdateRequestStatus.Cancelled);
                new Notification(Configuration, DB).SendNotification(param.ActionEmployeeID, param.InstanceID, "canceled");
                return ApiResult<bool>.Ok("Request has been canceled");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"Unable to get cancel :\n{Format.ExceptionString(e)}");
            }
        }
    }
}