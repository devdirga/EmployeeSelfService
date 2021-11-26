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
namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class GroupController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public GroupController(IConfiguration conf, IUserSession session)
        {
            Configuration = conf;
            Session = session;
        }

        //[Authorize]
        //[PageCodeAuthorize("GroupManagement", ActionGrant.Read)]
        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Administration", URL=""},
                new Breadcrumb{Title="Group ", URL=""}
            };
            ViewBag.Title = "Group Management";
            ViewBag.Icon = "mdi mdi-settings";
            return View("~/Areas/ESS/Views/Administration/Group.cshtml");
        }
            
        public async Task<IActionResult> GetData()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/administration/group/get/data", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Group>>.Result>(response.Content);
            return new ApiResult<List<Group>>(result);
            
        }

        public async Task<IActionResult> GetDetail(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/administration/group/getdetail/{token}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Group>>.Result>(response.Content);
            return new ApiResult<List<Group>>(result);
        }

        
        public async Task<IActionResult> Save([FromBody] Group param)
        {
            // var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/administration/group/save", Method.POST);
            param.UpdateBy = Session.DisplayName();
            request.AddJsonParameter(param);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<Group>.Result>(response.Content);
            return new ApiResult<Group>(result);
        }
    }
}