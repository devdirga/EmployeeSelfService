using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Auth;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class RetirementController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;

        public RetirementController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Retirement"}
            };
            ViewBag.Title = "Retirement";
            ViewBag.Icon = "mdi mdi-calendar-check";
            ViewBag.HeaderTitle = "Retirement";
            return View();
        }

        public async Task<IActionResult> Get()
        {            
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/retirement/{employeeID}", Method.GET);
            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<Retirement>.Result>(response.Content);
            return new ApiResult<Retirement>(result);
        }

        public async Task<IActionResult> Request([FromForm] RetirementForm param)
        {                        
            var retirement = JsonConvert.DeserializeObject<Retirement>(param.JsonData);
            var client = new Client(Configuration);
            var request = new Request("api/employee/retirement/request", Method.POST);

            retirement.EmployeeID = Session.Id();
            retirement.EmployeeName = Session.DisplayName();

            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(retirement));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);        
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> GetByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/employee/retirement/getbyinstance/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<Retirement>.Result>(resp.Content);
            return new ApiResult<Retirement>(result);
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
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/retirement/download/{source}/{id}")))
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