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

namespace KANO.Api.Training.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TrainingController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Core.Model.Training _training;
        private TrainingAdapter _trainingAdapter;
        private UpdateRequest _updateRequest;

        public TrainingController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;

            _trainingAdapter = new TrainingAdapter(Configuration);            
            _training = new Core.Model.Training(DB, Configuration);            
            _updateRequest = new UpdateRequest(DB, Configuration);
        }

        [HttpPost("get")]
        public IActionResult GetTrainings(GridDateRange param)
        {
            try
            {
                var trainings = _training.GetSWithRegistrationDetail(param.Username, param.Range, param.Status);                
                return ApiResult<List<Core.Model.Training>>.Ok(trainings);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get trainings :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("history")]
        public IActionResult GetHistory([FromBody] GridDateRange param)
        {
            try
            {
                var trainings = _trainingAdapter.GetHistory(param.Username, true);
                return ApiResult<List<Core.Model.Training>>.Ok(trainings);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get trainings :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("list/type")]
        public IActionResult GetTypes()
        {
            try
            {
                var data = _trainingAdapter.GetTypes();                
                return ApiResult<List<TrainingType>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get training types :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("list/subType")]
        public IActionResult GetSubTypes()
        {
            try
            {
                var data = _trainingAdapter.GetSubTypes();                
                return ApiResult<List<TrainingSubType>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get training sub types :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("references/{employeeID}")]
        public IActionResult GetReferences(string employeeID)
        {
            try
            {
                var result = _training.GetReferences(employeeID);
                return ApiResult<List<TrainingReference>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get training references :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] TrainingRegistration param)
        {
            var adapter = new WorkFlowRequestAdapter(Configuration);
            //var updateRequest = new UpdateRequest();
            try
            {
                param.RegistrationDate = DateTime.Now;
                var training = _trainingAdapter.Get(param.TrainingID);
                if (training != null) {                    
                    var AXRequestID = adapter.RegisterTraining(param) ;
                    
                    param.AXRequestID = AXRequestID;
                    //updateRequest.Create(AXRequestID, param.EmployeeID, UpdateRequestModule.TRAINING_REGISTRATION, $"Training registration");
                    //updateRequest.AXRequestID = AXRequestID;

                    //DB.Save(updateRequest);
                    DB.Save(param);
                                        
                    return ApiResult<object>.Ok(param, "Employee has been registered successfully");
                }
                
                throw new Exception($"Unable to find training with id : {param.TrainingID}");                                
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to register trainings :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }
    }
}
