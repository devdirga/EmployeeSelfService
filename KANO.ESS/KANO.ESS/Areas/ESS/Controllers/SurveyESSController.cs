using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using RestSharp;
using System.Net;
using System.Security.Claims;
using Newtonsoft.Json;
using System.IO;
using KANO.Core.Lib.Helper;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using KANO.Core.Service.Odoo;
using System.Collections;
using Aspose.Cells;
using ParameterType = RestSharp.ParameterType;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class SurveyESSController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;

        public SurveyESSController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            var nama = "api";
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Survey"}
            };
            ViewBag.Title = "Survey";
            ViewBag.Icon = "mdi mdi-checkbox-multiple-marked-outline";
            return View();
        }

        public async Task<IActionResult> GetNad()
        {
            string URL = "http://localhost:8069/api/common/auth/login";
            string urlParameters = "?email=dzanurano@gmail.com&password=1234&company=belajar";
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(URL);

            // Add an Accept header for JSON format.
            client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
            // List data response.
            HttpResponseMessage response = client.GetAsync(urlParameters).Result;  // Blocking call! Program will wait here until a response is received or a timeout occurs.
            if (response.IsSuccessStatusCode)
            {
                // Parse the response body.
                //var dataObjects = response.Content.ReadAsAsync<IEnumerable<DataObject>>().Result;  //Make sure to add a reference to System.Net.Http.Formatting.dll
                //foreach (var d in dataObjects)
                //{
                //    Console.WriteLine("{0}", d.Name);

                //}
                var responseTask = client.GetAsync(urlParameters);
                responseTask.Wait();

                var result = responseTask.Result;
                return ApiResult<object>.Ok(result);
            }
            else
            {
                Console.WriteLine("{0} ({1})", (int)response.StatusCode, response.ReasonPhrase);
            }

            //Make any other calls using HttpClient here.

            //Dispose once all HttpClient calls are complete. This is not necessary if the containing object will be disposed of; for example in this case the HttpClient instance will be disposed automatically when the application terminates so the following call is superfluous.
            client.Dispose();

            return ApiResult<object>.Ok("Event has been saved successfully");
        }

        public async Task<IActionResult> GetSurvey()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/survey/employee", Method.POST);
            request.AddJsonParameter(Session.Id());
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Survey>>.Result>(response.Content);
            string domain = Configuration["Odoo:Domain"];
            Uri baseUri = new Uri(domain);
            foreach (var d in result.Data)
            {
                Uri myUri = new Uri(baseUri, d.SurveyUrl.AbsolutePath + "?userid=" + Session.Id() + "&umail=" + Session.Email());
                d.SurveyUrl = myUri;
            }

            var oSurveyResult = GetSurveyResultEmployee().FindAll(s=>s.CreateDate.ToLocalTime().Date == DateTime.Now.ToLocalTime().Date);

            foreach(var nadia in result.Data)
            {
                //var ooo = oSurveyResult.FindAll(t => t.CreateDate == DateTime.Now);
                if (oSurveyResult.FindAll(b => b.SurveyID[0].ToString() == nadia.OdooID).Count > 0)
                {
                    nadia.AlreadyFilled = true;
                } else
                {
                    nadia.AlreadyFilled = false;
                    Uri myRev = new Uri(baseUri, nadia.SurveyUrl.AbsolutePath);
                }
            }
            return new ApiResult<List<Survey>>(result);
        }

        [Route("/ESS/SurveyESS/Employee")]
        public List<OdooSurveyRecord> GetSurveyResultEmployee()
        {
            string sessionOdoo = Session.OdooSessionID();
            var xx = OdooService.GetSurveyResult(Configuration, sessionOdoo);
            var data = xx.Result.Records.FindAll(x => x.UserID == Session.Id() && x.Status == "done");
            return data;
        }

        [Route("/ESS/SurveyESS/History")]
        public List<OdooSurveyRecord> GetHistory([FromBody] DateRange dateRange)
        {
            string sesOdoo = Session.OdooSessionID();
            var url = Configuration["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");
            var r = Tools.normalizeFilter(dateRange);
            var pa = new ParamSurveyResult();
            pa.Params.domain.Add("&");
            pa.Params.domain.Add(new ArrayList() { "user_id", "=", Session.Id() });
            pa.Params.domain.Add(new ArrayList() { "state", "=", "done" });
            pa.Params.domain.Add(new ArrayList() { "create_date", ">=", r.Start.ToLocalTime() });
            pa.Params.domain.Add(new ArrayList() { "create_date", "<", r.Finish.AddDays(1).ToLocalTime() });
            pa.Params.fields = new paramField();
            var json = JsonConvert.SerializeObject(pa);

            var client = new RestClient(url);
            client.AddDefaultHeader("Content-Type", "application/json");

            var request = new RestRequest("/web/dataset/search_read", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.AddCookie("session_id", sesOdoo);
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            IRestResponse response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<OdooSurveyResponse>(response.Content);
            return result.Result.Records;
        }
    }
}