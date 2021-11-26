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