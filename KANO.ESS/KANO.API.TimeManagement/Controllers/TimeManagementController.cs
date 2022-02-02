using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using MongoDB.Driver;
using Newtonsoft.Json;

namespace KANO.Api.TimeManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeManagementController : ControllerBase
    {

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private TimeAttendance _timeAttendance;
        private Core.Model.Agenda _agenda;
        private readonly String UnableGetTimeAttendance = "Unable to get TimeAttendance for";

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public TimeManagementController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _timeAttendance = new TimeAttendance(DB, Configuration);
            _agenda = new Core.Model.Agenda(DB, Configuration);
        }
        
        [HttpGet("{employeeID}")]
        public async Task<IActionResult> Gets(string employeeID)
        {
            try
            {
                var range = new DateRange(DateTime.Now.AddMonths(-1), DateTime.Now);
                var result = _timeAttendance.GetS(employeeID, range);
                return ApiResult<List<TimeAttendanceResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get TimeAttendance '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }
        
        [HttpPost("get/range")]
        public async Task<IActionResult> GetRange([FromBody]GridDateRange param)
        {
            try
            {
                var range = new DateRange(param.Range.Start, param.Range.Finish);
                var result = _timeAttendance.GetS(param.Username, range);
                return ApiResult<List<TimeAttendanceResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get TimeAttendance '' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("subordinate/{employeeID}")]
        public async Task<IActionResult> GetSubordinate(string employeeID)
        {
            try
            {
                var range = new DateRange(DateTime.Now.AddMonths(-1), DateTime.Now);
                var adapter = new TimeManagementAdapter(Configuration);
                var data = adapter.GetSubordinate(employeeID, range);

                return ApiResult<List<TimeAttendance>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<List<TimeAttendance>>.Error(HttpStatusCode.BadRequest, $"Error loading time attendance :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("subordinate/range")]
        public async Task<IActionResult> GetSubordinateRange([FromBody]GridDateRange param)
        {
            try
            {
                var adapter = new TimeManagementAdapter(Configuration);
                var data = adapter.GetSubordinate(param.Username, param.Range);

                return ApiResult<List<TimeAttendance>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<List<TimeAttendance>>.Error(HttpStatusCode.BadRequest, $"Error loading time attendance :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("get/period")]
        public async Task<IActionResult> GetPeriodTable()
        {
            try
            {
                var adapter = new TimeManagementAdapter(Configuration);
                var data = adapter.GetPeriodTable();

                return ApiResult<List<Period>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<List<Period>>.Error(HttpStatusCode.BadRequest, $"Error loading period :\n{Format.ExceptionString(e)}");
            }
            
        }

        [HttpGet("absencecode/get")]
        public async Task<IActionResult> Get()
        {
            try
            {
                var adapter = new HRAdapter(Configuration);
                var data = adapter.Get();

                return ApiResult<List<AbsenceCode>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<List<TimeAttendance>>.Error(HttpStatusCode.BadRequest, $"Error loading time attendance :\n{Format.ExceptionString(e)}");
            }
        }       

        private string fileUpload(IEnumerable<IFormFile> FileUpload) {
            var uploadDirectory = Tools.UploadPathConfiguration(Configuration);
            var file = FileUpload.FirstOrDefault();
            var newFilename = String.Format("AbsenceRecomendation_{0}{1}", DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), Path.GetExtension(file.FileName));
            var newFilepath = Path.Combine(uploadDirectory, newFilename);

            // Upload file
            using (var fileStream = new FileStream(newFilepath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            return newFilepath;
        }

        [HttpGet("AbsenceImported/{employeeID}")]
        public async Task<IActionResult> GetAbsenceImported(string employeeID)
        {
            try
        {
                var adapter = new TimeManagementAdapter(Configuration);
                var data = adapter.GetAbsenceImported(employeeID);

                return ApiResult<List<AbsenceImported>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<List<AbsenceImported>>.Error(HttpStatusCode.BadRequest, $"Error loading Absence Imported :\n{Format.ExceptionString(e)}");
            }
        }


        [HttpPost("timeattendance/create")]
        public IActionResult CreateTimeAttendance([FromForm] TimeAttendanceForm param, ActionType action = ActionType.Create)
        {
            var workflowAdapter = new WorkFlowRequestAdapter(Configuration);
            var strAction = Enum.GetName(typeof(ActionType), action);
            try
            {
                string Filepath = string.Empty;
                var adapter = new WorkFlowRequestAdapter(Configuration);

                TimeAttendance oldData = new TimeAttendance();
                TimeAttendance data = new TimeAttendance();
                data = JsonConvert.DeserializeObject<TimeAttendance>(param.JsonData);

                // Get existing request
                var instanceID = workflowAdapter.GetAbsenceInstanceID(data.EmployeeID, data.LoggedDate);
                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    // Requesting new time attendance
                    instanceID = adapter.RequestTimeAttendance(data);
                }

                // var instanceID = adapter.RequestTimeAttendance(data);

                oldData = DB.GetCollection<TimeAttendance>()
                    .Find(x => x.AXID == data.AXID && x.EmployeeID == data.EmployeeID && (x.Status == UpdateRequestStatus.InReview) && (x.LoggedDate == data.LoggedDate))
                    .FirstOrDefault();
                data.Upload(Configuration, oldData, param.FileUpload, x => String.Format("AbsenceRecomendation_{0}_{1}", DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), x.EmployeeID));                

                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    throw new Exception("Unable to get AX Request ID");
                }

                // Store to workflow
                UpdateRequest updateRequest = new UpdateRequest
                {
                    AXRequestID = instanceID,
                    EmployeeID = data.EmployeeID,
                    Module = UpdateRequestModule.UPDATE_TIMEATTENDANCE,
                    Notes = data.Reason,
                    Description = $"Absence Recomendation {Format.StandarizeDate(data.LoggedDate)}",
                    UpdateBy = data.EmployeeName
                };                                

                data.Action = action;
                data.AXRequestID = instanceID;

                var tasks = new List<Task<TaskRequest<object>>>();

                tasks.Add(Task.Run(() =>
                {
                    DB.Save(data);
                    return TaskRequest<object>.Create("TimeAttendance", true);
                }));

                tasks.Add(Task.Run(() => {
                    DB.Save(updateRequest);
                    return TaskRequest<object>.Create("UpdateRequest", true);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                // Send approval notification
                new Notification(Configuration, DB).SendApprovals(data.EmployeeID, data.AXRequestID);

                return ApiResult<object>.Ok($"TimeAttendance draft '{strAction}' request has been saved");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"TimeAttendance draft '{strAction}' is failed :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("timeattendance/update")]
        public IActionResult UpdateTimeAttendance([FromForm] TimeAttendanceForm param)
        {
            return this.CreateTimeAttendance(param, ActionType.Update);
        }

        [HttpGet("timeattendance/remove/{requestID}")]
        public IActionResult DiscardAbsenceRecomendation(string requestID = "")
        {
            var adapter = new WorkFlowTrackingAdapter(Configuration);
            //adapter.Cancel(Convert.ToInt64(AXID));
            
            
            try
            {
                var ta = DB.GetCollection<TimeAttendance>().Find(x => x.Id == requestID).FirstOrDefault();
                if (ta != null)
                {

                    var yy = DB.GetCollection<UpdateRequest>().Find(x => x.AXRequestID == ta.AXRequestID).FirstOrDefault();
                    yy.Status = UpdateRequestStatus.Cancelled;                    

                    ta.Status = UpdateRequestStatus.Cancelled;
                    DB.Save(ta);

                    var adp = new WorkFlowTrackingAdapter(Configuration);
                    adp.Cancel(Convert.ToInt64(ta.AXRequestID));
                    
                }
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to discard Time Attendance request :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<object>.Ok($"Time Attendance data request has been discarded ");
        }

        [HttpGet("timeattendance/get/{employeeID}/{axRequestID}")]
        public IActionResult GetAbsenceRecomendation(string employeeID, string axRequestID)
        {
            try
            {
                var result = _timeAttendance.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<TimeAttendance>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get absence recomendation :\n{Format.ExceptionString(e)}");
            }          
        }

        [HttpGet("timeattendance/download/{id}")]
        public IActionResult Download(string id)
        {
            var result = _timeAttendance.GetByID(id);

            // Download the data
            try
            {
                var bytes = result.Download();
                return File(bytes, "application/force-download", Path.GetFileName(result.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("download/{employeeID}/{axRequestID}")]
        public IActionResult Download(string employeeID, string axRequestID)
        {
            var result = _timeAttendance.GetByAXRequestID(employeeID, axRequestID);

            // Download the data
            try
            {
                if (result.Accessible)
                {
                    var bytes = result.Download();
                    return File(bytes, "application/force-download", Path.GetFileName(result.Filepath));
                }

                throw new Exception("Unable to access file");
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPost("agendaget/range")]
        public IActionResult AgendaGetRange([FromBody] GridDateRange param)
        {
            try
            {
                var result = _agenda.GetS(param.Username, param.Range);
                return ApiResult<List<Core.Model.Agenda>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get agenda '' :\n{Format.ExceptionString(e)}");
            }
            //return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [Authorize]
        [HttpGet("m/{employeeID}")]
        public IActionResult MGets(string employeeID)
        {
            try
            {
                return ApiResult<List<TimeAttendanceResult>>.Ok(
              _timeAttendance.GetS(employeeID, new DateRange(DateTime.Now.AddMonths(-1), DateTime.Now)));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"{UnableGetTimeAttendance} {employeeID} :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpPost("mget/range")]
        public IActionResult MGetRange([FromBody] GridDateRange param)
        {
            try
            {
                return ApiResult<List<TimeAttendanceResult>>.Ok(
                  _timeAttendance.GetS(param.Username, new DateRange(param.Range.Start, param.Range.Finish)));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"{UnableGetTimeAttendance} {param.Username} :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("msubordinate/{employeeID}")]
        public IActionResult MGetSubordinate(string employeeID)
        {
            try
            {
                return ApiResult<List<TimeAttendance>>.Ok(
              new TimeManagementAdapter(Configuration).GetSubordinate(employeeID, new DateRange(DateTime.Now.AddMonths(-1), DateTime.Now)));
            }
            catch (Exception e)
            {
                return ApiResult<List<TimeAttendance>>.Error(
  HttpStatusCode.BadRequest, $"{UnableGetTimeAttendance} subordinate {employeeID} :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpPost("msubordinate/range")]
        public IActionResult MGetSubordinateRange([FromBody] GridDateRange param)
        {
            try
            {
                return ApiResult<List<TimeAttendance>>.Ok(
              new TimeManagementAdapter(Configuration).GetSubordinate(param.Username, param.Range));
            }
            catch (Exception e)
            {
                return ApiResult<List<TimeAttendance>>.Error(
  HttpStatusCode.BadRequest, $"{UnableGetTimeAttendance} subordinate {param.Username} :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mget/period")]
        public IActionResult MGetPeriodTable()
        {
            try
            {
                return ApiResult<List<Period>>.Ok(
                  new TimeManagementAdapter(Configuration).GetPeriodTable());
            }
            catch (Exception e)
            {
                return ApiResult<List<Period>>.Error(
HttpStatusCode.BadRequest, $"Error loading period :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mabsencecode/get")]
        public IActionResult MGet()
        {
            try
            {
                return ApiResult<List<AbsenceCode>>.Ok(
                  new HRAdapter(Configuration).Get());
            }
            catch (Exception e)
            {
                return ApiResult<List<TimeAttendance>>.Error(
  HttpStatusCode.BadRequest, $"Error loading time attendance :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mabsenceimported/{employeeID}")]
        public IActionResult MGetAbsenceImported(string employeeID)
        {
            try
            {
                return ApiResult<List<AbsenceImported>>.Ok(
              new TimeManagementAdapter(Configuration).GetAbsenceImported(employeeID));
            }
            catch (Exception e)
            {
                return ApiResult<List<AbsenceImported>>.Error(
HttpStatusCode.BadRequest, $"Error loading Absence Imported :\n{Format.ExceptionString(e)}");
            }
        }
        [HttpPost("mtimeattendance/create")]
        public IActionResult MCreateTimeAttendance([FromForm] TimeAttendanceForm param)
        {
            var workflowAdapter = new WorkFlowRequestAdapter(Configuration);
            var strAction = Enum.GetName(typeof(ActionType), ActionType.Create);
            try
            {
                string Filepath = string.Empty;
                TimeAttendance oldData = new TimeAttendance();
                TimeAttendance data = JsonConvert.DeserializeObject<TimeAttendance>(param.JsonData);
                // Get existing request
                var instanceID = workflowAdapter.GetAbsenceInstanceID(data.EmployeeID, data.LoggedDate);
                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    instanceID = new WorkFlowRequestAdapter(Configuration).RequestTimeAttendance(data);
                }
                oldData = DB.GetCollection<TimeAttendance>()
                    .Find(x => x.AXID == data.AXID && x.EmployeeID == data.EmployeeID &&
                    (x.Status == UpdateRequestStatus.InReview) && (x.LoggedDate == data.LoggedDate))
                    .FirstOrDefault();

                data.Upload(Configuration, oldData, param.FileUpload,
                    x => String.Format("AbsenceRecomendation_{0}_{1}",
                    DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), x.EmployeeID));

                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    throw new Exception("Unable to get AX Request ID");
                }
                data.Action = ActionType.Create;
                data.AXRequestID = instanceID;
                var tasks = new List<Task<TaskRequest<object>>>
                {
                    Task.Run(() =>
                    {
                        DB.Save(data);
                        return TaskRequest<object>.Create("TimeAttendance", true);
                    }),

                    Task.Run(() =>
                    {
                        DB.Save(new UpdateRequest {
                                    AXRequestID = instanceID,
                                    EmployeeID = data.EmployeeID,
                                    Module = UpdateRequestModule.UPDATE_TIMEATTENDANCE,
                                    Notes = data.Reason,
                                    Description = $"Absence Recomendation {Format.StandarizeDate(data.LoggedDate)}",
                                    UpdateBy = data.EmployeeName
                                });
                        return TaskRequest<object>.Create("UpdateRequest", true);
                    })
                };

                var t = Task.WhenAll(tasks);
                try { t.Wait(); }
                catch (Exception) { throw; }

                // Send approval notification
                new Notification(Configuration, DB).SendNotification(data.EmployeeID, data.AXRequestID);
                return ApiResult<object>.Ok($"TimeAttendance draft '{strAction}' request has been saved");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"TimeAttendance draft '{strAction}' is failed :\n{Format.ExceptionString(e)}");
            }
        }
        [HttpPost("mtimeattendance/update")]
        public IActionResult MUpdateTimeAttendance([FromForm] TimeAttendanceForm param)
        {
            var workflowAdapter = new WorkFlowRequestAdapter(Configuration);
            var strAction = Enum.GetName(typeof(ActionType), ActionType.Update);
            try
            {
                string Filepath = string.Empty;
                TimeAttendance oldData = new TimeAttendance();
                TimeAttendance data = new TimeAttendance();
                data = JsonConvert.DeserializeObject<TimeAttendance>(param.JsonData);
                // Get existing request
                var instanceID = workflowAdapter.GetAbsenceInstanceID(data.EmployeeID, data.LoggedDate);
                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    instanceID = new WorkFlowRequestAdapter(Configuration).RequestTimeAttendance(data);
                }
                oldData = DB.GetCollection<TimeAttendance>()
                    .Find(x => x.AXID == data.AXID && x.EmployeeID == data.EmployeeID &&
                    (x.Status == UpdateRequestStatus.InReview) && (x.LoggedDate == data.LoggedDate))
                    .FirstOrDefault();

                data.Upload(Configuration, oldData, param.FileUpload, x => String.Format("AbsenceRecomendation_{0}_{1}",
                    DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), x.EmployeeID));

                if (string.IsNullOrWhiteSpace(instanceID))
                {
                    throw new Exception("Unable to get AX Request ID");
                }
                data.Action = ActionType.Update;
                data.AXRequestID = instanceID;
                var tasks = new List<Task<TaskRequest<object>>>
                {
                    Task.Run(() =>
                    {
                        DB.Save(data);
                        return TaskRequest<object>.Create("TimeAttendance", true);
                    }),

                    Task.Run(() =>
                    {
                        DB.Save(new UpdateRequest {
                                    AXRequestID = instanceID,
                                    EmployeeID = data.EmployeeID,
                                    Module = UpdateRequestModule.UPDATE_TIMEATTENDANCE,
                                    Notes = data.Reason,
                                    Description = $"Absence Recomendation {Format.StandarizeDate(data.LoggedDate)}",
                                    UpdateBy = data.EmployeeName
                                });
                        return TaskRequest<object>.Create("UpdateRequest", true);
                    })
                };

                var t = Task.WhenAll(tasks);
                try { t.Wait(); }
                catch (Exception) { throw; }

                // Send approval notification
                new Notification(Configuration, DB).SendNotification(data.EmployeeID, data.AXRequestID);
                return ApiResult<object>.Ok($"TimeAttendance draft '{strAction}' request has been saved");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"TimeAttendance draft '{strAction}' is failed :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mtimeattendance/remove/{requestID}")]
        public IActionResult MDiscardAbsenceRecomendation(string requestID = "")
        {
            try
            {
                TimeAttendance timeAttendance = DB.GetCollection<TimeAttendance>().Find(a => a.Id == requestID).FirstOrDefault();
                if (!timeAttendance.Equals(null))
                {

                    UpdateRequest updateRequest = DB.GetCollection<UpdateRequest>().Find(x => x.AXRequestID == timeAttendance.AXRequestID).FirstOrDefault();
                    updateRequest.Status = UpdateRequestStatus.Cancelled;
                    DB.Save(updateRequest);
                    timeAttendance.Status = UpdateRequestStatus.Cancelled;
                    DB.Save(timeAttendance);
                    new WorkFlowTrackingAdapter(Configuration).Cancel(Convert.ToInt64(timeAttendance.AXRequestID));
                    return ApiResult<object>.Ok($"Time Attendance data request has been discarded");
                }
                return ApiResult<object>.Ok($"Time Attendance has not found");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.BadRequest, $"Unable to discard Time Attendance request :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mtimeattendance/get/{employeeID}/{axRequestID}")]
        public IActionResult MGetAbsenceRecomendation(string employeeID, string axRequestID)
        {
            try
            {
                return ApiResult<TimeAttendance>.Ok(
                  _timeAttendance.GetByAXRequestID(employeeID, axRequestID));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
  HttpStatusCode.BadRequest, $"Unable to get absence recomendation :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mtimeattendance/download/{id}")]
        public IActionResult MDownload(string id)
        {
            var result = _timeAttendance.GetByID(id);
            try { return File(result.Download(), "application/force-download", Path.GetFileName(result.Filepath)); }
            catch (Exception e) { throw e; }
        }
        [HttpGet("mdownload/{employeeID}/{axRequestID}")]
        public IActionResult MDownload(string employeeID, string axRequestID)
        {
            var result = _timeAttendance.GetByAXRequestID(employeeID, axRequestID);
            try
            {
                if (result.Accessible) { return File(result.Download(), "application/force-download", Path.GetFileName(result.Filepath)); }
                throw new Exception("Unable to access file");
            }
            catch (Exception e) { throw e; }
        }
        [Authorize]
        [HttpPost("magendaget/range")]
        public IActionResult MAgendaGetRange([FromBody] GridDateRange param)
        {
            try
            {
                return ApiResult<List<Core.Model.Agenda>>.Ok(
                  _agenda.GetRangeMobile(param.Username, param.Range));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
  HttpStatusCode.BadRequest, $"Unable to get agenda '' :\n{Format.ExceptionString(e)}");
            }
        }
        [HttpGet("magendadownload/{employeeID}")]
        public IActionResult MAgendaDownload(string employeeID)
        {
            var token = employeeID.Replace("_", @"/");
            // Download the data
            try
            {
                var file = new FieldAttachment();
                var decodedToken = WebUtility.UrlDecode(token);
                file.Filepath = Hasher.Decrypt(decodedToken);
                var bytes = file.Download();
                return File(bytes, "application/force-download", file.Filename);
            }
            catch (Exception e) { throw e; }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }

    }
}