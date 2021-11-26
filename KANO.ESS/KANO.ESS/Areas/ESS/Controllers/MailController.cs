using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Model;
using Microsoft.AspNetCore.Mvc;

namespace KANO.ESS.Areas.ESS.Controllers
{
    public class MailController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS", URL=""},
                new Breadcrumb{Title="Mail", URL=""},
            };
            ViewBag.Title = "Mail";
            ViewBag.Icon = "mdi mdi-email";
            return View();
        }
    }
}