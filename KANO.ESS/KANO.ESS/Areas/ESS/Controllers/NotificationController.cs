using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RestSharp;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class NotificationController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string api = "api/notification/";
        private static readonly string apiAbsenceUser = "api/absence/user/";
        private readonly String ApiNotification = "api/notification/";
        private readonly String BearerAuth = "Bearer ";

        public NotificationController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public async Task<IActionResult> Get([FromBody] FetchParam param)
        {
            param.Limit = (param.Limit <= 0)? 10: param.Limit;
            param.Offset = (param.Offset < 0)? 0: param.Offset;
            param.Filter = (string.IsNullOrEmpty(param.Filter))? "all": param.Filter;

            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/notification/get/{employeeID}/{param.Limit}/{param.Offset}/{param.Filter}", Method.GET);
            var response = client.Execute(request);           
            if (response.StatusCode != HttpStatusCode.OK &&  string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Notification>>.Result>(response.Content);
            return new ApiResult<List<Notification>>(result);
        }

        public async Task<IActionResult> Subscribe([FromBody] NotificationSubscription param)
        {
            if (string.IsNullOrWhiteSpace(param.Receiver)) {
                param.Receiver = Session.Id();
                param.ReceiverName = Session.DisplayName();
            }

            var client = new Client(Configuration);
            var request = new Request($"api/notification/subscribe", Method.POST);
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK && string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<NotificationSubscription>.Result>(response.Content);
            return new ApiResult<NotificationSubscription>(result);
        }

        public async Task<IActionResult> Unubscribe([FromBody] NotificationSubscription param)
        {
            if (string.IsNullOrWhiteSpace(param.Receiver))
            {
                param.Receiver = Session.Id();
                param.ReceiverName = Session.DisplayName();
            }

            var client = new Client(Configuration);
            var request = new Request($"api/notification/unsubscribe", Method.POST);
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK && string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<NotificationSubscription>.Result>(response.Content);
            return new ApiResult<NotificationSubscription>(result);
        }

        public async Task<IActionResult> SubscriptionCheck([FromBody] NotificationSubscription param)
        {

            var receiver = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/notification/subscription/check", Method.POST);
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK && string.IsNullOrWhiteSpace(response.Content)) return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);

            var result = JsonConvert.DeserializeObject<ApiResult<bool>.Result>(response.Content);
            return new ApiResult<bool>(result);
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [HttpPost]
        [AllowAnonymous]
        public IActionResult MGet([FromBody] FetchParam param)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            param.Limit = (param.Limit <= 0) ? 10 : param.Limit;
            param.Offset = (param.Offset < 0) ? 0 : param.Offset;
            param.Filter = (string.IsNullOrEmpty(param.Filter)) ? "all" : param.Filter;
            var response = new Client(Configuration).Execute(new Request($"{ApiNotification}mget", Method.POST, param, "Authorization", bearerAuth));
            if (response.StatusCode != HttpStatusCode.OK && string.IsNullOrWhiteSpace(response.Content)) {
                return ApiResult<object>.Error(response.StatusCode, response.StatusDescription);
            }
            return new ApiResult<List<Notification>>(JsonConvert.DeserializeObject<ApiResult<List<Notification>>.Result>(response.Content));
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult MSetRead([FromBody] Notification p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            var response = new Client(Configuration).Execute(new Request($"{ApiNotification}msetread", Method.POST, p, "Authorization", bearerAuth));
            if (response.StatusCode != HttpStatusCode.OK && string.IsNullOrWhiteSpace(response.Content)) {
                return ApiResult<Notification>.Error(response.StatusCode, response.StatusDescription);
            }
            return new ApiResult<Notification>(JsonConvert.DeserializeObject<ApiResult<Notification>.Result>(response.Content));
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult TestNotif([FromBody] Notification p)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            var response = new Client(Configuration).Execute(new Request($"{ApiNotification}msendfromax", Method.POST, p, "Authorization", bearerAuth));
            if (response.StatusCode != HttpStatusCode.OK && string.IsNullOrWhiteSpace(response.Content)) {
                return ApiResult<Notification>.Error(response.StatusCode, response.StatusDescription);
            }
            return new ApiResult<Notification>(JsonConvert.DeserializeObject<ApiResult<Notification>.Result>(response.Content));
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult SendFirebase([FromBody] Notification p)
        {
            var apiFirebase = Configuration.GetSection("Request:FcmApi").Value;
            var key = Configuration.GetSection("Request:FcmKey").Value;
            try {
                p.Timestamp = DateTime.Now;
                if (string.IsNullOrWhiteSpace(p.Receiver)) {
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Receiver could not be empty");
                }
                if (string.IsNullOrWhiteSpace(p.Sender)) {
                    p.Sender = Notification.DEFAULT_SENDER;
                }
                User user = JsonConvert.DeserializeObject<ApiResult<User>.Result>(new Client(Configuration).Execute(new Request($"{apiAbsenceUser}userbyusername/{p.Receiver}", Method.GET)).Content).Data;
                WebRequest webRequest = WebRequest.Create(new Uri(apiFirebase));
                webRequest.Method = "POST";
                webRequest.Headers.Add($"Authorization: key={key}");
                webRequest.ContentType = "application/json";
                byte[] byteArray = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new {
                    notification = new { body = p.Message },
                    data = new { module = p.Module, value = p.Message },
                    to = user.FirebaseToken,
                    priority = "high",
                    direct_boot_ok = true
                }));
                webRequest.ContentLength = byteArray.Length;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                using (Stream dataStream = webRequest.GetRequestStream()) {
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    using (WebResponse webResponse = webRequest.GetResponse()) {
                        using (Stream dataStreamResponse = webResponse.GetResponseStream()) {
                            using (StreamReader tReader = new StreamReader(dataStreamResponse)) {
                                tReader.ReadToEnd();
                            }
                        }
                    }
                }
                return ApiResult<Notification>.Ok("success");
            }
            catch (Exception e) { return ApiResult<Notification>.Error(HttpStatusCode.BadGateway, e.Message); }
        }
    }
}