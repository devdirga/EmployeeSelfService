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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KANO.Api.Employee.Controllers
{
    [Route("api/employee/[controller]")]
    [ApiController]
    public class RetirementController : Controller
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Core.Model.Employee _employee;
        private Retirement _mpp;
        private Core.Model.User _user;        
        private UpdateRequest _updateRequest;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public RetirementController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;

            _employee = new Core.Model.Employee(DB, Configuration);
            _mpp = new Retirement(DB, Configuration);
            _user = new User(DB, Configuration);            
            _updateRequest = new UpdateRequest(DB, Configuration);
        }

        [HttpGet("{employeeID}")]
        public IActionResult Get(string employeeID)
        {           
            Retirement data;
            try 
            {                             
                data = _mpp.Get(employeeID);                
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to mpp request for '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<Retirement>.Ok(data);
        }

        [HttpPost("request")]
        public IActionResult Request([FromForm]RetirementForm param)
        {           
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var data = JsonConvert.DeserializeObject<Retirement>(param.JsonData);
            data.Description = data.Reason;
            try
            {
                data.Upload(Configuration, null, param.FileUpload, x => String.Format("MPPRequest_{0}_{1}", DateTime.Now, x.EmployeeID));
                var instanceID =  adapter.RequestRetirement(data);
                
                if (!string.IsNullOrWhiteSpace(instanceID))
                {                   
                    var updateReq = new UpdateRequest();
                    updateReq.AXRequestID = instanceID;
                    updateReq.EmployeeID = data.EmployeeID;
                    updateReq.Module = UpdateRequestModule.RETIREMENT_REQUEST;                                    

                    updateReq.Description = $"MPP & CB Request";
                    updateReq.Notes = data.Reason;
                    DB.Save(updateReq);
                    
                    data.AXRequestID = instanceID;
                    DB.Save(data);

                    // Send approval notification
                    new Notification(Configuration, DB).SendApprovals(data.EmployeeID, data.AXRequestID);
                    
                    return ApiResult<object>.Ok($"MPP & CB request has been saved");
                }

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Unable to request update to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"MPP & CB request is failed :\n{e.Message}");
            }
        }

        [HttpGet("getbyinstance/{employeeID}/{axRequestID}")]
        public IActionResult GetByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _mpp.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<Core.Model.Retirement>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get recruitment data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("download/{employeeID}/{axRequestID}")]
        public IActionResult Download(string employeeID, string axRequestID)
        {
            var result = _mpp.GetByAXRequestID(employeeID, axRequestID);

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

    }
}
