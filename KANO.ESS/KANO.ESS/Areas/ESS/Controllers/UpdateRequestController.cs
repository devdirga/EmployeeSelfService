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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class UpdateRequestController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;

        public UpdateRequestController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public async Task<IActionResult> Get()
        {
            var param = new FetchParam();
            param.Limit = (param.Limit <= 0) ? 10 : param.Limit;
            param.Offset = (param.Offset < 0) ? 0 : param.Offset;

            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/common/updateRequest/{employeeID}", Method.GET);
            var response = client.Execute(request);            
            if (response.StatusCode != HttpStatusCode.OK &&  string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<List<TrackingRequest>>.Result>(response.Content);
            return new ApiResult<List<TrackingRequest>>(result);
        }

        public async Task<IActionResult> GetRange([FromBody] ParamTaskFilter param)
        {   
            param.Limit = (param.Limit <= 0) ? 10 : param.Limit;
            param.Offset = (param.Offset < 0) ? 0 : param.Offset;

            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/common/updateRequest/range/{employeeID}", Method.POST);

            request.AddJsonParameter(param);
            var response = client.Execute(request);            
            if (response.StatusCode != HttpStatusCode.OK &&  string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<List<TrackingRequest>>.Result>(response.Content);
            return new ApiResult<List<TrackingRequest>>(result);
        }

        public async Task<IActionResult> GetByInstanceID(string token)
        {   
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/common/updateRequest/{employeeID}/{token}", Method.GET);
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK &&  string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<TrackingRequest>.Result>(response.Content);
            return new ApiResult<TrackingRequest>(result);
        }

        public async Task<IActionResult> GetByEmployeeInstanceID(string source, string id)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/common/updateRequest/{source}/{id}", Method.GET);
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK &&  string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<TrackingRequest>.Result>(response.Content);
            return new ApiResult<TrackingRequest>(result);
        }

        //public async Task<IActionResult> GetByEmployeeInstanceID(string source, string id)
        //{
        //    var client = new Client(Configuration);
        //    var request = new Request($"api/common/updateRequest/{source}/{id}", Method.GET);
        //    var response = client.Execute(request);

        //    var result = JsonConvert.DeserializeObject<ApiResult<TrackingRequest>.Result>(response.Content);
        //    return new ApiResult<TrackingRequest>(result);
        //}

        [AllowAnonymous]
        public IActionResult MGetByInstanceID(string source, string id)
        {
            var employeeID = source;
            var client = new Client(Configuration);
            var request = new Request($"api/common/updateRequest/{employeeID}/{id}", Method.GET);
            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK && string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);
            var result = JsonConvert.DeserializeObject<ApiResult<TrackingRequest>.Result>(response.Content);
            return new ApiResult<TrackingRequest>(result);
        }
    }
}
