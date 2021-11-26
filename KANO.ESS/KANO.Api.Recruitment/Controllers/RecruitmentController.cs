using System;
using System.Collections.Generic;
using System.Globalization;
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace KANO.Api.Recruitment.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecruitmentController : Controller
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Core.Model.Employee _employee;
        private Core.Model.Recruitment  _recruitment;
        private Application _application;
        private Core.Model.User _user;        
        private UpdateRequest _updateRequest;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public RecruitmentController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;

            _employee = new Core.Model.Employee(DB, Configuration);
            _recruitment = new Core.Model.Recruitment(DB, Configuration);
            _application = new Application(DB, Configuration);
            _user = new User(DB, Configuration);            
            _updateRequest = new UpdateRequest(DB, Configuration);
        }

        [HttpPost("request/list")]
        public IActionResult GetRequests([FromBody]GridDateRange param)
        {           
            List<Core.Model.Recruitment> data;
            try 
            {                             
                data = _recruitment.GetRequisitions(param.Username);                
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get recruitment request by '{param.Username}' :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Core.Model.Recruitment>>.Ok(data);
        }

        [HttpPost("openings")]
        public IActionResult GetOpenings([FromBody]GridDateRange param)
        {           
            List<Core.Model.Recruitment> data;
            try 
            {                             
                data = _recruitment.GetOpenings(param.Username, param.Range);                
                var recruitmentIDs = data.Select(x=>x.RecruitmentID).Distinct();
                var appliedRecruitment = this.DB.GetCollection<Application>().Find(x => x.EmployeeID == param.Username && recruitmentIDs.Contains(x.RecruitmentID)).ToList();
                foreach(var d in data){
                    d.Application = appliedRecruitment.Find(x=>x.RecruitmentID == d.RecruitmentID);
                }
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get recruitment opening by '{param.Username}' :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Core.Model.Recruitment>>.Ok(data);
        }

        [HttpPost("history")]
        public IActionResult GetHistory([FromBody]GridDateRange param)
        {           
            List<Core.Model.Recruitment> data = new List<Core.Model.Recruitment>();
            try 
            {                             
                var adapter = new RecruitmentAdapter(Configuration);
                data = adapter.GetHistory(param.Username);                
                var options = new ParallelOptions() { MaxDegreeOfParallelism = 5 };
                Parallel.ForEach(data, options, (currentData) =>
                {
                    currentData.Application = adapter.GetApplication(param.Username, currentData.RecruitmentID, false);                                        
                });
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get recruitment history by '{param.Username}' :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Core.Model.Recruitment>>.Ok(data);
        }

        [HttpGet("detail/{employeeID}/{recruitmentID}")]
        public IActionResult GetDetail(string employeeID, string recruitmentID)
        {           
            Application data;
            try 
            {                             
                var adapter = new RecruitmentAdapter(Configuration);
                data = adapter.GetApplication(employeeID, recruitmentID);                
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get recruitment detail by '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<Application>.Ok(data);
        }

        [HttpGet("getbyinstance/{employeeID}/{axRequestID}")]
        public IActionResult GetByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _recruitment.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<Core.Model.Recruitment>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get recruitment data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("job/list")]
        public IActionResult GetJobs()
        {           
            List<Job> data;
            try 
            {                             
                var adapter = new RecruitmentAdapter(Configuration);
                data = adapter.GetJobs();                
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get job data :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Job>>.Ok(data);
        }

        [HttpGet("position/list")]
        public IActionResult GetPositions()
        {           
            List<Position> data;
            try 
            {                             
                var adapter = new RecruitmentAdapter(Configuration);
                data = adapter.GetPositions();                
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get position data :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Position>>.Ok(data);
        }

        [HttpPost("list")]
        public IActionResult GetRecruitments([FromBody]GridDateRange param)
        {           
            List<Core.Model.Recruitment> data;
            try 
            {                             
                var adapter = new RecruitmentAdapter(Configuration);
                data = adapter.GetS(param.Username, param.Range, (RecruitmentStatus) param.Status);      
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to recruitment list :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Core.Model.Recruitment>>.Ok(data);
        }

        [HttpPost("applications")]
        public IActionResult GetApplications([FromBody]GridDateRange param)
        {           
            try 
            {                         
                var adapter = new RecruitmentAdapter(Configuration);    
                var recruitments = adapter.GetS(param.Username, param.Range);
                var recruitmentsIDs = recruitments.Select(x=>x.RecruiterID).Distinct();
                var applications = this.DB.GetCollection<Application>().Find(x => x.EmployeeID == param.Username && recruitmentsIDs.Contains(x.RecruitmentID)).ToList();
                foreach (var recruitment in recruitments) {
                    recruitment.Application = applications.Find(x=>x.RecruitmentID==recruitment.RecruitmentID);
                }

                return ApiResult<List<Core.Model.Recruitment>>.Ok(recruitments);       
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get applications for '{param.Username}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("request")]
        public IActionResult Request([FromForm]RecruitmentForm param)
        {           
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var data = JsonConvert.DeserializeObject<Core.Model.Recruitment>(param.JsonData);
            try
            {
                data.RecruitmentID = "ESS-" + DateTime.Now.Year + "-" + SequenceNo.Get(DB, "ESS-Recruitment-" + DateTime.Now.Year).ClaimAsInt(DB).ToString("0000", new CultureInfo("en-US"));
                data.Upload(Configuration, null, param.FileUpload, x => String.Format("RecruitmentRequest_{0}_{1}", data.CreatedDate, x.EmployeeID));
                var instanceID =  adapter.RequestRecruitment(data);
                
                if (!string.IsNullOrWhiteSpace(instanceID))
                {                    
                    var updateReq = new UpdateRequest();
                    updateReq.AXRequestID = instanceID;
                    updateReq.EmployeeID = data.EmployeeID;
                    updateReq.Module = UpdateRequestModule.RECRUITMENT_REQUEST;

                    updateReq.Description = $"Recruitment Request";
                    updateReq.Notes = data.Reason;
                    DB.Save(updateReq);
                    
                    data.AXRequestID = instanceID;
                    DB.Save(data);

                    // Send approval notification
                    new Notification(Configuration, DB).SendApprovals(data.EmployeeID, data.AXRequestID);
                    return ApiResult<object>.Ok($"Recruitment request has been saved");
                }

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Unable to request update to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Recruitment request is failed :\n{e.Message}");
            }
        }

        [HttpPost("apply")]
        public IActionResult Apply([FromForm]ApplicationForm param)
        {           
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var data = JsonConvert.DeserializeObject<Application>(param.JsonData);
            try
            {
                data.Upload(Configuration, null, param.FileUpload, x => String.Format("Application_{0}_{1}", data.CreatedDate, x.EmployeeID));
                var instanceID =  adapter.ApplyToRecruitment(data);
                
                if (!string.IsNullOrWhiteSpace(instanceID))
                {                   
                    //var updateReq = new UpdateRequest();
                    //updateReq.AXRequestID = instanceID;
                    //updateReq.EmployeeID = data.EmployeeID;
                    //updateReq.Module = UpdateRequestModule.APPLICATION_REQUEST;                                    

                    //updateReq.Description = $"Application";
                    //updateReq.Notes = data.Reason;
                    //DB.Save(updateReq);
                    data.AddHistory(data.ApplicationStatus);
                    data.AXRequestID = instanceID;
                    DB.Save(data);
                    return ApiResult<object>.Ok($"Application request has been saved");
                }

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Unable to request update to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Application request is failed :\n{e.Message}");
            }
        }

        [HttpGet("download/{employeeID}/{axRequestID}")]
        public IActionResult Download(string employeeID, string axRequestID)
        {
            var result = _recruitment.GetByAXRequestID(employeeID, axRequestID);

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

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }
    }
}
