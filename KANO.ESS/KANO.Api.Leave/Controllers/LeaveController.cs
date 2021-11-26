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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace KANO.Api.Leave.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LeaveController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Core.Model.Leave _leave;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public LeaveController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _leave = new Core.Model.Leave(DB, Configuration);
        }

        [HttpPost("{employeeID}")]
        public IActionResult Get(string employeeID, [FromBody] DateRange range)
        {            
            try
            {
                var results = _leave.GetS(employeeID, range);
                return ApiResult<List<Core.Model.Leave>>.Ok(results);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading leave :\n{Format.ExceptionString(e)}");
            }            
        }

        [HttpPost("calendar/{employeeID}")]
        public IActionResult GetCalendar(string employeeID, [FromBody] DateRange range)
        {
            try
            {
                var tasks = new List<Task<TaskRequest<object>>>();

                // Fetch leave data
                tasks.Add(Task.Run(() =>
                {
                    var leaves = _leave.GetS(employeeID, range);
                    return TaskRequest<object>.Create("leave", leaves);
                }));

                // Fetch holiday data
                tasks.Add(Task.Run(() =>
                {
                    var adapter = new LeaveAdapter(Configuration);
                    var holidays = adapter.GetHolidays(employeeID, range);
                    return TaskRequest<object>.Create("holiday", holidays);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                // Combine result
                var result = new LeaveCalendar();
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    var Families = new List<Family>();
                    var FamiliesUpdateRequest = new List<Family>();
                    foreach (var r in t.Result)
                        if (r.Label == "holiday")
                            result.Holidays = (List<HolidaySchedule>)r.Result;
                        else
                            result.Leaves = (List<Core.Model.Leave>)r.Result;

                }

                return ApiResult<LeaveCalendar>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get leave '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
           
        }

        [HttpGet("info/{employeeID}")]
        public IActionResult GetInfo(string employeeID)
        {
            var result = new List<LeaveMaintenance>();
            try
            {                
                var adapter = new LeaveAdapter(Configuration);
                result = adapter.GetMaintenance(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading leave :\n{Format.ExceptionString(e)}");
            }
          
            return ApiResult<List<LeaveMaintenance>>.Ok(result);
        }

        [HttpGet("info/all/{employeeID}")]
        public IActionResult GetInfoAll(string employeeID)
        {
            try
            {
                var tasks = new List<Task<TaskRequest<object>>>();

                // Fetch leave data
                tasks.Add(Task.Run(() =>
                {
                    var leaves = _leave.GetPending(employeeID);
                    return TaskRequest<object>.Create("pending", leaves);
                }));

                // Fetch maintenance data
                tasks.Add(Task.Run(() =>
                {
                    var adapter = new LeaveAdapter(Configuration);
                    var maintenance = adapter.GetMaintenance(employeeID);
                    return TaskRequest<object>.Create("maintenance", maintenance);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                // Combine result
                var result = new LeaveInfo();
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    var Families = new List<Family>();
                    var FamiliesUpdateRequest = new List<Family>();
                    foreach (var r in t.Result)
                    {
                        if (r.Label == "maintenance")
                        {
                            result.Maintenances = (List<LeaveMaintenance>)r.Result;
                            if(result.Maintenances != null){
                                result.Maintenances = result.Maintenances.FindAll(x=>x.Remainder > 0);
                                result.TotalRemainder = 0;
                                result.Maintenances.ForEach((maintenance) =>
                                {
                                    result.TotalRemainder += maintenance.Remainder;
                                });
                            }
                        }
                        else
                        {
                            result.TotalPending = 0;
                            var pendingLeaves = (List<Core.Model.Leave>)r.Result;
                            pendingLeaves.ForEach((leave) =>
                            {
                                result.TotalPending += leave.PendingRequest;
                            });

                        }
                    }

                }

                return ApiResult<LeaveInfo>.Ok(result);
            }
            catch (Exception e) {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get leave info '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("save")]
        public IActionResult Save([FromForm] LeaveForm param)
        {
            try
            {
                var leave = JsonConvert.DeserializeObject<Core.Model.Leave>(param.JsonData);
                var strStart = Format.StandarizeDate(leave.Schedule.Start);
                var strFinish = Format.StandarizeDate(leave.Schedule.Finish);

                leave.Upload(Configuration, null, param.FileUpload, x => String.Format("Leave_{0}_{1}_{2}", leave.EmployeeID, strStart, strFinish));

                var adatpter = new WorkFlowRequestAdapter(Configuration);
                var instanceID = adatpter.RequestLeave(leave);
                if (!string.IsNullOrWhiteSpace(instanceID))
                {
                    var updateReq = new UpdateRequest();
                    updateReq.AXRequestID = instanceID;
                    updateReq.EmployeeID = leave.EmployeeID;
                    updateReq.Module = UpdateRequestModule.LEAVE;
                    
                    var strStartFinish = $"{strStart} - {strFinish}";
                    if (strStart == strFinish)
                    {
                        strStartFinish = strStart;
                    }

                    updateReq.Description = $"Leave Request {strStartFinish}";
                    updateReq.Notes = leave.Reason;
                    DB.Save(updateReq);

                    leave.Schedule.Finish = leave.Schedule.Finish.AddHours(23).AddMinutes(59).AddSeconds(59);
                    leave.Reason = leave.Description;
                    leave.AXRequestID = instanceID;
                    DB.Save(leave);

                    // Send approval notification
                    new Notification(Configuration, DB).SendApprovals(leave.EmployeeID, leave.AXRequestID);

                    return ApiResult<object>.Ok($"Leave has been saved successfully");
                }

                throw new Exception("Unable to get AX Request ID");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving leave data :\n{Format.ExceptionString(e)}");
            }            
        }

        [HttpGet("delete/{leaveID}")]
        public IActionResult Delete(string leaveID)
        {

            try
            {
                var leave = DB.GetCollection<Core.Model.Leave>().Find(x => x.Id == leaveID).FirstOrDefault();

                var adp = new WorkFlowTrackingAdapter(Configuration);
                adp.Cancel(Convert.ToInt64(leave.AXRequestID));
                
                var updateRequest = DB.GetCollection<UpdateRequest>().Find(x => x.AXRequestID == leave.AXRequestID).FirstOrDefault();
                updateRequest.Status = UpdateRequestStatus.Cancelled;                
                DB.Save(updateRequest);

                leave.Status = UpdateRequestStatus.Cancelled;
                DB.Save(leave);


                         
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error deleting leave data :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<object>.Ok("Leave has been deleted successfully");
        }

        [HttpGet("type/{employeeID}")]
        public IActionResult GetType(string employeeID)
        {
            var result = new List<LeaveType>();
            try
            {
                var adapter = new LeaveAdapter(Configuration);
                result = adapter.GetLeaveType(employeeID);
            }
            catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error get leave type :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<LeaveType>>.Ok(result);
        }

        [HttpGet("subordinate/{employeeID}")]
        public IActionResult GetSubordinate(string employeeID)
        {
            List<LeaveSubordinate> result = new List<LeaveSubordinate>();
            try
            {
                var adapter = new LeaveAdapter(Configuration);
                result = adapter.GetLeaveSubordinate(employeeID);
            }
            catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error get leave subordinate:\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<LeaveSubordinate>>.Ok(result);
        }

        [HttpGet("history/{employeeID}")]
        public IActionResult GetLeaveHistory(string employeeID)
        {
            List<LeaveHistory> result = new List<LeaveHistory>();
            try
            {
                var adapter = new LeaveAdapter(Configuration);
                result = adapter.GetLeaveHistory(employeeID);
            }
            catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error get leave history: \n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<LeaveHistory>>.Ok(result);
        }

        [HttpPost("holiday/range")]
        public IActionResult GetHolidays([FromBody]HolidayParam param)
        {
            List<HolidaySchedule> result = new List<HolidaySchedule>();
            try
            {                
                var adapter = new LeaveAdapter(Configuration);
                result = adapter.GetHolidays(param.EmployeeID, param.Range);
            }
            catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error get holiday: \n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<HolidaySchedule>>.Ok(result);
        }        

        [HttpGet("subtitution/{employeeID}")]
        public IActionResult GetLeaveSubtitutions(string employeeID)
        {
            List<Employee> result = new List<Employee>();
            try
            {                
                var adapter = new LeaveAdapter(Configuration);
                result = adapter.GetLeaveSubtitutions(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error get subtitution: \n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<Employee>>.Ok(result);
        }

        [HttpGet("get/{employeeID}/{axRequestID}")]
        public IActionResult GetLeave(string employeeID, string axRequestID)
        {
            try
            {
                var result = _leave.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<Core.Model.Leave>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get leave data :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpGet("download/{employeeID}/{axRequestID}")]
        public IActionResult Download(string employeeID, string axRequestID)
        {
            var result = _leave.GetByAXRequestID(employeeID, axRequestID);

            // Download the data
            try
            {
                if (result.Accessible) {
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

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration) ,"success");
        }
    }
}