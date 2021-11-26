using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Auth;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using static KANO.Core.Lib.Auth.AuthPrincipal;


namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class UserController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public UserController(IConfiguration conf, IUserSession session)
        {
            Configuration = conf;
            Session = session;
        }


        //Authorize: Read
        //[Authorize]
        //[PageCodeAuthorize("UserManagement", ActionGrant.Read)]
        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Administration", URL=""},
                new Breadcrumb{Title="User ", URL=""}
            };
            ViewBag.Title = "User Management";
            ViewBag.Icon = "mdi mdi-settings";
            return View("~/Areas/ESS/Views/Administration/User.cshtml");
        }

        [HttpPost]       
        public async Task<IActionResult> GetData([FromBody] KendoGrid param)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/administration/user/get/data", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<User>>.Result>(response.Content);
            return new ApiResult<List<User>>(result);
            
        }       

        [HttpGet]
        public async Task<IActionResult> GetDetail( string Username)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/administration/user/get/detail/{Username}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<User>>.Result>(response.Content);
            return new ApiResult<List<User>>(result);
        }

        
        [HttpPost]
        public async Task<IActionResult> Save([FromBody]Core.Model.User param)
        {
            // var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/administration/user/save", Method.POST);
            
            param.UpdateBy = Session.DisplayName();

            request.AddJsonParameter(param);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<User>.Result>(response.Content);
            return new ApiResult<User>(result);
        }
    }
}