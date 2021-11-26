
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using RestSharp;
using Microsoft.Extensions.Configuration;
using System.Net;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System;
using System.IO;
using KANO.Core.Service;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class HomeController : Controller
    {
        private IConfiguration Configuration;

        public HomeController(IMongoManager mongo, IConfiguration config)
        {
            Configuration = config;
        }

        public IActionResult Index()
        {
            //var userRole = User.Identities.LastOrDefault().Claims.ToList().FirstOrDefault(x => x.Type == "ROLE").Value.ToUpper();
            //ViewBag.Role = userRole;

            var email = User.Identity.Name;
            //var keyword = "";
            //if (data != null)
            //{
            //    keyword = data.TenantReference;
            //} 
            //if (keyword == null || string.IsNullOrEmpty(keyword))
            //{
            //    return View("~/Areas/ESS/Views/MemberArea/Index.cshtml");
            //}
            //else
            //{
                return View();
            //}
        }

    }
}