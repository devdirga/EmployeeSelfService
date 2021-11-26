using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using System.Net;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using KANO.Core.Service.AX;
using System.IO;

namespace KANO.Api.Travel.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TravelController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private readonly IConfiguration Configuration;
        private Core.Model.Travel _travel;

        public TravelController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _travel = new Core.Model.Travel(DB, Configuration);
        }

        [HttpPost("get")]
        public IActionResult GetTravel([FromBody]GridDateRange param)
        {
            try
            {
                var range = new DateRange(param.Range.Start, param.Range.Finish);
                var result = _travel.GetS(param.Username, range);

                if (param.Status > -1) {
                    result = result.FindAll(x => x.TravelRequestStatus == (KESSTEServices.KESSTrvExpTravelReqStatus) param.Status);
                }

                return ApiResult<List<Core.Model.Travel>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get travel request '' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("get/status/{employeeID}/{status}")]
        public IActionResult GetTravelByStatus(string employeeID, long status)
        {
            var travel = new List<Core.Model.Travel>();
            var adapter = new TravelAdapter(Configuration);
            try
            {
                travel = adapter.GetTravelByStatus(employeeID, (KESSTEServices.KESSTrvExpTravelReqStatus) status);                
                return ApiResult<List<Core.Model.Travel>>.Ok(travel);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get travel request '' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("request")]
        public IActionResult TravelRequest([FromForm]TravelForm param)
        {
            var action = ActionType.Create;
            var strAction = Enum.GetName(typeof(ActionType), action);            
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var data = JsonConvert.DeserializeObject<Core.Model.Travel>(param.JsonData);

            if (string.IsNullOrWhiteSpace(data.RequestForID) && !data.IsGuest)
            {
                data.RequestForID = data.EmployeeID;
                data.RequestForName = data.EmployeeName;
            }

            try
            {
                data.Upload(Configuration, null, param.FileUpload, x => String.Format("Travel_{0}_{1}_{2}", data.Schedule.Start, data.Schedule.Finish, x.EmployeeID));
                var AXID =  adapter.RequestTravelExpense(data);
                
                if (long.Parse(AXID) > 0)
                {                   
                    data.AXID = long.Parse(AXID);
                    //data.Upload(Configuration, null, param.FileUpload, x => String.Format("Travel_{0}_{1}_{2}", data.Schedule.Start, data.Schedule.Finish, x.EmployeeID));
                    data.Action = action;

                    DB.Save(data);
                    return ApiResult<object>.Ok($"Travel request has been saved");
                }

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Unable to request update to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Travel request is failed :\n{e.Message}");
            }

        }   
        
        [HttpPost("revise")]
        public IActionResult TravelRevision([FromForm]TravelForm param)
        {
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var data = JsonConvert.DeserializeObject<Core.Model.Travel>(param.JsonData);

            if (string.IsNullOrWhiteSpace(data.RequestForID) && !data.IsGuest)
            {
                data.RequestForID = data.EmployeeID;
                data.RequestForName = data.EmployeeName;
            }

            var oldData = _travel.GetByTravelID(data.EmployeeID, data.TravelID).Travel;

            try
            {
                var AXID = adapter.RevisionTravelExpense(data);

                if (long.Parse(AXID) > 0)
                {
                    data.AXID = long.Parse(AXID);

                    if (param.FileUpload != null)
                    {
                        data.Upload(Configuration, oldData, param.FileUpload, x => String.Format("Travel_{0}_{1}_{2}", data.Schedule.Start, data.Schedule.Finish, x.EmployeeID));
                    }
                    else
                    {
                        data.Filepath = oldData.Filepath;
                        data.Filename = oldData.Filename;
                    }

                    data.Id = oldData.Id;
                    DB.Save(data);
                    return ApiResult<object>.Ok($"Travel request has been revised");
                }

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Unable to request revision to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Travel revision is failed :\n{e.Message}");
            }
        }

        [HttpGet("list/purposes")]
        public IActionResult GetPurpose()
        {
            var adapter = new TravelAdapter(Configuration);
            List<TravelPurpose> result = new List<TravelPurpose>();            
            try
            {
                result = adapter.GetTravelPurposes();
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get travel purposes '' :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<TravelPurpose>>.Ok(result);
        }

        [HttpGet("list/transportations")]
        public IActionResult GetTransportations()
        {
            var adapter = new TravelAdapter(Configuration);
            List<Transportation> result = new List<Transportation>();
            try
            {
                result = adapter.GetTransportations();
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get transportation '' :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<Transportation>>.Ok(result);
        }

        [HttpGet("download/attachment/{employeeID}/{travelRequestID}/{AXID}")]
        public IActionResult DownloadAttachment(string employeeID, string travelRequestID, string AXID)
        {
            var bytes = new byte[] { };

            var adapter = new TravelAdapter(Configuration);
            try
            {
                var travel = adapter.GetTravelByTravelRequestID(employeeID, travelRequestID);
                if (travel.SPPD.Count() > 0) {
                    var attachment = travel.SPPD[0].Attachments.Find(x=>x.AXID == long.Parse(AXID));
                    if (attachment != null) {
                        return File(bytes, "application/force-download", attachment.Filepath);
                    }
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get sppd attachment");
                }
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get sppd in travel {travelRequestID}");

            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get sppd list '' :\n{Format.ExceptionString(e)}");
            }            
        }

        [HttpGet("download/{employeeID}/{travelRequestID}")]
        public IActionResult Download(string employeeID, string travelRequestID)
        {
            var bytes = new byte[] { };

            var adapter = new TravelAdapter(Configuration);
            try
            {
                var travel = adapter.GetTravelByTravelRequestID(employeeID, travelRequestID, true);
                if (travel != null)
                {
                    return File(bytes, "application/force-download", travel.Filepath);
                }
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get sppd in travel {travelRequestID}");

            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get sppd list '' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("sppd/{axid}")]
        public IActionResult GetListSPPD(string sppdID)
        {
            List<SPPD> sppd = new List<SPPD>();
            var adapter = new TravelAdapter(Configuration);
            try
            {
                 sppd = adapter.GetSPPD(sppdID);
               
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get sppd list '' :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<SPPD>>.Ok(sppd);
        }

        [HttpGet("get/{employeeID}/{axRequestID}")]
        public IActionResult GetTravelByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _travel.GetByAXRequestID(employeeID, axRequestID, true);
                return ApiResult<Core.Model.Travel>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get travel data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("getbyinstance/{employeeID}/{axRequestID}")]
        public IActionResult GetByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _travel.GetByInstanceID(employeeID, axRequestID, false);
                return ApiResult<Core.Model.Travel>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get travel data :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpGet("list/travelStatus")]
        public IActionResult GetMedicalType()
        {
            return ApiResult<List<string>>.Ok(TravelAdapter.GetTravelStatus());
        }

        [HttpGet("list/travelType")]
        public IActionResult GetTravelType()
        {
            return ApiResult<List<string>>.Ok(TravelAdapter.GetTravelType());
        }

        [HttpGet("close/{travelRequestID}")]
        public IActionResult Close(string travelRequestID)
        {
            var adapter = new WorkFlowRequestAdapter(Configuration);
            try
            {
                var result = adapter.CloseTravelExpense(travelRequestID);
                return ApiResult<object>.Ok(result, $"Travel \"{travelRequestID}\" has been closed successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Close travel is failed :\n{e.Message}");
            }

        }

        
        [HttpGet("downloads/{axID}/{axDocID}")]
        public IActionResult Downloads(string axID, string axDocID)
        {
            //var bytes = new byte[] { };

            var adapter = new TravelAdapter(Configuration);
            try
            {
                var travel = adapter.GetTravelByAxID(axID, true);

                var filepath = string.Empty;

                foreach (var document in travel.DocumentList)
                {
                    if (long.Parse(axDocID) == document.AXID)
                    {
                        var bytes = document.Download();
                        filepath = Path.GetFileName(document.Filepath);
                        return File(bytes, "application/force-download", filepath);
                    }
                }
              
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get sppd in travel {axID}");

            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get sppd list '' :\n{Format.ExceptionString(e)}");
            }
        }
        
        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }
    }
}
