using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using KANO.Core.Service;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using RestSharp;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using KANO.Core.Lib.Helper;
using System.Net;
using System.IO;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class TravelController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IUserSession Session;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public TravelController(IConfiguration conf, IUserSession session)
        {
            Configuration = conf;
            Session = session;
        }                

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Travel", URL=""}
            };
            ViewBag.Title = "Travel";
            ViewBag.Icon = "mdi mdi-wallet-travel";
            return View();
        }

        public IActionResult Get([FromBody]GridDateRange param)
        {
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/travel/get", Method.POST);
            
            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var travel = JsonConvert.DeserializeObject<ApiResult<List<Travel>>.Result>(response.Content);
            return new ApiResult<List<Travel>>(travel);            
        }

        public IActionResult GetByStatus(string token)
        {
            var employeeID = Session.Id();
            var status = long.Parse(token);
            var client = new Client(Configuration);
            var request = new Request($"api/travel/get/status/{employeeID}/{status}", Method.POST);

            var response = client.Execute(request);
            var travel = JsonConvert.DeserializeObject<ApiResult<List<Travel>>.Result>(response.Content);
            return new ApiResult<List<Travel>>(travel);
        }


        public async Task<IActionResult> Request([FromForm]TravelForm param)
        {
            var data = new Travel();
            try
            {
                data = JsonConvert.DeserializeObject<Travel>(param.JsonData);
            }
            catch(Exception e)
            {

            }
            //var data = JsonConvert.DeserializeObject<Travel>(param.JsonData);
            var req = new Request("api/travel/request", Method.POST);
            
            data.EmployeeID = Session.Id();
            data.EmployeeName = Session.DisplayName();
            data.TransactionDate = DateTime.Now;
            data.CreatedDate = DateTime.Now;
            data.CreatedBy = Session.DisplayName();
            
            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(data));
            if (param.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            var res = await (new Client(Configuration)).Upload(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Revise ([FromForm] TravelForm param)
        {
            Travel data;
            try
            {
                data = JsonConvert.DeserializeObject<Travel>(param.JsonData);
            }
            catch (Exception e)
            {
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e);
                return View("Error");
            }

            var req = new Request("api/travel/revise", Method.POST);

            data.EmployeeID = Session.Id();
            data.EmployeeName = Session.DisplayName();
            data.TransactionDate = DateTime.Now;
            data.CreatedDate = DateTime.Now;
            data.CreatedBy = Session.DisplayName();

            data.RevisionBy = Session.Id();
            data.RevisionDate = DateTime.Now;

            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(data));
            if (param.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            var res = await (new Client(Configuration)).Upload(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);
            return new ApiResult<object>(result);

        }
        
        public IActionResult TravelExpenseDiscard(String token)
        {
            var req = new Request($"api/TravelExpense/remove/{token}", Method.GET);
            var res = (new Client(Configuration)).Execute(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult Employee()
        {
            var req = new Request("api/TravelExpense/employee", Method.GET);
            var res = (new Client(Configuration)).Execute(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);
            return new ApiResult<object>(result);
        }        

        public IActionResult GetPurposes()
        {
            var req = new Request($"api/travel/list/purposes", Method.GET);

            var res = (new Client(Configuration)).Execute(req);
            var result = JsonConvert.DeserializeObject<ApiResult<List<TravelPurpose>>.Result>(res.Content);

            return new ApiResult<List<TravelPurpose>>(result);
        }

        public IActionResult GetTransportations()
        {
            var req = new Request($"api/travel/list/transportations", Method.GET);

            var res = (new Client(Configuration)).Execute(req);
            var result = JsonConvert.DeserializeObject<ApiResult<List<Transportation>>.Result>(res.Content);

            return new ApiResult<List<Transportation>>(result);
        }

        public async Task<IActionResult> DownloadAttacment(string source, string id, string x)
        {
            try
            {
                var employeeID = Session.Id();
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/travel/download/attachment/{employeeID}/{source}/{id}")))
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

        public async Task<IActionResult> Download(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/travel/download/{source}/{id}")))
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

        public async Task<IActionResult> GetByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/travel/getbyinstance/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<Travel>.Result>(resp.Content);
            return new ApiResult<Travel>(result);
        }

        public IActionResult GetTravelStatus()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/travel/list/travelStatus", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(response.Content);
            return new ApiResult<List<string>>(result);
        }

        public IActionResult Close(string token)
        {
            var req = new Request($"api/travel/close/{token}", Method.GET);
            var res = (new Client(Configuration)).Execute(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult GetTravelType()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/travel/list/travelType", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(response.Content);
            return new ApiResult<List<string>>(result);
        }

        public async Task<IActionResult> Downloads(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl)) { return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");  }                    

                // Fetching file info
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/travel/downloads/{source}/{id}")))
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

    }
}