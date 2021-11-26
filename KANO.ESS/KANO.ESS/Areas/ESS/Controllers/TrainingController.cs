using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
//using KANO.ESS.Model;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using KANO.Core.Service.AX;
using System.Net;
using KANO.Core.Lib;
using System.Security.Claims;
using RestSharp;
using KANO.Core.Lib.Extension;
using Newtonsoft.Json;
using KANO.Core.Service;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class TrainingController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;

        public TrainingController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Training"}
            };
            ViewBag.Title = "Training";
            ViewBag.Icon = "mdi mdi-run";
            return View();
        }

        public IActionResult Get([FromBody] GridDateRange param)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/training/get", Method.POST);

            param.Username = employeeID;
            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var employee = JsonConvert.DeserializeObject<ApiResult<List<Training>>.Result>(response.Content);
            return new ApiResult<List<Training>>(employee);
        }

        public IActionResult GetHistory([FromBody] GridDateRange param)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/training/history", Method.POST);

            param.Username = employeeID;
            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var employee = JsonConvert.DeserializeObject<ApiResult<List<Training>>.Result>(response.Content);
            return new ApiResult<List<Training>>(employee);
        }

        public IActionResult GetTypes()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/training/list/type", Method.GET);

            var response = client.Execute(request);
            var employee = JsonConvert.DeserializeObject<ApiResult<List<TrainingType>>.Result>(response.Content);
            return new ApiResult<List<TrainingType>>(employee);
        }

        public IActionResult GetSubTypes()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/training/list/subType", Method.GET);

            var response = client.Execute(request);
            var employee = JsonConvert.DeserializeObject<ApiResult<List<TrainingSubType>>.Result>(response.Content);
            return new ApiResult<List<TrainingSubType>>(employee);
        }

        public IActionResult Register([FromBody] TrainingRegistration param)
        {
            param.EmployeeID = Session.Id();
            param.EmployeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/training/register", Method.POST);

            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var employee = JsonConvert.DeserializeObject<ApiResult<TrainingRegistration>.Result>(response.Content);
            return new ApiResult<TrainingRegistration>(employee);
        }

        public IActionResult GetReferences()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/training/references/{employeeID}", Method.GET);
            var response = client.Execute(request);
            var employee = JsonConvert.DeserializeObject<ApiResult<List<TrainingReference>>.Result>(response.Content);
            return new ApiResult<List<TrainingReference>>(employee);
        }

    }
}