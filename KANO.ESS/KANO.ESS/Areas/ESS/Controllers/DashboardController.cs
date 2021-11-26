using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using KANO.Core.Model;
using KANO.Core.Service;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class DashboardController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;

        public DashboardController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Dashboard"}
            };
            ViewBag.Title = "Dashboard";
            ViewBag.Icon = "mdi mdi-home";

            var EmployeeName = Session.DisplayName();
            ViewBag.EmployeeName = EmployeeName;
            return View();
        }

        public IActionResult CallCenter()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Dashboard", URL=""},
                new Breadcrumb{Title="Call Center", URL=""}
            };
            ViewBag.Title = "Call Center";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        public IActionResult GetTest([FromBody] ParamTest param)
        {
            return ApiResult<ParamTest>.Ok(param);
        }

        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }

    }

    public class ParamTest
    {
        public string param1 { get; set; }
        public int param2 { get; set; }
    }
}