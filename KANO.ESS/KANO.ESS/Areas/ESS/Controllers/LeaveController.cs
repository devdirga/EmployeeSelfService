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
using MongoDB.Driver;
using RestSharp;
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
    public class LeaveController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;
        private readonly String Api = "api/leave/";
        private readonly String BearerAuth = "Bearer ";

        public LeaveController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Leave"}
            };
            ViewBag.Title = "Leave";
            ViewBag.Icon = "mdi mdi-bag-personal";
            return View();
        }

        public IActionResult MyLeave()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Leave"},
                new Breadcrumb{Title="My Leave"}
            };
            return View();
        }

        public IActionResult SubordinateLeave()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Leave"},
                new Breadcrumb{Title="Subordinate Leave"}
            };
            return View();
        }

        public async Task<IActionResult> Get([FromBody] DateRange range)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/leave/{employeeID}", Method.POST);

            request.AddJsonParameter(range);

            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<List<Leave>>.Result>(response.Content);
            return new ApiResult<List<Leave>>(result);
        }

        public async Task<IActionResult> GetCalendar([FromBody] DateRange range)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/leave/calendar/{employeeID}", Method.POST);

            request.AddJsonParameter(range);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<LeaveCalendar>.Result>(response.Content);
            return new ApiResult<LeaveCalendar>(result);
        }

        public async Task<IActionResult> GetInfo()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/leave/info/{employeeID}", Method.GET);
            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<List<LeaveMaintenance>>.Result>(response.Content);
            return new ApiResult<List<LeaveMaintenance>>(result);
        }

        public async Task<IActionResult> GetInfoAll()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/leave/info/all/{employeeID}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<LeaveInfo>.Result>(response.Content);
            return new ApiResult<LeaveInfo>(result);
        }

        public async Task<IActionResult> Save([FromForm]LeaveForm param)
        {
            var leave = JsonConvert.DeserializeObject<Leave>(param.JsonData);

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/leave/save", Method.POST);
            
            leave.EmployeeID = employeeID;
            leave.EmployeeName = employeeName;
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(leave));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Remove(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/leave/delete/{token}", Method.GET);
            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> GetType()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/leave/type/{employeeID}", Method.GET);
            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<List<LeaveType>>.Result>(response.Content);
            return new ApiResult<List<LeaveType>>(result);
        }

        public async Task<IActionResult> GetSubordinate()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/leave/subordinate/{employeeID}", Method.GET);
            var response = client.Execute(request);            
            var result = JsonConvert.DeserializeObject<ApiResult<List<LeaveSubordinate>>.Result>(response.Content);
            return new ApiResult<List<LeaveSubordinate>>(result);
        }

        public async Task<IActionResult> GetHistory()
        {
            var employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/leave/history/{employeeID}", Method.GET);
            var resp = cl.Execute(req);
            
            var result = JsonConvert.DeserializeObject<ApiResult<List<LeaveHistory>>.Result>(resp.Content);
            return new ApiResult<List<LeaveHistory>>(result);
        }

        public async Task<IActionResult> GetHoliday([FromBody]DateRange param)
        {                        
            var newParam = new HolidayParam
            {
                Range = param,
                EmployeeID = Session.Id()
            };            
            return await this.GetHolidayByEmployeeID(newParam);
        }

        public async Task<IActionResult> GetHolidayByEmployeeID([FromBody]HolidayParam param)
        {
            var cl = new Client(Configuration);
            var req = new Request($"api/leave/holiday/range", Method.POST);            
            if (string.IsNullOrWhiteSpace(param.EmployeeID)) param.EmployeeID = Session.Id();

            req.AddJsonParameter(param);
            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<List<HolidaySchedule>>.Result>(resp.Content);
            return new ApiResult<List<HolidaySchedule>>(result);
        }

        public async Task<IActionResult> GetSubtitutions()
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/leave/subtitution/{employeeID}", Method.GET);

            var resp = cl.Execute(req);
            
            var result = JsonConvert.DeserializeObject<ApiResult<List<Employee>>.Result>(resp.Content);
            return new ApiResult<List<Employee>>(result);
        }

        public async Task<IActionResult> GetByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/leave/get/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);
            
            var result = JsonConvert.DeserializeObject<ApiResult<Leave>.Result>(resp.Content);
            return new ApiResult<Leave>(result);
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
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/leave/download/{source}/{id}")))
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
            return new ApiResult<List<Leave>>(
                JsonConvert.DeserializeObject<ApiResult<List<Leave>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}m/{p.Username}", Method.POST, p.Range, "Authorization", bearerAuth)).Content));
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult MGetCalendar([FromBody] GridDateRange p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<LeaveCalendar>(
                JsonConvert.DeserializeObject<ApiResult<LeaveCalendar>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mcalendar/{p.Username}", Method.POST, p.Range, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetInfo(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<LeaveMaintenance>>(
                JsonConvert.DeserializeObject<ApiResult<List<LeaveMaintenance>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}minfo/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetInfoAll(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<LeaveInfo>(
                JsonConvert.DeserializeObject<ApiResult<LeaveInfo>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}minfo/all/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> MCreate([FromForm] LeaveForm p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            var leave = JsonConvert.DeserializeObject<Leave>(p.JsonData);
            var req = new Request($"{Api}msave", Method.POST, "Authorization", bearerAuth);
            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(leave));
            if (p.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", p.FileUpload.FirstOrDefault());
            }
            return new ApiResult<object>(
                JsonConvert.DeserializeObject<ApiResult<object>.Result>(
                    (await new Client(Configuration).Upload(req)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MRemove(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<object>(
                JsonConvert.DeserializeObject<ApiResult<object>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mdelete/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetType(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<LeaveType>>(
                JsonConvert.DeserializeObject<ApiResult<List<LeaveType>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mtype/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetSubordinate(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<LeaveSubordinate>>(
                JsonConvert.DeserializeObject<ApiResult<List<LeaveSubordinate>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}msubordinate/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetHistory(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<LeaveHistory>>(
                JsonConvert.DeserializeObject<ApiResult<List<LeaveHistory>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mhistory/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult MGetHoliday([FromBody] GridDateRange p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            HolidayParam param = new HolidayParam { Range = p.Range, EmployeeID = p.Username };
            return new ApiResult<List<HolidaySchedule>>(
                JsonConvert.DeserializeObject<ApiResult<List<HolidaySchedule>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mholiday/range", Method.POST, param, "Authorization", bearerAuth)).Content));
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult MGetHolidayByEmployeeID([FromBody] HolidayParam p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<HolidaySchedule>>(
                JsonConvert.DeserializeObject<ApiResult<List<HolidaySchedule>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mholiday/range", Method.POST, p, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetSubtitutions(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<Employee>>(
                JsonConvert.DeserializeObject<ApiResult<List<Employee>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}msubtitution/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetByInstanceID(string source, string id)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<Leave>(
                JsonConvert.DeserializeObject<ApiResult<Leave>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mget/{source}/{id}", Method.GET, "Authorization", bearerAuth)).Content));
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
                return ApiResult<object>.Error(HttpStatusCode.InternalServerError, $"Well it is embarassing, internal server error : {e.Message}");
            }
        }
    }
}