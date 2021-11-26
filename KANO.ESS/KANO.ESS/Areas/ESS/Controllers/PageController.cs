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
    public class PageController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public PageController(IConfiguration conf, IUserSession session)
        {
            Configuration = conf;
            Session = session;
        }

        //[Authorize]
        //[PageCodeAuthorize("PageManagement", ActionGrant.Read)]
        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Administration", URL=""},
                new Breadcrumb{Title="Page", URL=""}
            };
            ViewBag.Title = "Page Management";
            ViewBag.Icon = "mdi mdi-settings";
            return View("~/Areas/ESS/Views/Administration/Page.cshtml");
        }

        [HttpGet]        
        public async Task<IActionResult> GetData()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/administration/page/get", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Page>>.Result>(response.Content);
            return new ApiResult<List<Page>>(result);
            
        }

        [HttpGet]        
        public async Task<IActionResult> FindTiered()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/administration/page/get/tiered", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<TieredPageManagement>>.Result>(response.Content);
            return new ApiResult<List<TieredPageManagement>>(result);
            
        }

        [HttpGet]        
        public async Task<IActionResult> FindByUserLogin()
        {
            var id = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/administration/page/user/{id}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<MenuPage>>.Result>(response.Content);
            return new ApiResult<List<MenuPage>>(result);
        }
        
        [HttpGet]
        public async Task<IActionResult> GetDetail(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/administration/page/detail/{token}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Page>>.Result>(response.Content);
            return new ApiResult<List<Page>>(result);
        }

        [HttpPost]
        public async Task<IActionResult> Save( [FromBody]  Page param)
        {

            param.UpdateBy = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/administration/page/save", Method.POST);
            request.AddJsonParameter(param);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<Page>.Result>(response.Content);
            return new ApiResult<Page>(result);
        }
    }
}