using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace KANO.Api.Common.Controllers
{
    [Route("api/common/[controller]")]
    [ApiController]
    public class UpdateRequestController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private UpdateRequest _updateRequest;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public UpdateRequestController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _updateRequest = new UpdateRequest(DB, conf);
        }

        //[HttpGet("updateRequest/{employeeID}/{limit=10}/{offset=0}")]
        //public async Task<IActionResult> GetUpdateRequest(string employeeID, int limit = 10, int offset = 0)
        //{
        //    var result = _updateRequest.GetS(employeeID, limit, offset);
        //    var total = _updateRequest.GetTotal(employeeID);

        //    return ApiResult<List<UpdateRequest>>.Ok(result, total);
        //}

        [HttpGet("{employeeID}")]
        public async Task<IActionResult> GetUpdateRequestDetail(string employeeID)
        {
            List<TrackingRequest> result;
            try
            {
                result = _updateRequest.GetDetail(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get update request for {employeeID} :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<TrackingRequest>>.Ok(result, result.Count);           
        }

        [HttpPost("range/{employeeID}")]
        public async Task<IActionResult> GetRange(string employeeID, [FromBody] ParamTaskFilter param)
        {
            List<TrackingRequest> result;
            try
            {

                if (param.Range == null)
                    result = _updateRequest.GetDetail(employeeID);
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

                    result = _updateRequest.GetDetailRange(employeeID, param.Range);
                }
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get tasks for {employeeID} :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<TrackingRequest>>.Ok(result.OrderByDescending(x => x.SubmitDateTime).ToList());
        }


        [HttpGet("{employeeID}/{instanceID}")]
        public async Task<IActionResult> GetUpdateRequestDetail(string employeeID, string instanceID)
        {
            TrackingRequest result;
            try
            {
                var data = _updateRequest.GetDetail(employeeID, instanceID);
                result = data.First();
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get update request for {employeeID} :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<TrackingRequest>.Ok(result);
        }
    }
}