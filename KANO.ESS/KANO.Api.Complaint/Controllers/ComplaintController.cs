using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using System.Net;
using Newtonsoft.Json;
using MongoDB.Driver;
using KANO.Core.Service;
using KANO.Core.Model;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;

using KANO.Core.Service.AX;

using KANO.Core.Lib.Middleware.ServerSideAnalytics.Mongo;

namespace KANO.Api.Complaint.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private TicketRequest _ticket;
        private ComplaintMailTemplate mailTemplate;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public ComplaintController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _ticket = new TicketRequest(DB, Configuration);
        }

        [HttpPost("get")]
        public IActionResult Get([FromBody] KendoGrid param)
        {
            var filter = KendoMongoBuilder<TicketRequest>.BuildFilter(param);
            var sort = KendoMongoBuilder<TicketRequest>.BuildSort(param);

            try
            {
                var tasks = new List<Task<TaskRequest<List<TicketRequest>>>> {
                    Task.Run(() =>
                    {
                    var TicketRequests = DB.GetCollection<TicketRequest>("Tickets")
                                        .Find(filter)
                                        .Limit(param.Take)
                                        .Skip(param.Skip)
                                        .Sort(sort)
                                        .ToList();
                    return TaskRequest<List<TicketRequest>>.Create("DB", TicketRequests);
                    })
                };

                var t = Task.WhenAll(tasks);

                try
                {
                    t.Wait();
                }
                catch (Exception)
                {
                    throw;
                }

                var result = new List<TicketRequest>();

                if (t.Status == TaskStatus.RanToCompletion)
                {
                    var TicketUpdateRequest = new List<TicketRequest>();

                    foreach (var r in t.Result)
                    {
                        if (r.Label == "DB")
                        {
                            TicketUpdateRequest = r.Result;
                        }
                    }
                        
                    foreach (var fur in TicketUpdateRequest)
                    {
                        result.Add(fur);
                    }
                }

                var total = DB.GetCollection<TicketRequest>("Tickets").CountDocuments(filter);

                return ApiResult<List<TicketRequest>>.Ok(result, total);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Fetching ticketRrequest data error :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("request")]
        public new IActionResult Request([FromForm] TicketForm param)
        {
            var updateRequest = new UpdateRequest();
            var data = JsonConvert.DeserializeObject<TicketRequest>(param.JsonData);
            var adapter = new WorkFlowRequestAdapter(Configuration);
            try
            {
                var AXRequestID = "";

                if(data.TicketType == TicketType.Complaint)
                {
                    AXRequestID =  adapter.RequestComplaint(data);
                    if (!string.IsNullOrWhiteSpace(AXRequestID))
                    {
                        updateRequest.Create(AXRequestID, data.EmployeeID, UpdateRequestModule.COMPLAINT, $"Complaint");
                        data.AXRequestID = AXRequestID;
                        DB.Save(updateRequest);

                        // Send approval notification
                        new Notification(Configuration, DB).SendApprovals(data.EmployeeID, data.AXRequestID);
                    }
                    else 
                    {
                        throw new Exception("Unable to request to AX");
                    }                    
                }
                else
                {
                    data.AXRequestID = Tools.RandomInt().ToString();                    
                }
                
                data.Upload(Configuration, null, param.FileUpload, x => String.Format("Ticket_{0}_{1}_{2}", data.CreatedDate, data.CreatedDate, x.EmployeeID));

                DB.Save(data);
                return ApiResult<object>.Ok($"Ticket request has been saved");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Ticket request is failed :\n{e.Message}");
            }

        }

        [HttpPost("updateStatus")]
        public IActionResult RequestUpdateStatus([FromForm] TicketForm param)
        {
            var data = JsonConvert.DeserializeObject<TicketRequest>(param.JsonData);
            try
            {
                TicketRequest Ticket = (_ticket.GetByAXID(data.AXID)).UpdateRequest;
                Ticket.Id = data.Id;
                Ticket.TicketStatus = data.TicketStatus;
                DB.Save(Ticket);
                return ApiResult<object>.Ok($"Ticket request status has been update");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Ticket request update status is failed :\n{e.Message}");
            }
        }

        [HttpGet("list/ticketType")]
        public IActionResult GetTravelType()
        {
            return ApiResult<List<string>>.Ok(new List<string>(Enum.GetNames(typeof(TicketType))));
        }

        [HttpGet("list/ticketStatus")]
        public IActionResult GetTravelStatus()
        {
            return ApiResult<List<string>>.Ok(new List<string>(Enum.GetNames(typeof(TicketStatus))));
        }

        [HttpGet("list/ticketMedia")]
        public IActionResult GetTravelMedia()
        {
            return ApiResult<List<string>>.Ok(new List<string>(Enum.GetNames(typeof(TicketMedia))));
        }

        [HttpGet("ticketcategory/getdata")]
        public IActionResult GetData()
        {
            var TicketCategories = new List<TicketCategory>();
            try
            {
                TicketCategories = DB.GetCollection<TicketCategory>().Find(x => x.EmployeeID != "").ToList();
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<TicketCategory>>.Ok(TicketCategories);
        }

        [HttpPost("ticketcategory/save")]
        public IActionResult Save([FromBody] TicketCategory param)
        {
            try
            {
                if (param.Id == null)
                {
                    DB.Save(param);
                    return ApiResult<object>.Ok(param, "Ticket category has been saved successfully");
                }

                if (param.Id != null && param.Name != "")
                {
                    var oldData = DB.GetCollection<TicketCategory>().Find(x => x.Id == param.Id).FirstOrDefault();
                    if(oldData != null)
                    {
                        param.Id = oldData.Id;
                        DB.Save(param);
                        return ApiResult<object>.Ok(param, "Ticket category has been update successfully");
                    }
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Error updating ticket category ( Ticket category not found)");
                }

                if (param.Id != null && param.Name == "")
                {
                    var oldData = DB.GetCollection<TicketCategory>().Find(x => x.Id == param.Id).FirstOrDefault();
                    if(oldData != null)
                    {
                        param.Id = oldData.Id;
                        DB.Delete(param);
                        return ApiResult<object>.Ok(param, "Ticket category has been delete successfully");
                    }
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Error updating ticket category ( Ticket category not found)");                    
                }
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Error saving ticket category :\n");
            }
            catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving user :\n{Format.ExceptionString(e)}");
            }
            
        }

        [HttpGet("gettemplate")]
        public async Task<IActionResult> GetTemplate()
        {
            mailTemplate = DB.GetCollection<ComplaintMailTemplate>().Find(x => x.Id == "ComplaintTemplate").FirstOrDefault();
            return ApiResult<ComplaintMailTemplate>.Ok(mailTemplate);
        }

    }
}
