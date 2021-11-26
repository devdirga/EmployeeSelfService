using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using MongoDB.Driver;
using System.Net;
using System.Security.Claims;
using Newtonsoft.Json;
using System.IO;
using KANO.Core.Lib.Helper;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class TimeManagementController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;

        public TimeManagementController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult MyTimeAttendance()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Time Management"},
                new Breadcrumb{Title="My Time Attendance"}
            };
            ViewBag.Title = "Time Management";
            ViewBag.Icon = "mdi mdi-calendar-clock";
            ViewBag.HeaderTitle = "Time Attendance";
            return View();
        }
        public IActionResult SubordinateAttendance()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Time Management"},
                new Breadcrumb{Title="Subordinate Attendance"}
            };
            ViewBag.Title = "Time Management";
            ViewBag.Icon = "mdi mdi-calendar-clock";
            ViewBag.HeaderTitle = "Subordinate Attendance";
            return View();
        }
        public IActionResult Agenda()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Time Management"},
                new Breadcrumb{Title="Agenda"}
            };
            ViewBag.Title = "Time Management";
            ViewBag.Icon = "mdi mdi-earth";
            ViewBag.HeaderTitle = "Agenda";
            return View();
        }
        public async Task<IActionResult> Get([FromBody] GridDateRange param)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/timeManagement/get/range", Method.POST);

            param.Username = employeeID;
            request.AddJsonParameter(param);

            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<List<TimeAttendanceResult>>.Result>(response.Content);
            return new ApiResult<List<TimeAttendanceResult>>(result);
        }

        public async Task<IActionResult> GetSubordinate([FromBody] GridDateRange param)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/timeManagement/subordinate/range", Method.POST);

            param.Username = employeeID;
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<List<TimeAttendance>>.Result>(response.Content);
            return new ApiResult<List<TimeAttendance>>(result);
        }

        public async Task<IActionResult> GetAbsenceCode()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/timeManagement/absencecode/get", Method.GET);
            var response = client.Execute(request);

            
            var result = JsonConvert.DeserializeObject<ApiResult<List<AbsenceCode>>.Result>(response.Content);
            return new ApiResult<List<AbsenceCode>>(result);
        }

        //public async Task<IActionResult> GetAbsenceCodeAll()
        //{
        //    var client = new Client(Configuration);
        //    var request = new Request($"api/timeManagement/absencecode/getall", Method.GET);
        //    var response = client.Execute(request);

            
        //    var result = JsonConvert.DeserializeObject<ApiResult<List<AbsenceCode>>.Result>(response.Content).Data;
        //    return ApiResult<List<AbsenceCode>>.Ok(result);
        //}
        

        public async Task<IActionResult> GetAbsenceImported()
        {   
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/timeManagement/AbsenceImported/{employeeID}", Method.GET);
            var response = client.Execute(request);
            
            var result = JsonConvert.DeserializeObject<ApiResult<List<AbsenceImported>>.Result>(response.Content);
            return new ApiResult<List<AbsenceImported>>(result);
        }

        public async Task<IActionResult> UpdateTimeAttendance([FromForm]TimeAttendanceForm param)
        {
            var timeAttendance = JsonConvert.DeserializeObject<TimeAttendance>(param.JsonData);

            var url = "api/timemanagement/timeattendance/update";
            if (timeAttendance.AXID <= 0)
            {
                url = "api/timemanagement/timeattendance/create";
            }

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request(url, Method.POST);

            timeAttendance.EmployeeID = employeeID;
            timeAttendance.EmployeeName = employeeName;

            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(timeAttendance));

            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            
            var result = JsonConvert.DeserializeObject<ApiResult<TimeAttendance>.Result>(response.Content);
            return new ApiResult<TimeAttendance>(result);
        }

        public IActionResult DiscardTimeAttendanceChange(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/timemanagement/timeattendance/remove/{token}", Method.GET);

            var response = client.Execute(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> GetByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/timemanagement/timeattendance/get/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);
            
            var result = JsonConvert.DeserializeObject<ApiResult<TimeAttendance>.Result>(resp.Content);
            return new ApiResult<TimeAttendance>(result);
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
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/timeManagement/download/{source}/{id}")))
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