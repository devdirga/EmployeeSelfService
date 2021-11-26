using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Model.Payroll;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class ConfigLoanController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;
        public ConfigLoanController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Configuration", URL=""},
                new Breadcrumb{Title="Loan", URL=""}
            };
            ViewBag.Title = "Configuration";
            ViewBag.Icon = "mdi mdi-settings";
            return View();
        }

        public IActionResult GetConfig()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/configloan/get", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<ConfigurationLoan>>.Result>(response.Content);
            return new ApiResult<List<ConfigurationLoan>>(result);
        }

        [HttpPost]
        public IActionResult SaveConfig([FromBody] ConfigurationLoan configLoan)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/configloan/save", Method.POST);
            request.AddJsonParameter(configLoan);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult SaveConfigTemplate([FromBody] LoanMailTemplate configTemplate)
        {
            var empId = Session.Id();
            var empName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/configloan/savetemplate", Method.POST);
            configTemplate.UserIDUpdate = empId;
            configTemplate.UserNameUpdate = empName;
            request.AddJsonParameter(configTemplate);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult GetTemplate([FromBody] LoanMailTemplate configTemplate)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/configloan/gettemplate", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<LoanMailTemplate>.Result>(response.Content);
            return new ApiResult<LoanMailTemplate>(result);
        }
    }
}