using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using KANO.Core.Service;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using RestSharp;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
using KANO.Core.Lib.Helper;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class AgendaController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IUserSession Session;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public AgendaController(IConfiguration conf, IUserSession session)
        {
            Configuration = conf;
            Session = session;
        }                

        public IActionResult Get([FromBody]GridDateRange param)
        {
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/timeManagement/agenda/get/range", Method.POST);
            
            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var travel = JsonConvert.DeserializeObject<ApiResult<List<Agenda>>.Result>(response.Content);
            return new ApiResult<List<Agenda>>(travel);            
        }

        public async Task<IActionResult> Download()
        {
            var token = Request.Form["token"];
            try
            {
                Console.WriteLine($"{DateTime.Now} >>> {token}");
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl)) { return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");  }

                var decodedToken = WebUtility.UrlDecode(token);
                var filepath = Hasher.Decrypt(decodedToken);
                var filename = Path.GetFileName(filepath);

                // Fetching file info
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/timeManagement/agenda/download/{token}")))
                {
                    return File(stream.ToArray(), "application/force-download", filename);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} <<< {Format.ExceptionString(e)}");
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e);
                return View("Error");
            }
        }

    }
}