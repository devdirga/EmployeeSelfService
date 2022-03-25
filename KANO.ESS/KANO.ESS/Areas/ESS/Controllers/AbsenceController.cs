using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;


namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class AbsenceController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly String Api = "api/absence/";
        private readonly String BearerAuth = "Bearer ";
        private readonly String ApiUser = "api/absence/user/";
        private readonly String ApiEntity = "api/absence/entity/";
        private readonly String ApiActivity = "api/absence/activity/";
        public AbsenceController(IConfiguration config)
        {
            Configuration = config;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Absence"}
            };
            ViewBag.Title = "Absence";
            ViewBag.Icon = "mdi mdi-bell";
            return View();
        }

        [AllowAnonymous]
        [HttpPost("api/user/login")]
        public IActionResult CreateToken()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/login", Method.POST);
            var basicAuth = "Basic ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                basicAuth = authToken;
            }
            request.Self.AddHeader("Authorization", basicAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.Content,
                    success = false
                });
            }
            var res = JsonConvert.DeserializeObject<BasicResult>(response.Content);

            if (res.Data != String.Empty)
            {
                Response.Cookies.Append("Access-Token", Convert.ToString(res.Data), new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });
            }
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = res.Success
            });
        }

        [AllowAnonymous]
        [HttpPost("api/user/me/logout")]
        public IActionResult Logout()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/me/logout", Method.POST);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            Response.Cookies.Delete("Access-Token");
            var res = JsonConvert.DeserializeObject<BasicResult>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = res.Success
            });
        }

        [AllowAnonymous]
        [HttpGet("api/user/me")]
        public IActionResult GetProfile()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/me", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<UserMobileResult>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpPut("api/user/me")]
        public IActionResult UpdateProfile([FromBody] User param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/me", Method.PUT);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.AddJsonParameter(param);
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<UserResult>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpPost("api/user/upload")]
        public async Task<IActionResult> UserUpload([FromForm] UploadForm param)
        {
            // Get Profile
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/me", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<User>.Result>(response.Content);

            // Upload file
            request = new Request($"api/absence/user/upload", Method.POST);
            if (param.FileUpload != null)
            {
                request.AddFormDataParameter("Username", res.Data.Username);
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            var responseUpload = await client.Upload(request);
            if (!responseUpload.ResponseMessage.IsSuccessStatusCode)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = responseUpload.ResponseMessage.StatusCode,
                    success = false
                });
            }
            var resUpload = JsonConvert.DeserializeObject<ApiResult<object>.Result>(responseUpload.Content);
            return Ok(new
            {
                data = resUpload.Data,
                message = resUpload.Message,
                success = string.IsNullOrEmpty(resUpload.Message)
            });

        }

        [AllowAnonymous]
        [HttpPost("api/user/register")]
        public IActionResult UserRegister([FromBody] User param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/register", Method.POST);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.AddJsonParameter(param);
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<UserResult>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/user/list")]
        public IActionResult GetUserList()
        {
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string search = Request.Query["search"].ToString();
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/list?skip={skip}&limit={limit}&search={search}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<object>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/user/locationlist")]
        public IActionResult GetLocationsByEntityID()
        {
            ObjectId entityID = String.IsNullOrEmpty(Request.Query["entityID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["entityID"].ToString());
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/locationlist?entityID={entityID}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<LocationResult>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/user/locations")]
        public IActionResult GetLocationsByUserID()
        {
            string userID = Request.Query["userID"];
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/locations?userID={userID}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<object>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/user/locations-distance")]
        public IActionResult GetLocationsDistanceByUserID()
        {
            string userID = Request.Query["userID"];
            string slatitude = Request.Query["latitude"];
            string slongitude = Request.Query["longitude"];
            var client = new Client(Configuration);
            var request = new Request($"api/absence/user/locations-distance?userID={userID}&latitude={slatitude}&longitude={slongitude}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<object>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/activity/log/list")]
        public IActionResult GetActivityLogList()
        {
            ObjectId entityID = String.IsNullOrEmpty(Request.Query["entityID"])  ? ObjectId.Empty : ObjectId.Parse(Request.Query["entityID"].ToString());
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string userID = Request.Query["userID"];
            ObjectId activityTypeID = String.IsNullOrEmpty(Request.Query["activityTypeID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["activityTypeID"].ToString());
            ObjectId locationID = String.IsNullOrEmpty(Request.Query["locationID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["locationID"].ToString());
            string startDate = String.IsNullOrEmpty(Request.Query["startDate"]) ? DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss") : Convert.ToDateTime(Request.Query["startDate"].ToString()).ToString("dd MMMM yyyy HH:mm:ss");
            string endDate = String.IsNullOrEmpty(Request.Query["endDate"]) ? DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss") : Convert.ToDateTime(Request.Query["endDate"].ToString()).ToString("dd MMMM yyyy HH:mm:ss");
            string search = Request.Query["search"].ToString();
            var client = new Client(Configuration);
            var request = new Request($"api/absence/activity/log/list?entityID={entityID}&skip={skip}&limit={limit}&userID={userID}&activityTypeID={activityTypeID}&locationID={locationID}&startDate=&endDate=&search={search}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<ActivityLogMap>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/activity/type/list")]
        public IActionResult GetActivityTypeList()
        {
            string entityID = Request.Query["entityID"].ToString();
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string search = Request.Query["search"].ToString();
            var client = new Client(Configuration);
            var request = new Request($"api/absence/activity/type/list?entityID={entityID}&skip={skip}&limit={limit}&search={search}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<ActivityTypeResult>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/activity/type/{ActivityTypeID}")]
        public IActionResult GetTypeByID(string ActivityTypeID)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/activity/type/{ActivityTypeID}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<ActivityTypeResult>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/activity/survey")]
        public IActionResult GetSurvey()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/activity/survey", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<SurveyResult>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/entity/list")]
        public IActionResult GetEntityList()
        {
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string search = Request.Query["search"].ToString();
            var client = new Client(Configuration);
            var request = new Request($"api/absence/entity/list?&skip={skip}&limit={limit}&search={search}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<EntityMap>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/entity/mlist")]
        public IActionResult MGetEntityList()
        {
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string search = Request.Query["search"].ToString();
            var client = new Client(Configuration);
            var request = new Request($"api/absence/entity/list?&skip={skip}&limit={limit}&search={search}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    Data = (object)null,
                    Message = response.StatusDescription,
                    Success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<EntityMap>>.Result>(response.Content);
            return Ok(new
            {
                Data = res.Data,
                Message = res.Message,
                Success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/entity/member/list")]
        public IActionResult GetEntityMemberList()
        {
            ObjectId entityID = String.IsNullOrEmpty(Request.Query["id"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["id"]);
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string search = Request.Query["search"].ToString();
            var client = new Client(Configuration);
            var request = new Request($"api/absence/entity/member/list?id={entityID}&skip={skip}&limit={limit}&search={search}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<EntityMemberMap>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/entity/{entityID}")]
        public IActionResult GetEntityByID(string entityID)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/entity/{entityID}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<EntityMap>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/event/list")]
        public IActionResult GetEventList()
        {
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string userID = Request.Query["userID"];
            string startDate = String.IsNullOrEmpty(Request.Query["startDate"]) ? DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss") : Convert.ToDateTime(Request.Query["startDate"].ToString()).ToString("dd MMMM yyyy HH:mm:ss");
            string endDate = String.IsNullOrEmpty(Request.Query["endDate"]) ? DateTime.Now.ToString("dd MMMM yyyy HH:mm:ss") : Convert.ToDateTime(Request.Query["endDate"].ToString()).ToString("dd MMMM yyyy HH:mm:ss");
            string search = Request.Query["search"].ToString();
            var client = new Client(Configuration);
            var request = new Request($"api/absence/event/list?skip={skip}&limit={limit}&userID={userID}&startDate=&endDate=&search={search}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<EventResult>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/event/{eventID}")]
        public IActionResult GetEventByID(string eventID)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/event/{eventID}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<EventResult>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpPost("api/event")]
        public IActionResult SaveEvent([FromBody] paramEvent param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/event", Method.POST);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.AddJsonParameter(param);
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<paramEvent>.Result>(response.Content);
            if (res.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var profile = GetProfile();
                    var profileBDoc = profile.ToBsonDocument();
                    var user = profileBDoc["Value"]["data"];
                    var mailer = new Mailer(Configuration);
                    var message = new MailMessage();
                    message.Subject = "New Event";
                    message.Body = $"Hi this is new event<br/><br/>";
                    message.To.Add(user["Email"].ToString());
                    mailer.SendMail(message);
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
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpPost("api/event/scan")]
        public IActionResult ScanEvent([FromBody] paramScanEvent param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/event/scan", Method.POST);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.AddJsonParameter(param);
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<EventResult>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpPut("api/event/{eventID}")]
        public IActionResult UpdateEvent([FromBody] paramEvent param)
        {
            var client = new Client(Configuration);
            var request = new Request("api/absence/event/{eventID}", Method.PUT);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.AddJsonParameter(param);
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<paramEvent>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/location/list")]
        public IActionResult GetlocationList()
        {
            ObjectId entityID = String.IsNullOrEmpty(Request.Query["entityID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["entityID"].ToString());
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string search = Request.Query["search"].ToString();

            var client = new Client(Configuration);
            var request = new Request($"api/absence/location/list?entityID={entityID}&skip={skip}&limit={limit}&search={search}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<LocationResult>>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpGet("api/location/{locationID}")]
        public IActionResult GetLocationByID(string locationID)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/location/{locationID}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<LocationResult>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpPost("api/absence/checkinout")]
        public IActionResult CheckInOut([FromBody] AbsenceForm param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/absence/docheckinout", Method.POST);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.AddJsonParameter(param);
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = response.StatusDescription,
                    success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return Ok(new
            {
                data = res.Data,
                message = res.Message,
                success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpPost("api/absence/inout")]
        public async Task<IActionResult> InOut([FromForm] TicketForm param)
        {
            try
            {
                Absences ticketRequest = JsonConvert.DeserializeObject<Absences>(param.JsonData);
                var req = new Request($"{Api}doinout", Method.POST);
                req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(ticketRequest));
                if (param.FileUpload != null)
                {
                    req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
                }
                var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>((await (new Client(Configuration)).Upload(req)).Content);
                return new ApiResult<object>(result);
                //var res = await (new Client(Configuration)).Upload(req);
                //var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                HttpStatusCode.InternalServerError, $"Well it is embarassing, internal server error : {e.Message}");

            }
        }

        [AllowAnonymous]
        [HttpGet("api/activity/log/lists")]
        public IActionResult GetActivityLogLists()
        {
            int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
            int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
            string userID = String.Empty;
            //GetUserFromToken
            var c = new Client(Configuration);
            var req = new Request("api/absence/user/me", Method.GET);
            var brrAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues athToken))
            {
                brrAuth = athToken;
            }
            req.Self.AddHeader("Authorization", brrAuth);
            var re = c.Execute(req);
            if (!re.IsSuccessful)
            {
                userID = String.Empty;
            }
            else
            {
                var r = JsonConvert.DeserializeObject<ApiResult<UserMobileResult>.Result>(re.Content);
                userID = r.Data.Username;
            }

            var client = new Client(Configuration);
            var request = new Request($"api/absence/activity/log/mlist?skip={skip}&limit={limit}&userID={userID}", Method.GET);
            var bearerAuth = "Bearer ";
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                bearerAuth = authToken;
            }
            request.Self.AddHeader("Authorization", bearerAuth);
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                return Ok(new
                {
                    Data = (object)null,
                    Message = response.StatusDescription,
                    Success = response.IsSuccessful
                });
            }
            var res = JsonConvert.DeserializeObject<ApiResult<List<ActivityLogMap>>.Result>(response.Content);
            return Ok(new
            {
                Data = res.Data,
                Message = res.Message,
                Success = string.IsNullOrEmpty(res.Message)
            });
        }

        [AllowAnonymous]
        [HttpPost("api/absence/doinoutdev")]
        public async Task<IActionResult> DoInOutDev([FromForm] TicketForm param)
        {
            try
            {
                Absences ticketRequest = JsonConvert.DeserializeObject<Absences>(param.JsonData);
                var req = new Request($"{Api}doinoutdev", Method.POST);
                req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(ticketRequest));
                if (param.FileUpload == null)
                {
                    return new ApiResult<object>(
                        JsonConvert.DeserializeObject<ApiResult<object>.Result>(
                            (await (new Client(Configuration)).Upload(req)).Content));
                }
                req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
                return new ApiResult<object>(
                    JsonConvert.DeserializeObject<ApiResult<object>.Result>(
                        (await (new Client(Configuration)).Upload(req)).Content));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                HttpStatusCode.InternalServerError, $"Well it is embarassing, internal server error : {e.Message}");

            }
        }

        [AllowAnonymous]
        [HttpGet("api/absence/updatedoinoutdev")]
        public IActionResult UpdateDoInOutDev()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            try
            {
                var request = new Request($"{Api}updatedoinoutdev", Method.GET, "Authorization", bearerAuth);
                var response = new Client(Configuration).Execute(
                    new Request($"{Api}updatedoinoutdev", Method.GET, "Authorization", bearerAuth));
                if (!response.IsSuccessful)
                {
                    return Ok(new
                    {
                        Data = (object)null,
                        Message = response.StatusDescription,
                        Success = response.IsSuccessful
                    });
                }
                var res = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
                return Ok(new
                {
                    Data = res.Data,
                    Message = res.Message,
                    Success = string.IsNullOrEmpty(res.Message)
                });
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                HttpStatusCode.InternalServerError, $"Well it is embarassing, internal server error : {e.Message}");
            }
        }

    }
}
