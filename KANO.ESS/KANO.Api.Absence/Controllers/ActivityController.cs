using System;
using System.Linq;
using KANO.Core.Model;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Collections.Generic;
using KANO.Core.Service.Odoo;
using System.Threading.Tasks;
using RestSharp;
using System.Collections;

namespace KANO.Api.Absence.Controllers
{
    [Route("api/absence/[controller]")]
    [ApiController]
    public class ActivityController : ControllerBase
    {
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase DB;
        private readonly IConfiguration Configuration;
        private readonly ActivityType _activityType;
        private readonly ActivityLog _activityLog;
        private readonly User _user;
        private readonly Location _location;
        private readonly Survey _survey;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public ActivityController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _activityType = new ActivityType(DB, Configuration);
            _activityLog = new ActivityLog(DB, Configuration);
            _user = new User(DB, Configuration);
            _location = new Location(DB, Configuration);
            _survey = new Survey(DB, Configuration);
        }

        [Authorize]
        [HttpGet("log/list")]
        public IActionResult GetActivityLogList()
        {
            try {
                ObjectId entityID = String.IsNullOrEmpty(Request.Query["entityID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["entityID"].ToString());
                int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
                int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
                string userID = Request.Query["userID"];
                ObjectId activityTypeID = String.IsNullOrEmpty(Request.Query["activityTypeID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["activityTypeID"].ToString());
                ObjectId locationID = String.IsNullOrEmpty(Request.Query["locationID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["locationID"].ToString());
                DateTime startDate = String.IsNullOrEmpty(Request.Query["startDate"]) ? DateTime.Now : Convert.ToDateTime(Request.Query["startDate"].ToString());
                DateTime endDate = String.IsNullOrEmpty(Request.Query["endDate"]) ? DateTime.Now : Convert.ToDateTime(Request.Query["endDate"].ToString());
                string keyword = Request.Query["search"].ToString();
                var result = _activityLog.Get(entityID, skip, limit, userID, activityTypeID, locationID, startDate, endDate, keyword);
                List<ActivityLogMap> activityLogResult = new List<ActivityLogMap>();
                foreach (ActivityLog log in result)
                    activityLogResult.Add(MapFromLog(log));
                return Ok(new { data = activityLogResult, message = String.Empty, success = true });
            }
            catch (Exception e) { return Ok(new { data = new ActivityLog(), message = Format.ExceptionString(e), success = false }); }
        }

        [Authorize]
        [HttpGet("type/list")]
        public IActionResult GetActivityTypeList()
        {
            try
            {
                string entityID = Request.Query["entityID"].ToString();
                int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
                int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
                string keyword = Request.Query["search"].ToString();

                var result = _activityType.Get(entityID, skip, limit, keyword).ToList();


                return Ok(new
                {
                    data = result,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = new ActivityType(),
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpGet("type/{ActivityTypeID}")]
        public IActionResult GetTypeByID(string ActivityTypeID)
        {
            try
            {
                var result = _activityType.GetByID(ActivityTypeID);
                return Ok(new
                {
                    data = result,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        public string AuthenticateOdoo(IConfiguration config)
        {
            var sessionID = "";

            string url = config["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");

            string username = config["Odoo:Username"];
            if (string.IsNullOrWhiteSpace(username))
                throw new Exception("Odoo:Username is not set in the configuration!");

            var password = config["Odoo:Password"];
            if (string.IsNullOrWhiteSpace(password))
                throw new Exception("Odoo:Password is not set in the configuration!");

            var db = config["Odoo:DB"];
            if (string.IsNullOrWhiteSpace(db))
                throw new Exception("Odoo:DB is not set in the configuration!");

            //Console.WriteLine("Login Odoo:"+ url);
            //Console.WriteLine("Username:"+ username);
            //Console.WriteLine("Password:"+ password);
            //Console.WriteLine("DB:"+ db);

            var c = new RestClient(url);
            c.AddDefaultHeader("Content-Type", "application/json");

            var param = new OdooRequest();
            param.Params.Add("db", db);
            param.Params.Add("login", username);
            param.Params.Add("password", password);

            var r = new RestRequest("/web/session/authenticate", Method.POST);
            r.AddHeader("Accept", "application/json");
            r.Parameters.Clear();
            r.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(param), ParameterType.RequestBody);

            try
            {
                var response = c.Execute(r);
                if (response.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var cookies = response.Cookies;
                    var cookie = cookies.Where(n => n.Name == "session_id").FirstOrDefault();
                    dynamic result = JsonConvert.DeserializeObject(response.Content);
                    Console.WriteLine("Login result:" + result);

                    if (cookie != null)
                    {
                        sessionID = cookie.Value;
                    } else
                    {
                        throw new Exception("Cookie null");
                    }
                } else
                {
                    throw new Exception("Failed login odooo");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error Login Odoo", e.Message);
                throw;
            }

            return sessionID;
        }

        [Authorize]
        [HttpGet("survey")]
        public IActionResult GetSurvey()
        {
            try
            {
                string sessionOddo = "";
                sessionOddo = AuthenticateOdoo(Configuration);
                Console.WriteLine("SessionOdoo:"+ sessionOddo);

                string userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                List<Survey> surveys = new List<Survey>();

                // ambil data answer survey
                var url = Configuration["Odoo:Url"];
                if (string.IsNullOrWhiteSpace(url))
                    throw new Exception("Odoo:Url is not set in the configuration!");

                var pa = new ParamSurveyResult();
                pa.Params.domain.Add("&");
                pa.Params.domain.Add("&");
                pa.Params.domain.Add("&");
                pa.Params.domain.Add(new ArrayList() { "user_id", "=", userid });
                pa.Params.domain.Add(new ArrayList() { "state", "=", "done" });
                pa.Params.domain.Add(new ArrayList() { "create_date", ">=", DateTime.Now.Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") });
                pa.Params.domain.Add(new ArrayList() { "create_date", "<=", DateTime.Now.AddDays(1).Date.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") });
                var json = JsonConvert.SerializeObject(pa);
                OdooSurveyResponse resJson = new OdooSurveyResponse();

                var client = new RestClient(url);
                client.AddDefaultHeader("Content-Type", "application/json");

                var request = new RestRequest("/web/dataset/search_read", Method.POST);
                request.AddHeader("Accept", "application/json");
                request.AddCookie("session_id", sessionOddo);
                request.AddParameter("application/json", json, ParameterType.RequestBody);

                IRestResponse response = client.Execute(request);
                //Console.WriteLine(response.Content);
                resJson = JsonConvert.DeserializeObject<OdooSurveyResponse>(response.Content);
                // ------------------ //

                string domain = Configuration["Odoo:Domain"];
                Uri baseUri = new Uri(domain);

                var result = _survey.GetWithFilter(userid);
                //Console.WriteLine("Data Survey:" + result.ToList().ToJson());
                List<SurveyResult> surveyResult = new List<SurveyResult>();
                int i = 0;
                foreach (Survey s in result)
                {
                    // Klo kosong answer baru ngisi data survey
                    Uri myUri = new Uri(baseUri, s.SurveyUrl.AbsolutePath + "?userid=" + userid);
                    s.SurveyUrl = myUri;
                    if (resJson.Result.Records.FindAll(r=>r.SurveyID[0].ToString() == s.OdooID).Count()==0)
                        surveyResult.Add(MapFromSurvey(s));

                    i++;
                }
                return Ok(new
                {
                    data = surveyResult,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpGet("type/surveyx")]
        public IActionResult GetSurveyx()
        {
            string sessionOddo = AuthenticateOdoo(Configuration);
            string userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            List<Survey> result = new List<Survey>();

            var url = Configuration["Odoo:Url"];
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("Odoo:Url is not set in the configuration!");

            try
            {
                var pa = new ParamSurveyResult();
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
                    result = DB.GetCollection<Survey>().Find(d => d.IsRequired == true && d.Participants.Contains(userid)).ToList();

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

                List<SurveyRequired> requireds = new List<SurveyRequired>();
                foreach(var n in result)
                {
                    requireds.Add(
                        new SurveyRequired
                        {
                            Id = new ObjectId(n.Id),
                            Title = n.Title,
                            SurveyUrl = n.SurveyUrl
                        }
                    );
                }

                return Ok(new
                {
                    data = requireds,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        public ActivityLogMap MapFromLog(ActivityLog activityLog)
        {
            User user = _user.GetEmployeeUser(activityLog.UserID);
            Location location = _location.GetByCode(activityLog.LocationID);
            ActivityType activityType = _activityType.GetByID(activityLog.ActivityTypeID.ToString());
            return new ActivityLogMap
            {
                Id = activityLog.Id.ToString(),
                EntityID = activityLog.EntityID.ToString(),
                ActivityType = new ActivityTypeMap
                {
                    ActivityTypeId = activityType.Id.ToString(),
                    ActivityTypeName = activityType.Name
                },
                Location = new LocationMap
                {
                    LocationID = location.Id.ToString(),
                    LocationName = location.Name
                },
                User = new UserMap
                {
                    Email = user.Email,
                    FirstName = user.FullName,
                    LastName = user.FullName,
                    UserId = user.Username,
                    Username = user.Username
                },
                CreatedBy = activityLog.CreatedBy.ToString(),
                CreatedDate = (DateTime)activityLog.CreatedDate,
                DateTime = (DateTime)activityLog.DateTime,
                Latitude = activityLog.Latitude,
                Longitude = activityLog.Longitude
            };
        }

        public SurveyResult MapFromSurvey(Survey survey)
        {
            return new SurveyResult
            {
                Id = survey.Id,
                Title = survey.Title,
                Description = survey.Description,
                SurveyUrl = survey.SurveyUrl
            };
        }

        [Authorize]
        [HttpGet("log/mlist")]
        public IActionResult MGetActivityLogList()
        {
            try {
                ObjectId entityID = String.IsNullOrEmpty(Request.Query["entityID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["entityID"].ToString());
                int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
                int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
                string userID = Request.Query["userID"];
                ObjectId activityTypeID = String.IsNullOrEmpty(Request.Query["activityTypeID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["activityTypeID"].ToString());
                ObjectId locationID = String.IsNullOrEmpty(Request.Query["locationID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["locationID"].ToString());
                DateTime startDate = String.IsNullOrEmpty(Request.Query["startDate"]) ? DateTime.Now : Convert.ToDateTime(Request.Query["startDate"].ToString());
                DateTime endDate = String.IsNullOrEmpty(Request.Query["endDate"]) ? DateTime.Now : Convert.ToDateTime(Request.Query["endDate"].ToString());
                string keyword = Request.Query["search"].ToString();
                var result = _activityLog.MGet(entityID, skip, limit, userID, activityTypeID, locationID, startDate, endDate, keyword);
                List<ActivityLogMap> activityLogResult = new List<ActivityLogMap>();
                foreach (ActivityLog log in result)
                    activityLogResult.Add(MapFromLog(log));
                return Ok(new { data = activityLogResult.OrderByDescending(a => a.DateTime).ToList(), message = String.Empty, success = true});
            }
            catch (Exception e)
            {
                return Ok(new { data = new ActivityLog(), message = Format.ExceptionString(e), success = false});
            }
        }
    }

    public class SurveyRequired
    {
        public ObjectId Id { get; set; }
        public string Title { get; set; }
        public Uri SurveyUrl { get; set; }
    }
}