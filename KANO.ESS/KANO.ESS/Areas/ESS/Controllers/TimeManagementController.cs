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
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class TimeManagementController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;
        private readonly String Api = "api/timemanagement/";
        private readonly String BearerAuth = "Bearer ";

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

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [HttpPost]
        [AllowAnonymous]
        public IActionResult MGet([FromBody] GridDateRange p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<TimeAttendanceResult>>(
                JsonConvert.DeserializeObject<ApiResult<List<TimeAttendanceResult>>.Result>(
                    (new Client(Configuration).Execute(new Request($"{Api}mget/range", Method.POST, p, "Authorization", bearerAuth))).Content));
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult MGetSubordinate([FromBody] GridDateRange p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<TimeAttendance>>(
                JsonConvert.DeserializeObject<ApiResult<List<TimeAttendance>>.Result>(
                    (new Client(Configuration).Execute(new Request($"{Api}msubordinate/range", Method.POST, p, "Authorization", bearerAuth))).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetAbsenceCode()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<AbsenceCode>>(
                JsonConvert.DeserializeObject<ApiResult<List<AbsenceCode>>.Result>(
                    (new Client(Configuration).Execute(new Request($"{Api}mabsencecode/get", Method.GET, "Authorization", bearerAuth))).Content));
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult MGetAbsenceImported([FromBody] GridDateRange p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<AbsenceImported>>(
                JsonConvert.DeserializeObject<ApiResult<List<AbsenceImported>>.Result>(
                    (new Client(Configuration).Execute(new Request($"{Api}mabsenceimported/{p.Username}", Method.GET, "Authorization", bearerAuth))).Content));
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> MUpdateTimeAttendance([FromForm] TimeAttendanceForm p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            var att = JsonConvert.DeserializeObject<TimeAttendance>(p.JsonData);
            var url = (att.AXID <= 0) ? $"{Api}mtimeattendance/create" : $"{Api}mtimeattendance/update";
            var req = new Request(url, Method.POST, "Authorization", bearerAuth);
            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(att));
            if (p.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", p.FileUpload.FirstOrDefault());
            }
            var res = await new Client(Configuration).Upload(req);
            return new ApiResult<TimeAttendance>(JsonConvert.DeserializeObject<ApiResult<TimeAttendance>.Result>(res.Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MDiscardTimeAttendanceChange(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<object>(
                JsonConvert.DeserializeObject<ApiResult<object>.Result>(
                    (new Client(Configuration).Execute(new Request($"{Api}mtimeattendance/remove/{token}", Method.GET, "Authorization", bearerAuth))).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetByInstanceID(string source, string id)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<TimeAttendance>(
                JsonConvert.DeserializeObject<ApiResult<TimeAttendance>.Result>(
                    (new Client(Configuration).Execute(new Request($"{Api}mtimeattendance/get/{source}/{id}", Method.GET, "Authorization", bearerAuth))).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MDownload(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");
                }
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}{Api}mdownload/{source}/{id}")))
                {
                    return File(stream.ToArray(), "application/force-download", x);
                }
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
HttpStatusCode.InternalServerError, $"Well it is embarassing, internal server error : {e.Message}");
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult MAgendaGet([FromBody] GridDateRange param)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<Agenda>>(JsonConvert.DeserializeObject<ApiResult<List<Agenda>>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}magendaget/range", Method.POST, param, "Authorization", bearerAuth)).Content));
        }
        [AllowAnonymous]
        public IActionResult MAgendaDownload([FromBody] TimeAttendanceForm param)
        {
            var token = param.JsonData;
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl)) { return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration"); }
                var decodedToken = WebUtility.UrlDecode(token);
                var filepath = Hasher.Decrypt(decodedToken);
                var filename = Path.GetFileName(filepath);

                var tokenreplacesplash = token.Replace(@"/", "_");
                // Fetching file info
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}{Api}magendadownload/{tokenreplacesplash}")))
                {
                    return File(stream.ToArray(), "application/force-download", filename);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} <<< {Format.ExceptionString(e)}");
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e);
                return View("Error");
            }
        }

    }
}