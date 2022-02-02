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
    public class AdministratorController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;
        private readonly String Api = "api/administration/";

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public AdministratorController(IConfiguration conf)
        {
            Configuration = conf;
        }

        public IActionResult Index()
        {
            return View();
        }
        public IActionResult UserGroup()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Administrator", URL=""},
                new Breadcrumb{Title="User Group ", URL=""}
            };
            ViewBag.Title = "Administrator";
            ViewBag.Icon = "mdi mdi-settings";
            return View();
        }
        public IActionResult UserManagement()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Administrator", URL=""},
                new Breadcrumb{Title="User Management ", URL=""}
            };
            ViewBag.Title = "Administrator";
            ViewBag.Icon = "mdi mdi-settings";
            return View();
        }

        public IActionResult ConfigPassword()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Administrator", URL=""},
                new Breadcrumb{Title="Config Password ", URL=""}
            };
            ViewBag.Title = "Administrator";
            ViewBag.Icon = "mdi mdi-settings";
            return View("~/Areas/ESS/Views/Administration/ConfigPassword.cshtml");
        }

        public IActionResult MobileVersion()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Administrator", URL=""},
                new Breadcrumb{Title="Mobile Version ", URL=""}
            };
            ViewBag.Title = "Administrator";
            ViewBag.Icon = "mdi mdi-settings";
            return View("~/Areas/ESS/Views/Administration/MobileVersion.cshtml");
        }

        public IActionResult DocumentRequest()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Administrator", URL=""},
                new Breadcrumb{Title="Document Request ", URL=""}
            };
            ViewBag.Title = "Document Request";
            ViewBag.Icon = "mdi mdi-settings";
            return View("~/Areas/ESS/Views/Administration/DocumentRequest.cshtml");
        }

        [HttpGet]
        [Route("GetUserGroups")]
        public async Task<IActionResult> GetUserGroups()
        {
            var data = new List<UserGroup>()
            {
                new UserGroup{Group = "Administrator", TotalUser = 1 },
                new UserGroup{Group = "HR Admin", TotalUser = 1 },
                new UserGroup{Group = "Employee", TotalUser = 105 },
            };
            //return data;
            return ApiResult<List<UserGroup>>.Ok(data);
        }

        [HttpGet]
        [Route("GetMenus")]
        public async Task<IActionResult> GetMenus()
        {
            var data = new List<Menus>()
            {
                new Menus{Module = "Employment", Menu = "Profile", Privileages = "Edit" },
                new Menus{Module = "Payroll", Menu = "Slip", Privileages = "Edit" },
                new Menus{Module = "Time Management", Menu = "Attendance", Privileages = "Edit" },
                new Menus{Module = "Leave", Menu = "Leave List", Privileages = "Edit" },
                new Menus{Module = "Travel Expense", Menu = "Expense", Privileages = "Edit" },
                new Menus{Module = "Insurenace", Menu = "Insurance", Privileages = "Edit" },
                new Menus{Module = "Performance Appraisal", Menu = "Appraisal", Privileages = "Edit" },
                new Menus{Module = "Training", Menu = "Traninng", Privileages = "Edit" },
                new Menus{Module = "Documentation", Menu = "Documentation", Privileages = "Edit" },
                new Menus{Module = "Administrator", Menu = "User Role", Privileages = "Edit" },
                new Menus{Module = "Administrator", Menu = "User Management", Privileages = "Edit" },
            };
            return ApiResult<List<Menus>>.Ok(data);
        }

        [HttpGet]
        [Route("GetUserManagements")]
        public async Task<IActionResult> GetUserManagements()
        {
            var data = new List<GetUserManagements>()
            {
                new GetUserManagements{EmployeeName = "agus@tps.co.id", UserLogin = "Agus Setiawan", UserGroup = "Employee", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "aris@tps.co.id", UserLogin = "Aris Saputro", UserGroup = "Employee", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "ditok@tps.co.id", UserLogin = "Ditok Andri", UserGroup = "Administrator", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "123456", UserLogin = "Ferry Iswono", UserGroup = "HR Admin", Authentication = "ESS Portal" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
            };
            return ApiResult<List<GetUserManagements>>.Ok(data);

            // var client = new Client(Configuration);
            // var request = new Request($"api/administrator/getdata", Method.GET);
            // var response = client.Execute(request);

            // var result = JsonConvert.DeserializeObject<ApiResult<List<UserModel>>.Result>(response.Content);
            // return new ApiResult<List<UserModel>>(result);
            
        }

        [AllowAnonymous]
        public IActionResult GetConfigPassword()
        {
            return new ApiResult<Core.Model.Auth.ConfigPassword>(
                JsonConvert.DeserializeObject<ApiResult<Core.Model.Auth.ConfigPassword>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}getconfigpassword", Method.GET)).Content));
        }
        public async Task<IActionResult> UpdateConfigPassword([FromForm] FamilyForm param)
        {
            var req = new Request($"{Api}updateconfigpassword", Method.POST);
            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Core.Model.Auth.ConfigPassword>(param.JsonData)));
            if (param.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            return new ApiResult<object>(JsonConvert.DeserializeObject<ApiResult<object>.Result>((await new Client(Configuration).Upload(req)).Content));
        }
        [AllowAnonymous]
        public IActionResult GetMobileVersion()
        {
            return new ApiResult<List<Mobile>>(
                JsonConvert.DeserializeObject<ApiResult<List<Mobile>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}getmobileversion", Method.GET)).Content));
        }
        public async Task<IActionResult> UpdateAndroidVersion([FromForm] FamilyForm param)
        {
            var req = new Request($"{Api}updateandroidversion", Method.POST);
            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Mobile>(param.JsonData)));
            if (param.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            return new ApiResult<object>(JsonConvert.DeserializeObject<ApiResult<object>.Result>(
                (await new Client(Configuration).Upload(req)).Content));
        }
        public async Task<IActionResult> UpdateIOSVersion([FromForm] FamilyForm param)
        {
            var req = new Request($"{Api}updateiosversion", Method.POST);
            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(JsonConvert.DeserializeObject<Mobile>(param.JsonData)));
            if (param.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            return new ApiResult<object>(JsonConvert.DeserializeObject<ApiResult<object>.Result>(
                (await new Client(Configuration).Upload(req)).Content));
        }
        [AllowAnonymous]
        public IActionResult Download(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/administration/download/{source}")))
                {
                    return File(stream.ToArray(), "application/force-download", id);
                }
            }
            catch (Exception)
            {
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                return View("Error");
            }
        }
    }
    //DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)
    internal class GetUserManagements
    {
        public int Id { get; set; }
        public string EmployeeName { get; set; }
        public string UserLogin { get; set; }
        public string UserGroup { get; set; }
        public string Authentication { get; set; }
        public DateTime LatestConnection { get; set; }

    }

    internal class Menus
    {
        public int Id { get; set; }
        public string Module { get; set; }
        public string Menu { get; set; }
        public string Privileages { get; set; }
    }

    internal class UserGroup
    {
        public int Id { get; set; }
        public string Group { get; set; }
        public int TotalUser { get; set; }
    }
}