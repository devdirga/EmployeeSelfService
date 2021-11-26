using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class TaskController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;

        public TaskController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Task"}
            };
            ViewBag.Title = "Task";
            ViewBag.Icon = "mdi mdi-check-box-multiple-outline";
            return View();
        }

         public async Task<IActionResult> Get()
        {
            var param = new FetchParam();
            param.Limit = (param.Limit <= 0) ? 10 : param.Limit;
            param.Offset = (param.Offset < 0) ? 0 : param.Offset;

            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/{employeeID}", Method.GET);
            var response = client.Execute(request);            
            if (response.StatusCode != HttpStatusCode.OK &&  string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<List<WorkFlowAssignment>>.Result>(response.Content);
            return new ApiResult<List<WorkFlowAssignment>>(result);
        }

        public async Task<IActionResult> GetRange([FromBody] ParamTaskFilter param)
        {
            param.Limit = (param.Limit <= 0) ? 10 : param.Limit;
            param.Offset = (param.Offset < 0) ? 0 : param.Offset;
            var employeeID = Session.Id();            

            var client = new Client(Configuration);
            var request = new Request($"api/common/task/range/{employeeID}", Method.POST);
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK &&  string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<List<WorkFlowAssignment>>.Result>(response.Content);
            return new ApiResult<List<WorkFlowAssignment>>(result);
        }

        public async Task<IActionResult> GetActive()
        {
            var param = new FetchParam();
            param.Limit = (param.Limit <= 0) ? 10 : param.Limit;
            param.Offset = (param.Offset < 0) ? 0 : param.Offset;

            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/active/{employeeID}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<WorkFlowAssignment>>.Result>(response.Content);
            return new ApiResult<List<WorkFlowAssignment>>(result);
        }

        public async Task<IActionResult> GetAssignee(string token)
        {            
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/assignee/{token}", Method.GET);
            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<List<Employee>>.Result>(response.Content);
            return new ApiResult<List<Employee>>(result);
        }
        
        public async Task<IActionResult> CountActive()
        {            
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/active/count/{employeeID}", Method.GET);
            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<List<object>>.Result>(response.Content);
            return new ApiResult<List<object>>(result);
        }

        public async Task<IActionResult> Approve([FromBody] ParamTask param)
        {
            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/approve", Method.POST);

            param.ActionEmployeeID = employeeID;
            param.ActionEmployeeName = employeeName;
            request.AddJsonParameter(param);

            var response = client.Execute(request);                       
            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> ApproveInvert([FromBody] ParamTask param)
        {
            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/approve/invert", Method.POST);

            param.ActionEmployeeID = employeeID;
            param.ActionEmployeeName = employeeName;
            request.AddJsonParameter(param);

            var response = client.Execute(request);                       
            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Reject([FromBody] ParamTask param)
        {
            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/reject", Method.POST);
            
            param.ActionEmployeeID = employeeID;
            param.ActionEmployeeName = employeeName;
            request.AddJsonParameter(param);

            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> RejectInvert([FromBody] ParamTask param)
        {
            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/reject/invert", Method.POST);
            
            param.ActionEmployeeID = employeeID;
            param.ActionEmployeeName = employeeName;
            request.AddJsonParameter(param);

            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Delegate([FromBody] ParamTask param)
        {
            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/delegate", Method.POST);

            param.ActionEmployeeID = employeeID;
            param.ActionEmployeeName = employeeName;
            request.AddJsonParameter(param);

            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Cancel([FromBody] ParamTask param)
        {
            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/common/task/cancel", Method.POST);

            param.ActionEmployeeID = employeeID;
            param.ActionEmployeeName = employeeName;
            request.AddJsonParameter(param);

            var response = client.Execute(request);            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Agenda()
        {
            var employeeID = Session.Id();

            var client = new Client(Configuration);
            var request = new Request($"api/common/task/agenda/{employeeID}", Method.GET);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }
    }
}