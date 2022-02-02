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
using Microsoft.AspNetCore.Authorization;
using System.IO;

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

                if(data.TicketType == KESSWRServices.KESSTicketType.Complaint)
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

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [Authorize]
        [HttpPost("mget")]
        public IActionResult MGet([FromBody] KendoGrid p)
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;
            List<KendoFilter> filters = p.Filter.Filters;
            bool isFilterDate = false;
            foreach (var fil in filters)
                if (fil.Field.Equals("CreatedDate"))
                {
                    isFilterDate = true;
                    if (fil.Operator.Equals("gte")) start = DateTime.Parse(fil.Value);
                    if (fil.Operator.Equals("lte")) end = DateTime.Parse(fil.Value);
                }
            try
            {
                List<Task<TaskRequest<List<TicketRequest>>>> tasks = new List<Task<TaskRequest<List<TicketRequest>>>> {
                    Task.Run(() => {
                        return TaskRequest<List<TicketRequest>>.Create("DB", DB.GetCollection<TicketRequest>()
                            .Find(KendoMongoBuilder<TicketRequest>.BuildFilter(p)).Limit(p.Take).Skip(p.Skip)
                            .Sort(KendoMongoBuilder<TicketRequest>.BuildSort(p)).ToList());
                    })
                };
                Task<TaskRequest<List<TicketRequest>>[]> t = Task.WhenAll(tasks);
                try { t.Wait(); }
                catch (Exception) { throw; }
                List<TicketRequest> result = new List<TicketRequest>();
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    List<TicketRequest> TicketUpdateRequest = new List<TicketRequest>();
                    foreach (var r in t.Result)
                        if (r.Label == "DB")
                        {
                            if (!isFilterDate) TicketUpdateRequest = r.Result;
                            else TicketUpdateRequest = r.Result.FindAll(a => a.CreatedDate >= start && a.CreatedDate <= end).ToList();
                        }
                    foreach (var fur in TicketUpdateRequest)
                        result.Add(fur);
                }
                return ApiResult<List<TicketRequest>>.Ok(result, result.Count);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [Authorize]
        [HttpPost("mgetresolution")]

        public IActionResult MGetResolution([FromBody] KendoGrid p)
        {
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;
            List<KendoFilter> filters = p.Filter.Filters;
            bool isFilterDate = false;
            foreach (var filter in filters)
                if (filter.Field.Equals("CreatedDate"))
                {
                    isFilterDate = true;
                    if (filter.Operator.Equals("gte")) start = DateTime.Parse(filter.Value);
                    if (filter.Operator.Equals("lte")) end = DateTime.Parse(filter.Value);
                }
            List<string> categs = new List<string>();
            foreach (var tc in DB.GetCollection<TicketCategory>().Find(_ => true).ToList())
                if (tc.Contacts.Select(a => a.EmployeeID).Contains(p.EmployeeID))
                    categs.Add(tc.Id);
            FilterDefinition<TicketRequest> f = KendoMongoBuilder<TicketRequest>.BuildFilter(p);
            try
            {
                List<Task<TaskRequest<List<TicketRequest>>>> tasks = new List<Task<TaskRequest<List<TicketRequest>>>> {
                    Task.Run(() => {
                        return TaskRequest<List<TicketRequest>>.Create("DB", DB.GetCollection<TicketRequest>()
                            .Find(f).Limit(p.Take).Skip(p.Skip)
                            .Sort(KendoMongoBuilder<TicketRequest>.BuildSort(p)).ToList());
                    })
                };
                Task<TaskRequest<List<TicketRequest>>[]> t = Task.WhenAll(tasks);
                try { t.Wait(); }
                catch (Exception) { throw; }
                List<TicketRequest> result = new List<TicketRequest>();
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    List<TicketRequest> TicketUpdateRequest = new List<TicketRequest>();
                    foreach (var r in t.Result)
                        if (r.Label == "DB")
                            if (!isFilterDate) TicketUpdateRequest = r.Result;
                            else TicketUpdateRequest = r.Result.FindAll(a => a.CreatedDate >= start && a.CreatedDate <= end).ToList();
                    foreach (var fur in TicketUpdateRequest)
                        if (categs.Contains(fur.TicketCategory))
                            result.Add(fur);

                }
                return ApiResult<List<TicketRequest>>.Ok(result, DB.GetCollection<TicketRequest>().CountDocuments(f));
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("mcomplaint")]
        public IActionResult Mcomplaint([FromForm] TicketForm p)
        {
            try
            {
                UpdateRequest ur = new UpdateRequest();
                TicketRequest ticket = JsonConvert.DeserializeObject<TicketRequest>(p.JsonData);
                ticket.TicketCategory = ticket.Category.Id;
                ticket.TicketNumber = DateTime.Now.ToLocalTime().ToString("yyyyMMTmmss");
                ticket.Upload(Configuration, null, p.FileUpload, x => String.Format("Ticket{0}{1}", ticket.CreatedDate.ToString("yyyyMMdHHmmss"), x.EmployeeID));
                if (ticket.TicketType == KESSWRServices.KESSTicketType.Complaint)
                {
                    string AXRID = new WorkFlowRequestAdapter(Configuration).RequestComplaint(ticket);
                    if (!string.IsNullOrWhiteSpace(AXRID))
                    {
                        ur.Create(AXRID, ticket.EmployeeID, UpdateRequestModule.COMPLAINT, "Complaint");
                        ticket.AXRequestID = AXRID;
                        DB.Save(ur);
                        new Notification(Configuration, DB).SendNotification(ticket.EmployeeID, ticket.AXRequestID);
                    }
                    else throw new Exception("Unable to request to AX");
                }
                else ticket.AXRequestID = Tools.RandomInt().ToString();
                DB.Save(ticket);
                return ApiResult<object>.Ok("Success");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("mresolution")]
        public IActionResult Mresolution([FromForm] TicketForm p)
        {
            try
            {
                TicketRequest t = JsonConvert.DeserializeObject<TicketRequest>(p.JsonData);
                TicketRequest ticket = _ticket.GetByID(t.Id);
                ticket.TicketStatus = t.TicketStatus;
                ticket.TicketResolution = t.TicketResolution;
                if (p.FileUpload != null)
                {
                    ticket.Attachments.Upload(Configuration, null, p.FileUpload, x => String.Format("TicketResolution{0}{1}", ticket.CreatedDate.ToString("yyyyMMdHHmmss"), x.Filename));
                }
                DB.Save(ticket);
                return ApiResult<object>.Ok("Success");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [Authorize]
        [HttpGet("mlist/tickettype")]
        public IActionResult MGetTravelType()
        {
            List<TicketTypeObject> tickettypes = new List<TicketTypeObject>();
            int i = 0;
            foreach (var item in (new List<string>(Enum.GetNames(typeof(KESSWRServices.KESSTicketType)))))
            {
                tickettypes.Add(new TicketTypeObject { Value = i++, Name = item });
            }
            return ApiResult<List<TicketTypeObject>>.Ok(tickettypes);
        }

        [Authorize]
        [HttpGet("mlist/ticketstatus")]
        public IActionResult MGetTravelStatus()
        {
            List<TicketStatusObject> ticketstatus = new List<TicketStatusObject>();
            int i = 0;
            foreach (var item in (new List<string>(Enum.GetNames(typeof(KESSWRServices.KESSTicketStatus)))))
            {
                ticketstatus.Add(new TicketStatusObject { Value = i++, Name = item });
            }
            ticketstatus.Add(new TicketStatusObject { Value = i, Name = "Reopen" });
            return ApiResult<List<TicketStatusObject>>.Ok(ticketstatus);
        }

        [Authorize]
        [HttpGet("mlist/ticketmedia")]
        public IActionResult MGetTravelMedia()
        {
            List<TicketMediaObject> ticketmedias = new List<TicketMediaObject>();
            int i = 0;
            foreach (var type in (new List<string>(Enum.GetNames(typeof(KESSWRServices.KESSTicketMedia)))))
            {
                ticketmedias.Add(new TicketMediaObject { Value = i++, Name = type });
            }
            return ApiResult<List<TicketMediaObject>>.Ok(ticketmedias);
        }

        [Authorize]
        [HttpGet("mticketcategory/getdata")]
        public IActionResult MGetData()
        {
            try { return ApiResult<List<TicketCategory>>.Ok(DB.GetCollection<TicketCategory>().Find(_ => true).ToList()); }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [Authorize]
        [HttpGet("getcomplaints/{employeeID}")]
        public IActionResult GetComplaints(string employeeID)
        {
            try { return ApiResult<List<TicketRequest>>.Ok(new ComplaintAdapter(Configuration).GetComplaintsByEmplID(employeeID)); }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpGet("mgetbyinstance/{employeeID}/{axRequestID}")]
        public IActionResult MGetByInstanceID(string employeeID, string axRequestID)
        {
            try { return ApiResult<TicketRequest>.Ok(_ticket.GetByInstanceID(employeeID, axRequestID)); }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpGet("download/{employeeID}")]
        public IActionResult MDownload(string employeeID)
        {
            try
            {
                TicketRequest t = _ticket.GetByID(employeeID);
                return File(t.Download(), "application/force-download", Path.GetFileName(t.Filepath));
            }
            catch (Exception e) { throw e; }
        }

        [HttpGet("rdownload/{employeeID}")]
        public IActionResult MRDownload(string employeeID)
        {
            try
            {
                TicketRequest t = _ticket.GetByID(employeeID);
                return File(t.Attachments.Download(), "application/force-download", Path.GetFileName(t.Attachments.Filepath));
            }
            catch (Exception e) { throw e; }
        }

    }
}
