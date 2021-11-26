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
using MongoDB.Bson;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class MobileAttendanceController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;

        public MobileAttendanceController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Mobile Attendance"},
                new Breadcrumb{Title="Dashboard"}
            };
            ViewBag.Title = "Dashboard";
            return View();
        }

        public IActionResult ManageLocation()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Mobile Attendance"},
                new Breadcrumb{Title="Location"}
            };
            ViewBag.Title = "Location Management";
            return View();
        }

        public IActionResult ManageEvent()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Mobile Attendance"},
                new Breadcrumb{Title="Event"}
            };
            ViewBag.Title = "Event Management";
            return View();
        }

        public IActionResult Members()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Mobile Attendance"},
                new Breadcrumb{Title="Member"}
            };
            ViewBag.Title = "Member";
            return View();
        }

        public async Task<IActionResult> Preketek()
        {
            var data = new List<GetUserManagements>()
            {
                new GetUserManagements{EmployeeName = "agus@tps.co.id", UserLogin = "Agus Setiawan", UserGroup = "Employee", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "aris@tps.co.id", UserLogin = "Aris Saputro", UserGroup = "Employee", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "ditok@tps.co.id", UserLogin = "Ditok Andri", UserGroup = "Administrator", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "123456", UserLogin = "Ferry Iswono", UserGroup = "HR Admin", Authentication = "ESS Portal" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
            };
            return ApiResult<List<GetUserManagements>>.Ok(data);
        }

        [HttpPost]
        [Route("ESS/MobileAttendance/GetUser")]
        public async Task<IActionResult> GetData()
        {
            var data = new List<GetUserManagements>()
            {
                new GetUserManagements{EmployeeName = "agus@tps.co.id", UserLogin = "Agus Setiawan", UserGroup = "Employee", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "aris@tps.co.id", UserLogin = "Aris Saputro", UserGroup = "Employee", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "ditok@tps.co.id", UserLogin = "Ditok Andri", UserGroup = "Administrator", Authentication = "LDAP" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
                new GetUserManagements{EmployeeName = "123456", UserLogin = "Ferry Iswono", UserGroup = "HR Admin", Authentication = "ESS Portal" , LatestConnection = DateTime.ParseExact("10/12/2019", "dd/MM/yyyy", null)},
            };
            return ApiResult<List<GetUserManagements>>.Ok(data);

            // var client = new Client(Configuration);
            // var request = new Request($"api/administrator/getdata", Method.GET);
            // var response = client.Execute(request);

            // var result = JsonConvert.DeserializeObject<ApiResult<List<UserModel>>.Result>(response.Content);
            // return new ApiResult<List<UserModel>>(result);

        }

        [HttpPost("ESS/MobileAttendance/Member/Get")]
        public async Task<IActionResult> GetMember([FromBody] KendoGrid param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/member/get/data", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);


            var result = JsonConvert.DeserializeObject<ApiResult<List<User>>.Result>(response.Content);
            return new ApiResult<List<User>>(result);
            //var obj = JsonConvert.DeserializeObject<IEnumerable<Dictionary<string, object>>>(response.Content);

            //return Ok(obj);
        }

        [HttpPost("ESS/MobileAttendance/Event/Attendance")]
        public async Task<IActionResult> GetAttendance([FromBody] KendoFilter param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/event/attendances", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);


            var result = JsonConvert.DeserializeObject<ApiResult<List<UserEvent>>.Result>(response.Content);
            return new ApiResult<List<UserEvent>>(result);
        }

        [HttpPost("ESS/MobileAttendance/Event/Get")]
        public async Task<IActionResult> GetEvent([FromBody] KendoGrid param)
        {
            try
            {
                var client = new Client(Configuration);
                var request = new Request($"api/mobileportal/event/get", Method.POST);
                request.AddJsonParameter(param);
                var response = client.Execute(request);

                //var result = JsonConvert.DeserializeObject<ApiResult<List<Event>>.Result>(response.Content);
                //return new ApiResult<List<Event>>(result);
                return Ok(response.Content);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(e);
            }
        }

        [HttpPost("ESS/MobileAttendance/Event/employee")]
        public async Task<IActionResult> GetEventByEmployee()
        {
            try
            {
                string empID = Session.Id();
                var client = new Client(Configuration);
                var request = new Request($"api/mobileportal/event/get/{empID}", Method.GET);
                var response = client.Execute(request);
                var result = JsonConvert.DeserializeObject(response.Content);
                return Ok(result);
            } catch(Exception e)
            {
                return ApiResult<object>.Error(e);
            }
        }

        [HttpPost("ESS/MobileAttendance/Event/Save")]
        public async Task<IActionResult> SaveEvent([FromBody] paramEvent param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/event/save", Method.POST);
            param.Organizer = Session.Id();
            request.AddJsonParameter(param);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
            //return Ok(response.Content);
        }

        [HttpPost("ESS/MobileAttendance/Event/Delete/{id}")]
        public async Task<IActionResult> DeleteEvent(string id)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/event/delete/{id}", Method.GET);
            request.AddJsonParameter(id);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
            //return Ok(response.Content);
        }

        public async Task<IActionResult> GetLocation([FromBody] KendoGrid param)
        {
            List<Location> location = new List<Location>();
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/location/get", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Dictionary<string, object>>>.Result>(response.Content);
            //return new ApiResult<List<Location>>(result);
            //var result = JsonConvert.DeserializeObject<Packman>(response.Content);
            //return new ApiResult<List<Location>>(response.Content);

            foreach (var item in result.Data)
            {
                //var code = item["code"] item["code"].ToString();
                location.Add(new Location
                {
                    Id = ObjectId.Parse(item["id"].ToString()),
                    Name = item["name"].ToString(),
                    Address = item["address"].ToString(),
                    //EntityID = ObjectId.Parse(item["entityID"].ToString()),
                    Latitude = Convert.ToDouble(item["latitude"].ToString()),
                    Longitude = Convert.ToDouble(item["longitude"].ToString()),
                    IsVirtual = Convert.ToBoolean(item["isVirtual"].ToString()),
                    Radius = Convert.ToDouble(item["radius"]),
                    Status = item["status"].ToString(),
                    Tags = JsonConvert.DeserializeObject<List<string>>(item["tags"].ToString()),
                    Code = item["code"] != null ? item["code"].ToString() : ""
                });
            }
            //return Ok(result);
            return Ok(location);
        }

        public async Task<IActionResult> SaveLocation([FromBody] paramLocation param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/location/save", Method.POST);
            param.CreatedBy = Session.Id();

            request.AddJsonParameter(param);
            var response = client.Execute(request);

            //var result = JsonConvert.DeserializeObject<ApiResult<List<User>>.Result>(response.Content);
            //return new ApiResult<List<User>>(result);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> SaveLocationMember([FromBody] User param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/member/save/location", Method.POST);
            param.UpdateBy = Session.Id();

            request.AddJsonParameter(param);
            var response = client.Execute(request);

            //var result = JsonConvert.DeserializeObject<ApiResult<List<User>>.Result>(response.Content);
            //return new ApiResult<List<User>>(result);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> GetActivityLogAll([FromBody] DateRange range)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/mobiledashboard/activity/all", Method.POST);

            request.AddJsonParameter(range);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject(response.Content);
            //var result = JsonConvert.DeserializeObject<ApiResult<List<ActivityLog>>.Result>(response.Content);
            //return new ApiResult<List<ActivityLog>>(result);
            return Ok(result);
        }

        public async Task<IActionResult> GetActivityLog([FromBody] DateRange range)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/mobiledashboard/activity/log", Method.POST);

            request.AddJsonParameter(range);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject(response.Content);
            //var result = JsonConvert.DeserializeObject<ApiResult<List<ActivityLog>>.Result>(response.Content);
            //return new ApiResult<List<ActivityLog>>(result);
            return Ok(result);
        }

        //[HttpPost("ESS/MobileAttendance/Activity/Log/Detail/{id}")]
        public async Task<IActionResult> GetActivityLogDetail([FromBody]ParamMbuh dt)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/mobileportal/mobiledashboard/activity/log/detail", Method.POST);

            request.AddJsonParameter(dt);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject(response.Content);
            //var result = JsonConvert.DeserializeObject<ApiResult<List<ActivityLog>>.Result>(response.Content);
            //return new ApiResult<List<ActivityLog>>(result);
            return Ok(result);
        }
    }
}