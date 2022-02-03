using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using KANO.Core.Service;
using KANO.Core.Lib.Extension;


using KANO.Core.Lib;
using RestSharp;
using Newtonsoft.Json;
using System.Net.Mail;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using KANO.Core.Lib.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class ComplaintController : Controller
    {
        private IConfiguration Configuration;
        private readonly IUserSession Session;
        private readonly String Api = "api/complaint/";
        private readonly String BearerAuth = "Bearer ";

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public ComplaintController(IConfiguration conf, IUserSession session)
        {
            Configuration = conf;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS ", URL=""},
                new Breadcrumb{Title="Complain & Request", URL=""}
            };
            ViewBag.Title = "Complain & Request";
            ViewBag.Icon = "mdi mdi-zip-box";
            return View();
        }

        public IActionResult Resolution()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS ", URL=""},
                new Breadcrumb{Title="Complain & Request Approval", URL=""}
            };
            ViewBag.Title = "Complain & Request Approval";
            ViewBag.Icon = "mdi mdi-zip-box";
            return View();
        }

        public IActionResult TicketCategory()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS ", URL=""},
                new Breadcrumb{Title="Ticket Categories", URL=""}
            };
            ViewBag.Title = "Ticket Categories";
            ViewBag.Icon = "mdi mdi-zip-box";
            return View();
        }

        public IActionResult GetTicketType()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/complaint/list/ticketType", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(response.Content);
            return new ApiResult<List<string>>(result);
        }

        public IActionResult GetTicketStatus()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/complaint/list/ticketStatus", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(response.Content);
            return new ApiResult<List<string>>(result);
        }

        public IActionResult GetTicketMedia()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/complaint/list/ticketMedia", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(response.Content);
            return new ApiResult<List<string>>(result);
        }

        public IActionResult Get([FromBody] KendoGrid param)
        {
            var employeeID = Session.Id();
            if(param.Filter == null){
                param.Filter = new KendoFilters();  
                param.Filter.Logic = "and";
                param.Filter.Filters = new List<KendoFilter>();
            }

            param.Filter.Filters.Add(new KendoFilter{
                Field = "EmployeeID",
                Operator="eq",
                Value=employeeID,
            });

            var cl = new Client(Configuration);            
                var req = new Request($"api/complaint/get", Method.POST);
            req.AddJsonParameter(param);
            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<List<TicketRequest>>.Result>(resp.Content);
            return new ApiResult<List<TicketRequest>>(result);
        }

        public IActionResult GetResolution([FromBody] KendoGrid param)
        {
            var cl = new Client(Configuration);
            var req = new Request($"api/complaint/get", Method.POST);
            req.AddJsonParameter(param);
            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<List<TicketRequest>>.Result>(resp.Content);
            return new ApiResult<List<TicketRequest>>(result);
        }

        [HttpPost]
        public async Task<IActionResult> Complaint([FromForm] TicketForm p)
        {
            try
            {
                TicketRequest t = JsonConvert.DeserializeObject<TicketRequest>(p.JsonData);
                t.EmployeeID = Session.Id();
                t.EmployeeName = Session.DisplayName();
                var req = new Request($"{Api}complaint", Method.POST);
                req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(t));
                if (p.FileUpload != null)
                {
                    req.AddFormDataFile("FileUpload", p.FileUpload.FirstOrDefault());
                }
                var res = JsonConvert.DeserializeObject<ApiResult<object>.Result>((await (new Client(Configuration)).Upload(req)).Content);
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    var response = SendUseTemplate(t, Session.Id());
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        res.StatusCode = HttpStatusCode.BadRequest;
                        res.Message = response;
                    }
                }
                return new ApiResult<object>(res);
            }
            catch (Exception) { throw; }
        }

        [HttpPost]
        public async Task<IActionResult> Resolution([FromForm] TicketForm p)
        {
            try
            {
                TicketRequest t = JsonConvert.DeserializeObject<TicketRequest>(p.JsonData);
                t.EmployeeID = Session.Id();
                var req = new Request($"{Api}resolution", Method.POST);
                req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(t));
                if (p.FileUpload != null)
                {
                    req.AddFormDataFile("FileUpload", p.FileUpload.FirstOrDefault());
                }
                var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>((await (new Client(Configuration)).Upload(req)).Content);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    SendUseTemplate(t, Session.Id());
                }
                return new ApiResult<object>(result);
            }
            catch (Exception) { throw; }
        }

        [HttpGet]
        public IActionResult GetTicketCategories()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/complaint/ticketcategory/getdata", Method.GET);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<TicketCategory>>.Result>(response.Content);
            return new ApiResult<List<TicketCategory>>(result);
        }

        [HttpPost]
        public IActionResult SaveTicketCategory([FromBody] TicketCategory param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/complaint/ticketcategory/save", Method.POST);
            param.EmployeeID = Session.Id();
            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<TicketCategory>.Result>(response.Content);
            return new ApiResult<TicketCategory>(result);
        }

        public async Task<IActionResult> GetByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/complaint/getbyinstance/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<TicketRequest>.Result>(resp.Content);
            return new ApiResult<TicketRequest>(result);
        }

        public async Task<IActionResult> Download(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/complaint/download/{source}/{id}")))
                {
                    return File(stream.ToArray(), "application/force-download", x);
                }
            }
            catch (Exception e)
            {
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e);
                return View("Error");
            }
        }

        private string SendUseTemplate(TicketRequest param, string empId)
        {
            try
            {
                var mailTemplate = GetTemplate();
                string bodyTemplate = mailTemplate.Body;

                bodyTemplate = bodyTemplate.Replace("#NIPP#", param.EmployeeID);
                bodyTemplate = bodyTemplate.Replace("#NAMA#", param.EmployeeName);
                bodyTemplate = bodyTemplate.Replace("#KETERANGAN#", param.Description);

                var mailer = new Mailer(Configuration);               

                var message = new MailMessage();
                foreach (var m in param.EmailTo)
                {
                    message.To.Add(m);
                }

                if (!string.IsNullOrWhiteSpace(param.EmailCC))
                {
                    List<string> mailCC = JsonConvert.DeserializeObject<List<string>>(param.EmailCC);
                    foreach (var m in mailCC)
                    {
                        message.CC.Add(m);
                    }
                }
                message.Subject = mailTemplate.Subject;

                message.Body = string.Format(bodyTemplate, param.Id);
                mailer.SendMail(message);
            }
            catch (Exception e)
            {
                return $"Error send email :\n{e.Message}";
            }
            return $"";
        }
        
        private ComplaintMailTemplate GetTemplate()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/complaint/gettemplate", Method.GET);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<ComplaintMailTemplate>.Result>(response.Content);
            return result.Data;

        }

        [AllowAnonymous]
        public IActionResult GetComplaints(string token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<TicketRequest>>(
                JsonConvert.DeserializeObject<ApiResult<List<TicketRequest>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}getcomplaints/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetTicketType()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<TicketTypeObject>>(
                JsonConvert.DeserializeObject<ApiResult<List<TicketTypeObject>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mlist/ticketType", Method.GET, "Authorization", bearerAuth)).Content));
        }

        [AllowAnonymous]
        public IActionResult MGetTicketStatus()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<TicketStatusObject>>(
                JsonConvert.DeserializeObject<ApiResult<List<TicketStatusObject>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mlist/ticketStatus", Method.GET, "Authorization", bearerAuth)).Content));
        }

        [AllowAnonymous]
        public IActionResult MGetTicketMedia()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<TicketMediaObject>>(
                JsonConvert.DeserializeObject<ApiResult<List<TicketMediaObject>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mlist/ticketMedia", Method.GET, "Authorization", bearerAuth)).Content));
        }

        [AllowAnonymous]
        public IActionResult MGet([FromBody] KendoGrid p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            if (p.Filter == null) { p.Filter = new KendoFilters { Logic = "and", Filters = new List<KendoFilter>() }; }
            p.Filter.Filters.Add(new KendoFilter { Field = "EmployeeID", Operator = "eq", Value = p.EmployeeID });
            return new ApiResult<List<TicketRequest>>(
                JsonConvert.DeserializeObject<ApiResult<List<TicketRequest>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mget", Method.POST, p, "Authorization", bearerAuth)).Content));
        }

        [AllowAnonymous]
        public IActionResult MGetResolution([FromBody] KendoGrid p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            if (p.Filter == null) { p.Filter = new KendoFilters { Logic = "and", Filters = new List<KendoFilter>() }; }
            p.Filter.Filters.Add(new KendoFilter { Field = "Action", Operator = "eq", Value = "0" });
            return new ApiResult<List<TicketRequest>>(
                JsonConvert.DeserializeObject<ApiResult<List<TicketRequest>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mgetresolution", Method.POST, p, "Authorization", bearerAuth)).Content));
        }

        [AllowAnonymous]
        public async Task<IActionResult> MComplaint([FromForm] TicketForm p)
        {
            try
            {
                TicketRequest t = JsonConvert.DeserializeObject<TicketRequest>(p.JsonData);
                t.TicketMedia = KESSWRServices.KESSTicketMedia.WalkInCustomer;
                var req = new Request($"{Api}mcomplaint", Method.POST);
                req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(t));
                if (p.FileUpload != null)
                {
                    req.AddFormDataFile("FileUpload", p.FileUpload.FirstOrDefault());
                }
                var res = JsonConvert.DeserializeObject<ApiResult<object>.Result>((await (new Client(Configuration)).Upload(req)).Content);
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    var response = SendUseTemplate(t, t.EmployeeID);
                    if (!string.IsNullOrWhiteSpace(response))
                    {
                        res.StatusCode = HttpStatusCode.BadRequest;
                        res.Message = response;
                    }
                }
                return new ApiResult<object>(res);
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.InternalServerError, e.Message); }
        }

        [AllowAnonymous]
        public async Task<IActionResult> MResolution([FromForm] TicketForm p)
        {
            try
            {
                Console.WriteLine($"data : {p.JsonData}");
                TicketRequest t = JsonConvert.DeserializeObject<TicketRequest>(p.JsonData);
                Console.WriteLine($"datas : {t.EmployeeID}");
                var r = new Request($"{Api}mresolution", Method.POST);
                r.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(t));
                if (p.FileUpload != null)
                {
                    r.AddFormDataFile("FileUpload", p.FileUpload.FirstOrDefault());
                }
                var res = JsonConvert.DeserializeObject<ApiResult<object>.Result>((await (new Client(Configuration)).Upload(r)).Content);
                if (res.StatusCode == HttpStatusCode.OK)
                {
                    //var response = SendUseTemplate(ticket, Session.Id());
                }
                return new ApiResult<object>(res);
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR :", e.Message);
                throw new ArgumentException("Parameter cannot be null", "jsonData");
            }
        }

        [AllowAnonymous]
        public IActionResult MGetTicketCategories()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<TicketCategory>>(
                JsonConvert.DeserializeObject<ApiResult<List<TicketCategory>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mticketcategory/getdata", Method.GET, "Authorization", bearerAuth)).Content));
        }

        [AllowAnonymous]
        public IActionResult MGetByInstanceID(string source, string id)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<TicketRequest>(
                JsonConvert.DeserializeObject<ApiResult<TicketRequest>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mgetbyinstance/{source}/{id}", Method.GET, "Authorization", bearerAuth)).Content));
        }

        [AllowAnonymous]
        public IActionResult MDownload(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}{Api}download/{source}")))
                {
                    return File(stream.ToArray(), "application/force-download", id);
                }
            }
            catch (Exception e)
            {
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e);
                return View("Error");
            }
        }

        [AllowAnonymous]
        public IActionResult MRDownload(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}{Api}rdownload/{source}")))
                {
                    return File(stream.ToArray(), "application/force-download", id);
                }
            }
            catch (Exception e)
            {
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e);
                return View("Error");
            }
        }
    }
}