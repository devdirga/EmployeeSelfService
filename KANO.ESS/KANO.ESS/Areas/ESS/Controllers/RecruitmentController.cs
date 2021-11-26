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
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class RecruitmentController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public RecruitmentController(IConfiguration conf,IUserSession session)
        {
            Configuration = conf;
            Session = session;
        }
        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Recruitment", URL=""},
                new Breadcrumb{Title="Request", URL=""}
            };
            ViewBag.Title = "Recruitment";
            ViewBag.Icon = "mdi mdi-library-books"; 
            return View("~/Areas/ESS/Views/Recruitment/Index.cshtml");
        }

        public IActionResult Application()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Recruitment", URL=""},
                new Breadcrumb{Title="Application", URL=""}
            };
            ViewBag.Title = "Application";
            ViewBag.Icon = "mdi mdi-library-books";
            return View("~/Areas/ESS/Views/Recruitment/JobOrPositionApplication.cshtml");
        }
        
        public async Task<IActionResult> GetRequest([FromBody] GridDateRange param)
        {            
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/recruitment/request/list", Method.POST);            
            request.AddJsonParameter(param);

            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<List<Recruitment>>.Result>(response.Content);
            return new ApiResult<List<Recruitment>>(result);
        }

        public async Task<IActionResult> GetOpenings([FromBody] GridDateRange param)
        {            
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/recruitment/openings", Method.POST);            
            request.AddJsonParameter(param);

            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<List<Recruitment>>.Result>(response.Content);
            return new ApiResult<List<Recruitment>>(result);
        }

        public async Task<IActionResult> GetHistory([FromBody] GridDateRange param)
        {            
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/recruitment/history", Method.POST);            
            request.AddJsonParameter(param);

            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<List<Recruitment>>.Result>(response.Content);
            return new ApiResult<List<Recruitment>>(result);
        }

        public async Task<IActionResult> GetDetail(string token)
        {            
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/recruitment/detail/{employeeID}/{token}", Method.GET);            

            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<Application>.Result>(response.Content);
            return new ApiResult<Application>(result);
        }

        public async Task<IActionResult> Get([FromBody] GridDateRange param)
        {            
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/recruitment/list", Method.POST);            
            request.AddJsonParameter(param);

            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<List<Recruitment>>.Result>(response.Content);
            return new ApiResult<List<Recruitment>>(result);
        }

        public async Task<IActionResult> GetByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/recruitment/getbyinstance/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<Recruitment>.Result>(resp.Content);
            return new ApiResult<Recruitment>(result);
        }
        
        public async Task<IActionResult> GetJobs()
        {            
            var client = new Client(Configuration);
            var request = new Request($"api/recruitment/job/list", Method.GET);            

            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<List<Job>>.Result>(response.Content);
            return new ApiResult<List<Job>>(result);
        }

        public async Task<IActionResult> GetPositions()
        {                        
            var client = new Client(Configuration);
            var request = new Request($"api/recruitment/position/list", Method.GET);            

            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<List<Position>>.Result>(response.Content);
            return new ApiResult<List<Position>>(result);
        }        

        public async Task<IActionResult> GetApplications([FromBody]GridDateRange param)
        {            
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/recruitment/applications", Method.POST);            
            request.AddJsonParameter(param);

            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<List<Recruitment>>.Result>(response.Content);
            return new ApiResult<List<Recruitment>>(result);
        }        

        public async Task<IActionResult> Request([FromForm] RecruitmentForm param)
        {            
            var recruitment = JsonConvert.DeserializeObject<Recruitment>(param.JsonData);
            recruitment.EmployeeName = Session.DisplayName();
            recruitment.EmployeeID = Session.Id();
            recruitment.RecruiterName = Session.DisplayName();
            recruitment.RecruiterID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request("api/recruitment/request", Method.POST);

            recruitment.EmployeeID = Session.Id();
            recruitment.EmployeeName = Session.DisplayName();
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(recruitment));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Apply([FromForm]ApplicationForm param)
        {            
            var application = JsonConvert.DeserializeObject<Application>(param.JsonData);
            var client = new Client(Configuration);
            var request = new Request("api/recruitment/apply", Method.POST);

            application.EmployeeID = Session.Id();
            application.EmployeeName = Session.DisplayName();
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(application));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
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
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/recruitment/download/{source}/{id}")))
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