using System;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using KANO.Core.Model;
using KANO.Core.Lib.Middleware.ServerSideAnalytics.Mongo;
using System.Collections.Generic;
using KANO.Core.Service;
using System.Net.Mail;
using System.Net;
using KANO.Core.Lib.Helper;

namespace KANO.ESS.Areas.Site
{
    [Area("Site")]
    public class DiagnosticController : Controller
    {
        private IConfiguration Configuration;

        public DiagnosticController(IConfiguration config)
        {
            Configuration = config;
        }

        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Log()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetHealthCheck()
        {
            var gatewayURL = Configuration["Request:GatewayUrl"];
            var uri = new Uri(gatewayURL);
            var newGatewayURL = $"{uri.Scheme}://{uri.Host}:{uri.Port}/";
            var cl = new Client(newGatewayURL);
            var req = new Request($"hc", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<Object>(resp.Content);
            return ApiResult<Object>.Ok(result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> GetLogs([FromBody] KendoGrid param)
        {
            var cl = new Client(Configuration);
            var req = new Request($"api/common/logger/get", Method.POST);
            req.AddJsonParameter(param);
            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<List<MongoWebRequest>>.Result>(resp.Content);
            return new ApiResult<List<MongoWebRequest>>(result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> TestNotification(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new ApiResult<string>(HttpStatusCode.BadRequest, "Well you forgot the param", null);
            }
            var cl = new Client(Configuration);
            var req = new Request($"api/notification/send", Method.POST);
            req.AddJsonParameter(new {
                Type=2,
	            Receiver= token,
	            Module="default",
	            Action="default",
	            Title="Notification Testing",
	            Message="Please just ignore this notification"
            });

            var resp = cl.Execute(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(resp.Content);
            return new ApiResult<object>(result);
        }

        [AllowAnonymous]
        public async Task<IActionResult> TestEmailSend(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return new ApiResult<string>(HttpStatusCode.BadRequest, "Well you forgot the param", null);
            }
            try
            {
                var mailer = new Mailer(Configuration);
                string Subject = "Email Testing";
                string Body = $"Dear {token},<br/>" +
                                    "Please ignore this message<br/>" +
                                    "Copyright © 2020 KANO";

                var message = new MailMessage();
                message.To.Add(token);
                message.Subject = Subject;
                message.Body = string.Format(Body);
                mailer.SendMail(message);

                return ApiResult<object>.Ok($"email has been sent successfully to ${token}");
            }
            catch (Exception e)
            {
                return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, Format.ExceptionString(e));
            }
        }
    }
}