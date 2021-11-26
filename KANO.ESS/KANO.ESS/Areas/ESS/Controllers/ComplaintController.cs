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

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class ComplaintController : Controller
    {
        private IConfiguration Configuration;
        private readonly IUserSession Session;

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

        public new async Task<IActionResult> Request([FromForm]TicketForm param)
        {
            TicketRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<TicketRequest>(param.JsonData);
                data.EmployeeID = Session.Id();
                data.EmployeeName = Session.DisplayName();
            }
            catch (Exception)
            {
                throw new ArgumentException("Parameter cannot be null", "jsonData");
            }
            var req = new Request("api/complaint/request", Method.POST);
            
            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(data));
            if (param.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            var res = await (new Client(Configuration)).Upload(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                var response = SendUseTemplate(data, Session.Id());
                if (!string.IsNullOrWhiteSpace(response)) {
                    result.StatusCode = HttpStatusCode.BadRequest;
                    result.Message = response;
                }
            }
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> RequestUpdateStatus([FromForm] TicketForm param)
        {
            TicketRequest data;
            try
            {
                data = JsonConvert.DeserializeObject<TicketRequest>(param.JsonData);
                data.EmployeeID = Session.Id();
            }
            catch (Exception)
            {
                throw new ArgumentException("Parameter cannot be null", "jsonData");
            }
            var req = new Request("api/complaint/updateStatus", Method.POST);
            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(data));
            var res = await (new Client(Configuration)).Upload(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                var response = SendUseTemplate(data, Session.Id());
            }
            return new ApiResult<object>(result);
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

    }
}