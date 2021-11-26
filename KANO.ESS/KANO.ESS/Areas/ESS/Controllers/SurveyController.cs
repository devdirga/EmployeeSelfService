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
using KANO.Core.Service.Odoo;
using MongoDB.Bson.Serialization;
using System.Collections;
using Aspose.Cells;
using ParameterType = RestSharp.ParameterType;
using Aspose.Cells.Charts;
using System.Reflection;
using System.Drawing;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class SurveyController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;
        private string TokenOdoo;
        private string xUrl, xUser, xPass, xDB, errorMessage, statusMessage;

        private static List<Dictionary<string, string>> GridSummary = new List<Dictionary<string, string>>();

        public SurveyController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;

            var genToken = GetToken(Configuration);
            if (genToken == null)
            {
                TokenOdoo = "null";
            }
            else
            {
                TokenOdoo = genToken.access_token;
            }
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Survey"}
            };
            ViewBag.Title = "Survey";
            ViewBag.Icon = "mdi mdi-checkbox-multiple-marked-outline";
            //ViewBag.Domain = Configuration["Odoo:Domain"] + "web?employee_id=" + Session.Id() + "#action=142&model=survey.survey&view_type=kanban&cids=1&menu_id=99";
            //ViewBag.OdooSession = Session.OdooSessionID();

            ViewBag.Domain = xUrl ?? "-";
            ViewBag.OdooSession = Session.OdooSessionID();
            ViewBag.TokenOdoo = TokenOdoo;
            ViewBag.Url = xUrl ?? "-";
            ViewBag.OdooEmail = xUser ?? "-";
            ViewBag.OdooPassword = xPass ?? "-";
            ViewBag.OdooCompany = xDB ?? "-";
            ViewBag.ErrorMessage = errorMessage;
            ViewBag.StatusMessage = statusMessage;
            return View();
        }

        public IActionResult New()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Survey"},
                new Breadcrumb{Title="New"}
            };
            ViewBag.Title = "Create Survey";
            ViewBag.Icon = "mdi mdi-checkbox-multiple-marked-outline";
            ViewBag.Action = "create";
            return View("~/Areas/ESS/Views/Survey/Form.cshtml");
        }

        public IActionResult Edit(string token)
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Survey"},
                new Breadcrumb{Title="Edit"}
            };
            ViewBag.Title = "Edit Survey";
            ViewBag.Icon = "mdi mdi-checkbox-multiple-marked-outline";
            ViewBag.Action = "update";
            ViewBag.ID = token;

            var result = this.getByID(token);
            ViewBag.StatusCode = result.StatusCode;
            ViewBag.Message = result.Message;
            ViewBag.Data = result.Data;

            return View("~/Areas/ESS/Views/Survey/Form.cshtml");
        }

        public IActionResult History()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Survey"},
                new Breadcrumb{Title="History"}
            };
            ViewBag.Title = "Survey History";
            ViewBag.Icon = "mdi mdi-checkbox-multiple-marked-outline";

            return View();
        }

        public IActionResult Summary(string token)
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Survey"},
                new Breadcrumb{Title="Summary"}
            };
            ViewBag.Title = "Survey Summary";
            ViewBag.Icon = "mdi mdi-checkbox-multiple-marked-outline";
            ViewBag.ID = token;
            return View();
        }

        public IActionResult SummaryById(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/survey/summary/{token}", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<SurveySummary>.Result>(response.Content);
            return new ApiResult<SurveySummary>(result);
        }

        public async Task<IActionResult> SummaryId([FromBody] SurveyForm param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/survey/summary", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<SurveySummary>.Result>(response.Content);
            return new ApiResult<SurveySummary>(result);
        }

        public async Task<IActionResult> Participant([FromBody] SurveyForm param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/survey/participant", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<SurveySummary>.Result>(response.Content);
            return new ApiResult<SurveySummary>(result);
        }

        public IActionResult Test(string token)
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Survey"},
                new Breadcrumb{Title="Test"}
            };
            ViewBag.Title = "Test Survey";
            ViewBag.Icon = "mdi mdi-checkbox-multiple-marked-outline";
            ViewBag.ID = token;

            var result = this.getByID(token);
            ViewBag.StatusCode = result.StatusCode;
            ViewBag.Message = result.Message;
            ViewBag.Data = result.Data;
            return View("~/Areas/ESS/Views/Survey/Survey.cshtml");
        }

        public IActionResult Fill(string token)
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Survey"},
                new Breadcrumb{Title="Test"}
            };
            ViewBag.Title = "Survey";
            ViewBag.Icon = "mdi mdi-checkbox-multiple-marked-outline";
            ViewBag.ID = token;

            var result = this.getByID(token);
            ViewBag.StatusCode = result.StatusCode;
            ViewBag.Message = result.Message;
            ViewBag.Data = result.Data;
            return View("~/Areas/ESS/Views/Survey/Survey.cshtml");
        }

        public IActionResult TestExternal()
        {
            return View();
        }

        public async Task<IActionResult> GetRange([FromBody] DateRange range)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/survey/range", Method.POST);

            request.AddJsonParameter(range);

            var tasks = new List<Task<TaskRequest<bool>>>();

            var result = new ApiResult<List<Survey>>.Result();
            tasks.Add(Task.Run(() =>
            {
                var response = client.Execute(request);
                result = JsonConvert.DeserializeObject<ApiResult<List<Survey>>.Result>(response.Content);
                return TaskRequest<bool>.Create("Survey", true);
            }));

            var odooData = new List<OdooSurvey>();
            tasks.Add(Task.Run(() =>
            {
                odooData = GetSurveyOdoo();
                return TaskRequest<bool>.Create("Odoo", true);
            }));

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                throw e;
            }

            foreach (var anye in result.Data)
            {
                var res = odooData.Find(x => x.Id.ToString() == anye.OdooID);
                if (odooData.Exists(y => y.Id.ToString() == anye.OdooID)) {
                    var dinda = odooData.Find(x => x.Id.ToString() == anye.OdooID);
                    dinda.DBId = anye.Id;
                    dinda.DBCreatedDate = anye.CreatedDate;
                    dinda.Schedule = anye.Schedule;
                    dinda.Recurrent = anye.Recurrent;
                    dinda.Participants = anye.Participants;
                    dinda.ParticipantType = anye.ParticipantType;
                    dinda.Published = anye.Published;
                    dinda.IsRequired = anye.IsRequired;
                    dinda.Departments = anye.Departments;
                }
            }
            //var rex = JsonConvert.DeserializeObject<ApiResult<List<OdooSurvey>>.Result>(odooData));
            //return new ApiResult<OdooSurvey>(odooData);
            //return ApiResult.Ok(odooData);
            return ApiResult<List<OdooSurvey>>.Ok(odooData);
        }

        public async Task<IActionResult> Get(string token)
        {
            var result = this.getByID(token);
            return new ApiResult<Survey>(result);
        }

        private ApiResult<Survey>.Result getByID(string id) {
            var client = new Client(Configuration);
            var request = new Request($"api/survey/{id}", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<Survey>.Result>(response.Content);
            return result;

        }

        public async Task<IActionResult> GetHistories([FromBody] DateRange range)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/survey/histories/{employeeID}", Method.POST);

            request.AddJsonParameter(range);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<Survey>.Result>(response.Content);
            return new ApiResult<Survey>(result);
        }

        public async Task<IActionResult> Save([FromBody] Survey param)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/survey/save", Method.POST);

            if (string.IsNullOrWhiteSpace(param.Id)) {
                param.CreatedBy = employeeID;
            } else {
                param.UpdateBy = employeeID;
            }
            
            string domain = Configuration["Odoo:Domain"];
            Uri baseUri = new Uri(domain);
            Uri myUri = new Uri(baseUri, param.SurveyUrl.AbsolutePath + "?userid=" + Session.Id() + "&umail=" + Session.Email());
            param.SurveyUrl = myUri;
            // TODO : remove on next development
            //param.ParticipantType = ParticipantType.All;
            //param.Participants = new List<string>();
            //param.Participants.Add("7312020022");

            try
            {
                if (param.ParticipantType == ParticipantType.Department)
                {
                    if (param.Departments.Count() == 0)
                    {
                        throw new Exception("No department selected");
                    }
                }

                if (param.ParticipantType == ParticipantType.Selected && param.Participants.Count() == 0)
                {
                    throw new Exception("No participant selected");
                }

                request.AddJsonParameter(param);
                var response = client.Execute(request);
                var result = JsonConvert.DeserializeObject<ApiResult<Survey>.Result>(response.Content);
                return new ApiResult<Survey>(result);
            } catch(Exception e)
            {
                //throw e;
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error save data :\n{Format.ExceptionString(e)}");
            }
        }

        public async Task<IActionResult> Publish(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/survey/publish/{token}", Method.POST);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<Survey>.Result>(response.Content);
            return new ApiResult<Survey>(result);
        }

        public async Task<IActionResult> Unpublish(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/survey/unpublish/{token}", Method.POST);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<Survey>.Result>(response.Content);
            return new ApiResult<Survey>(result);
        }

        public OdooToken GetToken(IConfiguration config)
        {
            var url = config["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");
            var username = config["Odoo:Username"];
            if (string.IsNullOrWhiteSpace(username))
                throw new Exception("Odoo:Username is not set in the configuration!");
            var password = config["Odoo:Password"];
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Odoo:Password is not set in the configuration!");
            var db = config["Odoo:DB"];
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Odoo:DB is not set in the configuration!");

            try
            {
                var client = new RestClient(url);
                var request = new RestRequest("/api/common/auth/login", Method.GET);

                request.AddQueryParameter("email", username);
                request.AddQueryParameter("password", password);
                request.AddQueryParameter("company", db);
                var response = client.Execute(request);

                xUrl = $"{response.ResponseUri.OriginalString}";
                xUser = username;
                xPass = password;
                xDB = db;

                errorMessage = response.ErrorMessage;
                statusMessage = response.StatusCode.ToString();
                //var status = StatusCode(response.StatusCode != System.Net.HttpStatusCode.OK ? 400 : 201);

                //TODO: transform the response here to suit your needs
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var res = JsonConvert.DeserializeObject<OdooToken>(response.Content);
                    return res;
                }
                else
                {
                    return null;
                }
            } catch (Exception e)
            {
                //throw e;
                var obj = new OdooToken();
                obj.access_token = "Error:" + e.Message;
                return obj;
            }
        }

        public List<OdooSurvey> GetSurveyOdoo()
        {
            PackageSurvey packageSurvey = new PackageSurvey();
            PackageSurveyObject packageSurveyDictionary = new PackageSurveyObject();
            List<OdooSurvey> result = new List<OdooSurvey>();
            List<Survey> surveys = new List<Survey>();

            //string url = "http://localhost:8069/api/object/survey.survey";
            string url = Configuration["Odoo:Url"];
            string domain = Configuration["Odoo:Domain"];
            //var data = GetToken(Configuration);

            //string bearerToken = data.access_token;
            //var client = new RestClient(url);
            //client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", TokenOdoo));

            //var request = new RestRequest("/api/object/survey.survey", Method.GET);

            string sessionOddo = Session.OdooSessionID();
            var pa = new ParamSurvey();
            var json = JsonConvert.SerializeObject(pa);

            var client = new RestClient(url);
            client.AddDefaultHeader("Content-Type", "application/json");

            var request = new RestRequest("/web/dataset/search_read", Method.POST);
            request.AddHeader("Accept", "application/json");
            request.AddCookie("session_id", sessionOddo);
            request.AddParameter("application/json", json, ParameterType.RequestBody);

            Uri baseUri = new Uri(domain);
            try
            {
                //var tasks = new List<Task<TaskRequest<bool>>>();

                //tasks.Add(Task.Run(() =>
                //{
                //    var response = client.Execute(request);
                //    var result = JsonConvert.DeserializeObject<PackageSurvey>(response.Content);
                //    packageSurvey = result;

                //    return TaskRequest<bool>.Create("Survey", true);
                //}));

                //tasks.Add(Task.Run(() =>
                //{
                //    var c = new Client(Configuration);
                //    var req = new Request($"api/survey/gets", Method.GET);
                //    req.AddJsonParameter(Session.Id());
                //    var resp = c.Execute(req);

                //    var res = JsonConvert.DeserializeObject<ApiResult<List<Survey>>.Result>(resp.Content);
                //    surveys = res.Data;
                //    return TaskRequest<bool>.Create("Survey", true);
                //}));

                //var t = Task.WhenAll(tasks);
                //try
                //{
                //    t.Wait();
                //}
                //catch (Exception e)
                //{
                //    throw e;
                //}

                var temp = new Survey();

                var c = new Client(Configuration);
                var req = new Request($"api/survey/gets", Method.GET);
                req.AddJsonParameter(Session.Id());
                var resp = c.Execute(req);


                var res = JsonConvert.DeserializeObject<ApiResult<List<Survey>>.Result>(resp.Content);
                surveys = res.Data;

                var response = client.Execute(request);
                var responseOdoo = JsonConvert.DeserializeObject<OdooSurveyResponseData>(response.Content);
                foreach (var xx in responseOdoo.Result.Records)
                {
                    Uri myUri = new Uri(baseUri, xx.PublicUrl.AbsolutePath + "?userid=" + Session.Id() + "&umail=" + Session.Email());
                    xx.SurveyUrl = myUri;
                    temp = surveys.Find(s => s.OdooID == xx.Id.ToString());
                    if (temp != null)
                    {
                        xx.DBId = temp.Id;
                        xx.Published = temp.Published;
                        xx.Schedule = temp.Schedule;
                        xx.Recurrent = temp.Recurrent;
                        xx.Participants = temp.Participants;
                    }
                }
                result = responseOdoo.Result.Records;
                //var checkObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.Content);
                //if (checkObject["survey.survey"].GetType().Name == "JObject")
                //{
                //    var resObj = JsonConvert.DeserializeObject<PackageSurveyObject>(response.Content);
                //    packageSurveyDictionary = resObj;

                //    Uri myUri = new Uri(baseUri, packageSurveyDictionary.DataSurvey.PublicUrl.AbsolutePath + "?userid=" + Session.Id() + "&umail=" + Session.Email());
                //    packageSurveyDictionary.DataSurvey.SurveyUrl = myUri;
                //    temp = surveys.Find(s => s.OdooID == packageSurveyDictionary.DataSurvey.Id.ToString());
                //    if (temp != null)
                //    {
                //        packageSurveyDictionary.DataSurvey.DBId = temp.Id;
                //        packageSurveyDictionary.DataSurvey.Published = temp.Published;
                //        packageSurveyDictionary.DataSurvey.Schedule = temp.Schedule;
                //        packageSurveyDictionary.DataSurvey.Recurrent = temp.Recurrent;
                //        packageSurveyDictionary.DataSurvey.Participants = temp.Participants;
                //    }

                //    result.Add(packageSurveyDictionary.DataSurvey);
                //}
                //else if (checkObject["survey.survey"].GetType().Name == "JArray")
                //{
                //    var resObj = JsonConvert.DeserializeObject<PackageSurvey>(response.Content);
                //    packageSurvey = resObj;

                //    foreach (var xx in packageSurvey.DataSurvey)
                //    {
                //        Uri myUri = new Uri(baseUri, xx.PublicUrl.AbsolutePath + "?userid=" + Session.Id() + "&umail=" + Session.Email());
                //        xx.SurveyUrl = myUri;
                //        temp = surveys.Find(s => s.OdooID == xx.Id.ToString());
                //        if (temp != null)
                //        {
                //            xx.DBId = temp.Id;
                //            xx.Published = temp.Published;
                //            xx.Schedule = temp.Schedule;
                //            xx.Recurrent = temp.Recurrent;
                //            xx.Participants = temp.Participants;
                //        }
                //    }
                //    result = packageSurvey.DataSurvey.ToList();
                //}
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading data :\n{Format.ExceptionString(e)}");
            }

            //return packageSurvey.DataSurvey.FindAll(c=>c.Published == true).ToList();
            //return Ok(result.DataSurvey);
            //return packageSurvey.DataSurvey.ToList();
            //return result.FindAll(c => c.State == "open");
            return result;

        }

        public OdooSurveyResponse GetSurveyResult([FromBody] DateRange param)
        {
            string sessionOdoo = Session.OdooSessionID();
            var xx = OdooService.GetSurveyResult(Configuration, sessionOdoo);
            return xx;
        }

        public IActionResult GetSurveyRecordById([FromBody] KendoGrid param)
        {
            string sessionOddo = Session.OdooSessionID();
            //var xx = OdooService.GetSurveyResult(Configuration, sessionOddo);
            //var data = xx.Result.Records.FindAll(u => u.SurveyID[0].ToString() == param.Filter.Filters[0].Value).OrderByDescending(c => c.CreateDate).ToList();
            //var result = data.FindAll(u => u.SurveyID[0].ToString() == param.Filter.Filters[0].Value).Skip(param.Skip).Take(param.Take);
            //return Ok(new { Data = result, Total = data.Count});
            List<OdooSurveyRecord> record = new List<OdooSurveyRecord>();
            List<Department> empData = new List<Department>();
            List<SurveyReport> reports = new List<SurveyReport>();
            List<string> participants = new List<string>();
            //GridSurveyReport grid = new GridSurveyReport();
            List<Dictionary<string, object>> grid = new List<Dictionary<string, object>>();
            Dictionary<string, object> rowData = new Dictionary<string, object>();

            string excel = "";
            string pdf = "";

            int total = 0;

            var url = Configuration["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");

            try
            {
                var pa = new ParamSurveyResult();
                pa.Params.domain.Add("&");
                pa.Params.domain.Add(new ArrayList() { "survey_id", "=", param.Id });
                pa.Params.domain.Add(new ArrayList() { "state", "=", "done" });
                pa.Params.fields = new paramField();
                var json = JsonConvert.SerializeObject(pa);
                OdooSurveyResponse resJson = new OdooSurveyResponse();
                

                var client = new RestClient(url);
                client.AddDefaultHeader("Content-Type", "application/json");

                var tasks = new List<Task<TaskRequest<bool>>>();
                tasks.Add(Task.Run(() =>
                {
                    var request = new RestRequest("/web/dataset/search_read", Method.POST);
                    request.AddHeader("Accept", "application/json");
                    request.AddCookie("session_id", sessionOddo);
                    request.AddParameter("application/json", json, ParameterType.RequestBody);

                    IRestResponse response = client.Execute(request);
                    resJson = JsonConvert.DeserializeObject<OdooSurveyResponse>(response.Content);

                    //var data = resJson.Result.Records.FindAll(u => u.SurveyID[0].ToString() == param.Filter.Filters[0].Value).OrderByDescending(c => c.CreateDate).ToList();
                    //var result = data.FindAll(u => u.SurveyID[0].ToString() == param.Filter.Filters[0].Value).Skip(param.Skip).Take(param.Take);

                    return TaskRequest<bool>.Create("Survey", true);
                }));

                tasks.Add(Task.Run(() =>
                {
                    var c = new Client(Configuration);
                    var req = new Request($"api/survey/data/employee", Method.GET);

                    var resp = c.Execute(req);

                    var res = JsonConvert.DeserializeObject<ApiResult<List<Department>>.Result>(resp.Content);
                    empData = res.Data;
                    return TaskRequest<bool>.Create("Employee", true);
                }));

                tasks.Add(Task.Run(() =>
                {
                    int surveyId = param.Id;
                    var c = new Client(Configuration);
                    var req = new Request($"api/survey/data/participants/{surveyId}", Method.GET);

                    var resp = c.Execute(req);

                    var res = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(resp.Content);
                    participants = res.Data;
                    return TaskRequest<bool>.Create("Participants", true);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                var data = resJson.Result.Records.OrderByDescending(c => c.CreateDate).ToList();
                //total = data.Count();
                //record = data.Skip(param.Skip).Take(param.Take).ToList();
                record = data;

                foreach(var ss in record)
                {
                    var p = empData.Find(y => y.Id == ss.UserID);
                    if (p != null) {
                        ss.Name = p.EmployeeName;
                        ss.Department = p.DepartmentName;
                    }
                }

                var temp = new OdooSurveyRecord();
                var currDate = DateTime.Now;
                var allDate = GetDates(currDate.Year, currDate.Month);

                var groupName = record.GroupBy(g => new { EmployeeID = g.UserID, FullName = g.Name, g.Department }).ToList();
                
                int i = 0;
                foreach (var ee in participants)
                {
                    rowData = new Dictionary<string, object>();
                    var group = groupName.Find(k => k.Key.EmployeeID == ee);
                    if (group == null) {
                        var p = empData.Find(y => y.Id == ee);
                        if(p != null)
                        {
                            rowData.Add("EmployeeID", ee);
                            rowData.Add("FullName", p.EmployeeName);
                            rowData.Add("Department", p.DepartmentName);
                            foreach (var anye in allDate)
                            {
                                rowData.Add("D" + anye.ToString("dd"), "");
                            }
                            grid.Add(rowData);
                        }
                        continue;
                    };
                    reports.Add(new SurveyReport {
                        EmployeeID = group.Key.EmployeeID,
                        FullName = group.Key.FullName,
                        Department = group.Key.Department
                    });
                    
                    rowData.Add("EmployeeID", group.Key.EmployeeID);
                    rowData.Add("FullName", group.Key.FullName);
                    rowData.Add("Department", group.Key.Department);
                    foreach (var dinda in allDate)
                    {
                        temp = group.Where(gg => gg.CreateDate.ToLocalTime().Date == dinda.ToLocalTime().Date).FirstOrDefault();
                        if (temp == null)
                        {
                            rowData.Add("D"+dinda.ToString("dd"), "");                            
                            reports[i].DateScore.Add(dinda, 0);
                        } else
                        {
                            //rowData.Add("D"+dinda.ToString("dd"), temp.Score != 0 ? temp.Score.ToString() : "Ok");
                            rowData.Add("D" + dinda.ToString("dd"), new {
                                Ids = temp.Id,
                                Score = temp.Score != 0 ? temp.Score.ToString() : "Done",
                                PassedCategory = temp.PassedCategory,
                                PassingGradeCategory = temp.PassingGradeCategory,
                                ScoringType = temp.ScoringType
                            });
                            reports[i].DateScore.Add(dinda, temp.Score);
                        }
                    }
                    grid.Add(rowData);
                    i++;
                }

                var exportHistory = ExportHistory(grid, param.Id, param.Type);
                excel = exportHistory.Value.Excel;
                pdf = exportHistory.Value.PDF;                           

                reports = reports.Skip(param.Skip).Take(param.Take).ToList();
                total = groupName.Count();

            } catch(Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading data :\n{Format.ExceptionString(e)}");
            }

            return Ok(new { Data = record, TotalSurvey = total, Employee = empData, Report = reports, Grid = grid, ExcelFile = excel, PDFFile = pdf });
        }

        public dynamic ExportHistory(List<Dictionary<string, object>> gridHistory, int id, string passingType)
        {
            Survey survey = new Survey();
            License lic = new License();
            try
            {
                var tasks = new List<Task<TaskRequest<bool>>>();
                tasks.Add(Task.Run(() =>
                {
                    var c = new Client(Configuration);
                    var req = new Request($"api/survey/get/{id}", Method.GET);
                    req.AddJsonParameter(Session.Id());
                    var resp = c.Execute(req);

                    var res = JsonConvert.DeserializeObject<ApiResult<Survey>.Result>(resp.Content);
                    survey = res.Data;
                    return TaskRequest<bool>.Create("Survey", true);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                Stream stream = System.IO.File.OpenRead(Directory.GetCurrentDirectory() + @"\License\Aspose.Total.lic");
                lic.SetLicense(stream);
                stream.Close();

                //string dirExport = Path.Combine("E:", "FileESS");
                string dirExport = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");

                Workbook workbook = new Workbook();

                Worksheet sheet = workbook.Worksheets[0];
                sheet.PageSetup.Orientation = PageOrientationType.Landscape;
                AutoFitterOptions oAutoFitterOptions = new AutoFitterOptions { AutoFitMergedCells = true, OnlyAuto = true };

                string[] alpha = new string[] {
                    "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
                    "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T",
                    "U", "V", "W", "X", "Y", "Z", "AA", "AB", "AC", "AD",
                    "AE", "AF", "AG", "AH", "AI", "AJ"
                };

                sheet.Cells["A1"].Value = survey.Title;

                int lenCol = gridHistory[0].Count();
                int i = 0;
                foreach (KeyValuePair<string, object> o in gridHistory[0])
                {
                    if (i < 3)
                    {
                        sheet.Cells[alpha[i] + "3"].PutValue(o.Key);
                        //sheet.Cells[alpha[i] + row.ToString()].PutValue(o.Value);
                    }
                    else
                    {
                        sheet.Cells[alpha[i] + "3"].PutValue(o.Key.Substring(1));
                        //sheet.Cells[alpha[i] + row.ToString()].PutValue(o.Value);
                    }

                    i++;
                }
                int row = 4;
                foreach (var obj in gridHistory)
                {
                    i = 0;
                    foreach (KeyValuePair<string, object> oxx in obj)
                    {
                        dynamic silvia = oxx.Value;
                        if (i < 3)
                        {
                            //sheet.Cells[alpha[i] + row.ToString()].PutValue(o.Key);
                            if (silvia == null)
                            {
                                sheet.Cells[alpha[i] + row.ToString()].PutValue("-");
                            } else
                            {
                                sheet.Cells[alpha[i] + row.ToString()].PutValue(silvia);
                            }
                            
                        }
                        else
                        {
                            if (silvia == null)
                            {
                                sheet.Cells[alpha[i] + row.ToString()].PutValue("-");
                            } else
                            {
                                var tt = silvia.GetType().GetProperty("Score");
                                if (tt == null)
                                {
                                    sheet.Cells[alpha[i] + row.ToString()].PutValue("-");
                                }
                                else
                                {
                                    if (silvia.PassedCategory == "npassed" || silvia.PassedCategory == "passed")
                                    {
                                        if (silvia.ScoringType == "no_scoring")
                                        {
                                            sheet.Cells[alpha[i] + row.ToString()].PutValue(silvia.Score);
                                        } else
                                        {
                                            sheet.Cells[alpha[i] + row.ToString()].PutValue(silvia.Score + "%");
                                        }
                                        
                                    }
                                    else
                                    {
                                        if (passingType == "1")
                                        {
                                            sheet.Cells[alpha[i] + row.ToString()].PutValue(silvia.Score + "%");
                                        } else
                                        {
                                            sheet.Cells[alpha[i] + row.ToString()].PutValue(silvia.PassingGradeCategory);
                                        }                                        
                                    }                                
                                }
                            }
                            //sheet.Cells.Columns[i].Width = 8;
                        }

                        i++;
                    }
                    row++;
                }
                Range _range;
                _range = sheet.Cells.CreateRange("A3", alpha[lenCol-1] + (gridHistory.Count()+4).ToString());

                Style style = workbook.CreateStyle();
                style.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
                _range.ApplyStyle(style, new StyleFlag() { Borders = true });

                PdfSaveOptions pdfSaveOptions = new PdfSaveOptions();
                pdfSaveOptions.AllColumnsInOnePagePerSheet = true;

                sheet.AutoFitRows(oAutoFitterOptions);
                sheet.AutoFitColumns(oAutoFitterOptions);

                string exportExcel = Path.Combine(dirExport, "History " + survey.Title + ".xlsx");
                string exportPdf = Path.Combine(dirExport, "History " + survey.Title + ".pdf");

                workbook.Save(exportExcel);
                workbook.Save(exportPdf, pdfSaveOptions);

                return Ok(new {
                    Excel = "/files/History " + survey.Title + ".xlsx",
                    PDF = "/files/History " + survey.Title + ".pdf"
                });
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading data :\n{Format.ExceptionString(e)}");
            }

        }

        public IActionResult GetSumaryById([FromBody] KendoGrid param)
        {
            GridSummary = new List<Dictionary<string, string>>();
            string sessionOddo = Session.OdooSessionID();
            int total = 0;
            List<Dictionary<string, string>> grid = new List<Dictionary<string, string>>();
            Dictionary<string, string> rowData = new Dictionary<string, string>();
            List<OdooSurveyRecord> record = new List<OdooSurveyRecord>();
            List<string> participants = new List<string>();
            string excel = "";
            string pdf = "";

            var url = Configuration["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");
            try
            {
                var pa = new ParamSurveyResult();
                pa.Params.domain.Add("&");
                pa.Params.domain.Add("&");
                pa.Params.domain.Add(new ArrayList() { "survey_id", "=", param.Id });
                pa.Params.domain.Add(new ArrayList() { "state", "=", "done" });
                pa.Params.domain.Add(new ArrayList() { "user_id", "!=", "" });
                pa.Params.fields = new paramField();
                var json = JsonConvert.SerializeObject(pa);
                OdooSurveyResponse resJson = new OdooSurveyResponse();

                var client = new RestClient(url);
                client.AddDefaultHeader("Content-Type", "application/json");

                var tasks = new List<Task<TaskRequest<bool>>>();

                tasks.Add(Task.Run(() =>
                {
                    var request = new RestRequest("/web/dataset/search_read", Method.POST);
                    request.AddHeader("Accept", "application/json");
                    request.AddCookie("session_id", sessionOddo);
                    request.AddParameter("application/json", json, ParameterType.RequestBody);

                    IRestResponse response = client.Execute(request);
                    resJson = JsonConvert.DeserializeObject<OdooSurveyResponse>(response.Content);
                    record = resJson.Result.Records;
                    //var data = resJson.Result.Records.FindAll(u => u.SurveyID[0].ToString() == param.Filter.Filters[0].Value).OrderByDescending(c => c.CreateDate).ToList();
                    //var result = data.FindAll(u => u.SurveyID[0].ToString() == param.Filter.Filters[0].Value).Skip(param.Skip).Take(param.Take);

                    return TaskRequest<bool>.Create("Survey", true);
                }));

                //tasks.Add(Task.Run(() =>
                //{
                //    var c = new Client(Configuration);
                //    var req = new Request($"api/survey/data/employee", Method.GET);

                //    var resp = c.Execute(req);

                //    var res = JsonConvert.DeserializeObject<ApiResult<List<Employee>>.Result>(resp.Content);
                //    int totalEmployee = res.Data.Count();
                //    return TaskRequest<bool>.Create("Employee", true);
                //}));

                tasks.Add(Task.Run(() =>
                {
                    int surveyId = param.Id;
                    var c = new Client(Configuration);
                    var req = new Request($"api/survey/data/participants/{surveyId}", Method.GET);

                    var resp = c.Execute(req);

                    var res = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(resp.Content);
                    participants = res.Data;
                    return TaskRequest<bool>.Create("Participants", true);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                var currDate = DateTime.Now;
                var allDate = GetDates(currDate.Year, currDate.Month);

                //var groupByDate = record.GroupBy(k => new { DateSurvey = k.CreateDate.ToLocalTime().Date }).Select(u => new { DateSurvey = u.Key.DateSurvey, CountFill = u.Count()}).ToList();

                var groupByDate = record.GroupBy(k => new { DateSurvey = k.CreateDate.ToLocalTime().Date, ID = k.UserID }).Select(x => x.OrderByDescending(y => y.CreateDate).First()).GroupBy(r => r.CreateDate.ToLocalTime().Date).Select(u => new { DateSurvey = u.Key, CountFill = u.Count() }).ToList();


                if (groupByDate.Count() == 0)
                {
                    throw new Exception("No data");
                }

                rowData.Add("Tanggal", "Partisipan survey");
                foreach (var d in allDate)
                {
                    rowData.Add("D" + d.ToString("dd"), participants.Count().ToString());
                }
                grid.Add(rowData);
                //GridSummary.Add(rowData);

                rowData = new Dictionary<string, string>();
                rowData.Add("Tanggal", "Partisipan mengisi");
                foreach (var d in allDate)
                {

                    var temp = groupByDate.Where(gg => gg.DateSurvey.ToLocalTime().Date == d.ToLocalTime().Date).FirstOrDefault();
                    if (temp == null)
                    {
                        rowData.Add("D" + d.ToString("dd"), "0");
                    }
                    else
                    {
                        rowData.Add("D" + d.ToString("dd"), temp.CountFill.ToString());
                    }
                }
                grid.Add(rowData);
                //GridSummary.Add(rowData);

                rowData = new Dictionary<string, string>();
                rowData.Add("Tanggal", "Prosentase");
                foreach (var d in allDate)
                {

                    var temp = groupByDate.Where(gg => gg.DateSurvey.ToLocalTime().Date == d.ToLocalTime().Date).FirstOrDefault();
                    if (temp == null)
                    {
                        rowData.Add("D" + d.ToString("dd"), "0");
                    }
                    else
                    {
                        double persen = temp.CountFill / Convert.ToDouble(participants.Count());
                        rowData.Add("D" + d.ToString("dd"), (Math.Round(persen, 4) * 100).ToString() + "%");
                    }
                }
                grid.Add(rowData);
                //GridSummary.Add(rowData);
                //rowData.Add("Title", "Participants");
                //rowData.Add("Title", "Fill");
                //rowData.Add("Title", "Tanggal");
                dynamic resultExport = ExportSummary(grid, param.Id);
                excel = resultExport.Value.Data.Excel;
                pdf = resultExport.Value.Data.PDF;

            } catch(Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading data :\n{Format.ExceptionString(e)}");
            }
            return Ok(new { Grid = grid, Excel = excel, PDF = pdf });
            //return new ApiResult<List<Dictionary<string, string>>>(grid);
            //return ApiResult<object>.Ok(grid);
        }

        public IActionResult GetListAnswer([FromBody] dynamic param)
        {
            var answerList = new OdooSurveyResponseAnswer();
            try
            {
                string url = Configuration["Odoo:Url"];
                string domain = Configuration["Odoo:Domain"];
                //var data = GetToken(Configuration);

                //string bearerToken = data.access_token;
                //var client = new RestClient(url);
                //client.AddDefaultHeader("Authorization", string.Format("Bearer {0}", TokenOdoo));

                //var request = new RestRequest("/api/object/survey.user_input_line?domain=[('user_input_id','='," + param.Id + ")]", Method.GET);
                //var response = client.Execute(request);

                //var result = JsonConvert.DeserializeObject<ListSurveyAnswer>(response.Content);


                string sessionOddo = Session.OdooSessionID();
                List<dynamic> domains = new List<dynamic>();
                domains.Add("user_input_id");
                domains.Add("=");
                domains.Add(param.Id);
                var pa = new ParamSurveyResult();
                pa.Params.model = "survey.user_input_line";
                pa.Params.fields = null;
                var json = JsonConvert.SerializeObject(pa);

                var client = new RestClient(url);
                client.AddDefaultHeader("Content-Type", "application/json");

                var request = new RestRequest("/web/dataset/search_read", Method.POST);
                request.AddHeader("Accept", "application/json");
                request.AddCookie("session_id", sessionOddo);
                request.AddParameter("application/json", json, ParameterType.RequestBody);

                var response = client.Execute(request);
                var result = JsonConvert.DeserializeObject<OdooSurveyResponseAnswer>(response.Content);
                answerList = result;
                //return Ok(new { Data = result.Result.Records });
            } catch(Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading data :\n{Format.ExceptionString(e)}");
            }

            return Ok(new { Data = answerList.Result.Records });
        }

        public static List<DateTime> GetDates(int year, int month)
        {
            var dates = new List<DateTime>();

            // Loop from the first day of the month until we hit the next month, moving forward a day at a time
            for (var date = new DateTime(year, month, 1); date.Month == month; date = date.AddDays(1))
            {
                dates.Add(date);
            }

            return dates;
        }

        //[HttpGet]
        //[Route("ESS/Survey/ExportSummary/{id}")]
        //public IActionResult ExportSummary([FromQuery(Name = "id")] int id)
        public dynamic ExportSummary(List<Dictionary<string, string>> GridSummary, int id)
        {
            Survey survey = new Survey();
            License lic = new License();
            try
            {
                var tasks = new List<Task<TaskRequest<bool>>>();
                tasks.Add(Task.Run(() =>
                {
                    var c = new Client(Configuration);
                    var req = new Request($"api/survey/get/{id}", Method.GET);
                    req.AddJsonParameter(Session.Id());
                    var resp = c.Execute(req);

                    var res = JsonConvert.DeserializeObject<ApiResult<Survey>.Result>(resp.Content);
                    survey = res.Data;
                    return TaskRequest<bool>.Create("Survey", true);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                Stream stream = System.IO.File.OpenRead(Directory.GetCurrentDirectory() + @"\License\Aspose.Total.lic");
                lic.SetLicense(stream);
                stream.Close();

                //string dirExport = Path.Combine("E:", "FileESS");
                string dirExport = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files");

                Workbook workbook = new Workbook();

                Worksheet sheet = workbook.Worksheets[0];
                sheet.PageSetup.Orientation = PageOrientationType.Landscape;
                AutoFitterOptions oAutoFitterOptions = new AutoFitterOptions { AutoFitMergedCells = true, OnlyAuto = true };

                string[] alpha = new string[] {
                    "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
                    "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T",
                    "U", "V", "W", "X", "Y", "Z", "AA", "AB", "AC", "AD",
                    "AE", "AF", "AG", "AH", "AI"
                };
                int i = 0;
                if (GridSummary.Count() == 0)
                {
                    throw new Exception("No data to export");
                }
                foreach (KeyValuePair<string, string> o in GridSummary[0])
                {
                    //Console.WriteLine("Key: {0}, Value: {1}", o.Key, o.Value);
                    if (i < 1)
                    {
                        sheet.Cells[alpha[i] + "20"].PutValue(o.Key);
                        sheet.Cells[alpha[i] + "21"].PutValue(o.Value);
                    }
                    else
                    {
                        sheet.Cells[alpha[i] + "20"].PutValue(o.Key.Substring(1));
                        sheet.Cells[alpha[i] + "21"].PutValue(int.Parse(o.Value));
                    }
                    i++;
                }

                i = 0;
                foreach (KeyValuePair<string, string> o in GridSummary[1])
                {
                    //Console.WriteLine("Key: {0}, Value: {1}", o.Key, o.Value);
                    if (i < 1)
                    {
                        sheet.Cells[alpha[i] + "22"].PutValue(o.Value);
                    }
                    else
                    {
                        sheet.Cells[alpha[i] + "22"].PutValue(int.Parse(o.Value));
                    }
                    i++;
                }

                i = 0;
                foreach (KeyValuePair<string, string> o in GridSummary[2])
                {
                    //Console.WriteLine("Key: {0}, Value: {1}", o.Key, o.Value);
                    if (i < 1)
                    {
                        sheet.Cells[alpha[i] + "23"].PutValue(o.Value);
                    }
                    else
                    {
                        sheet.Cells[alpha[i] + "23"].PutValue(o.Value);
                        //sheet.Cells.Columns[i].Width = 100;
                    }
                    i++;
                }

                //// data category or header
                //sheet.Cells["A20"].PutValue("Tanggal");
                //sheet.Cells["B20"].PutValue("1/12/2020");
                //sheet.Cells["C20"].PutValue("2/12/2020");
                //sheet.Cells["D20"].Value = "3/12/2020";

                //// data value
                //sheet.Cells["A21"].PutValue("Participants");
                //sheet.Cells["B21"].PutValue(60);
                //sheet.Cells["C21"].PutValue(60);
                //sheet.Cells["D21"].PutValue(60);

                //sheet.Cells["A22"].PutValue("Survey filled");
                //sheet.Cells["B22"].PutValue(25);
                //sheet.Cells["C22"].PutValue(40);
                //sheet.Cells["D22"].PutValue(35);

                int indexChart = sheet.Charts.Add(ChartType.LineWithDataMarkers, 0, 0, 15, 30);
                Chart chart = sheet.Charts[indexChart];
                chart.Title.Text = "Survey";

                //chart.NSeries.Add(sheet.Name + "!B21:P22", false);
                chart.NSeries.Add(sheet.Name + "!B21:"+ alpha[GridSummary[0].Count()-1] + "22", false);
                //chart.NSeries.CategoryData = "=sheet2!$C$1:$C$3";
                //chart.NSeries.CategoryData = sheet.Name + "!B20:P20";
                //chart.NSeries.CategoryData = sheet.Name + "!B20:"+ "P20";
                chart.NSeries.CategoryData = sheet.Name + "!B20:"+ alpha[GridSummary[0].Count() - 1] + "20";
                //chart.NSeries.CategoryData = sheet.Name + "!A20:P20";
                chart.NSeries[0].Name = "=Sheet1!$A$21";
                chart.NSeries[1].Name = "=Sheet1!$A$22";
                chart.Legend.Position = LegendPositionType.Bottom;

                Range _range;
                _range = sheet.Cells.CreateRange("A20", alpha[GridSummary[0].Count() - 1] + "23");
                //_range.SetOutlineBorders(CellBorderType.Thin, Color.Black);
                //_range.SetOutlineBorder(BorderType.BottomBorder, CellBorderType.Thin, Color.Black);

                Style style = workbook.CreateStyle();
                style.Borders[BorderType.LeftBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.RightBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.TopBorder].LineStyle = CellBorderType.Thin;
                style.Borders[BorderType.BottomBorder].LineStyle = CellBorderType.Thin;
                _range.ApplyStyle(style, new StyleFlag() { Borders = true });

                PdfSaveOptions pdfSaveOptions = new PdfSaveOptions();
                pdfSaveOptions.AllColumnsInOnePagePerSheet = true;

                sheet.AutoFitRows(oAutoFitterOptions);
                sheet.AutoFitColumns(oAutoFitterOptions);

                string exportExcel = Path.Combine(dirExport, survey.Title + ".xlsx");
                string exportPdf = Path.Combine(dirExport, survey.Title + ".pdf");

                workbook.Save(exportExcel);
                workbook.Save(exportPdf, pdfSaveOptions);
                return Ok(new
                {
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success",
                    Data = new {
                        //Excel = Path.Combine(dirExport, survey.Title + ".xlsx"),
                        //PDF = Path.Combine(dirExport, survey.Title + ".pdf")
                        Excel = "/files/" + survey.Title + ".xlsx",
                        PDF = "/files/" + survey.Title + ".pdf"
                    }
                });
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading data :\n{Format.ExceptionString(e)}");
            }
        }

        public IActionResult ExportSummaryx()
        {
            License lic = new License();
            try
            {
                Stream stream = System.IO.File.OpenRead(Directory.GetCurrentDirectory() + @"\License\Aspose.Total.lic");
                lic.SetLicense(stream);
                stream.Close();

                //string dirTemplate = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "data");
                //string dirExport = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "data");
                string dirExport = Path.Combine("E:", "FileESS");

                string exportExcel = Path.Combine(dirExport, "survey.xlsx");
                string exportPdf = Path.Combine(dirExport, "survey.pdf");

                Workbook workbook = new Workbook();

                Worksheet chartSheet = workbook.Worksheets[0];
                AutoFitterOptions oAutoFitterOptions = new AutoFitterOptions { AutoFitMergedCells = true, OnlyAuto = true };

                chartSheet.PageSetup.Orientation = PageOrientationType.Landscape;

                // data value
                Worksheet dataSheet = workbook.Worksheets[workbook.Worksheets.Add()];
                dataSheet.Cells["A1"].PutValue(40);
                dataSheet.Cells["A2"].PutValue(40);
                dataSheet.Cells["A3"].PutValue(40);

                dataSheet.Cells["B1"].PutValue(25);
                dataSheet.Cells["B2"].PutValue(30);
                dataSheet.Cells["B3"].PutValue(35);

                // data category
                dataSheet.Cells["C1"].PutValue("1/12/2020");
                dataSheet.Cells["C2"].PutValue("2/12/2020");
                dataSheet.Cells["C3"].Value = "3/12/2020";

                dataSheet.PageSetup.Orientation = PageOrientationType.Landscape;
                // create chart
                int indexChart = chartSheet.Charts.Add(ChartType.LineWithDataMarkers, 0, 0, 15, 8);
                Chart chart = chartSheet.Charts[indexChart];
                chart.Title.Text = "Survey";

                chart.NSeries.Add(dataSheet.Name + "!A1:B3", true);
                //chart.NSeries.CategoryData = "=sheet2!$C$1:$C$3";
                chart.NSeries.CategoryData = dataSheet.Name + "!C1:C3";
                chart.NSeries[0].Name = "Participants";
                chart.NSeries[1].Name = "Survey filled";
                chart.Legend.Position = LegendPositionType.Bottom;                

                workbook.Save(exportExcel);
                PdfSaveOptions pdfSaveOptions = new PdfSaveOptions() {
                    AllColumnsInOnePagePerSheet = true
                };
                chartSheet.AutoFitRows(oAutoFitterOptions);
                chartSheet.AutoFitColumns(oAutoFitterOptions);
                workbook.Save(exportPdf, pdfSaveOptions);
                //workbook.Save(Path.Combine(dirExport, "output.html"), SaveFormat.Html);
                return Ok(new
                {
                    StatusCode = HttpStatusCode.OK,
                    Message = "Success"
                });
            } catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading data :\n{Format.ExceptionString(e)}");
            }

        }

        public IActionResult ExportSurveyx() {
            Aspose.Cells.License lic = new Aspose.Cells.License();
            try
            {
                // Get the license file into stream
                
                System.IO.Stream stream = System.IO.File.OpenRead(Directory.GetCurrentDirectory() + @"\License\Aspose.Total.lic");

                // Set the License stream
                
                lic.SetLicense(stream);

                // Close the stream

                stream.Close();

                // Set the fonts directory

                //CellsHelper.FontDir = MapPath("~") + @"\Fonts";
                string dirTemplate = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "data");
                string dirExport = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "data");

                //Open the template file
                string templateExcel = dirTemplate + @"\template.xlsx";
                string templatePdf = dirTemplate + @"\template.pdf";
                //Workbook workbook = new Workbook(templateExcel);
                Workbook workbook = new Workbook();
                Worksheet worksheet = workbook.Worksheets[0];

                // add sample values to cells
                worksheet.Cells["A1"].PutValue(50);
                worksheet.Cells["A2"].PutValue(100);
                worksheet.Cells["A3"].PutValue(150);
                worksheet.Cells["B1"].PutValue(4);
                worksheet.Cells["B2"].PutValue(20);
                worksheet.Cells["B3"].PutValue(50);

                Worksheet chartSheet = workbook.Worksheets[workbook.Worksheets.Add()];

                // add a chart to the worksheet
                //int chartIndex = worksheet.Charts.Add(Aspose.Cells.Charts.ChartType.Line, 0, 0, 15, 5);
                int chartIndex = chartSheet.Charts.Add(ChartType.Line, 0, 0, 15, 5);

                // access the instance of the newly added chart
                Chart chart = chartSheet.Charts[chartIndex];

                chart.Title.Text = "Survey";

                // add chart data source from "A1" to "B3"
                chart.NSeries.Add(worksheet.Name + "!A1:B3", true);
                // manipulate data
                Cells cells = worksheet.Cells;
                cells["A20"].Value = "Hello Anye";
                cells["A21"].Value = "Hello Dinda";
                cells["A22"].Value = "Hello Nadia";

                PdfSaveOptions pdfSaveOptions = new PdfSaveOptions();

                // Set the image type to other format instead of using the default image type, that is, EMF

                //pdfSaveOptions.ImageType = System.Drawing.Imaging.ImageFormat.Png;

                // Save the PDF file
                //workbook.Save(dirExport + @"\dest.pdf", pdfSaveOptions);
                workbook.Save(templatePdf, pdfSaveOptions);

                // Save the XLSX file
                //workbook.Save(dirExport + @"\dest.xlsx");
                workbook.Save(templateExcel);

                return Ok("Success");
            } catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading data :\n{Format.ExceptionString(e)}");
            }
        }

        public bool IsList(object o)
        {
            if (o == null) return false;
            //bool ck = o is IList &&
            //       o.GetType().IsGenericType &&
            //       o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
            bool ck = o is IList;
            return ck;
        }

        public bool IsObject(object o)
        {
            if (o == null) return false;
            //bool ck = o is IDictionary &&
            //       o.GetType().IsGenericType &&
            //       o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
            bool ck = o is Object;
            return ck;
        }

        public bool IsDictionary(object o)
        {
            if (o == null) return false;
            //bool ck = o is IDictionary &&
            //       o.GetType().IsGenericType &&
            //       o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));
            bool ck = o is IDictionary;
            return ck;
        }
    }

    public class SurveyReport
    {
        public string EmployeeID { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public Dictionary<DateTime, double?> DateScore { get; set; } = new Dictionary<DateTime, double?>();
    }

    public class GridSurveyReport
    {
        public List<Dictionary<string, string>> Result { get; set; } = new List<Dictionary<string, string>>();
    }
}