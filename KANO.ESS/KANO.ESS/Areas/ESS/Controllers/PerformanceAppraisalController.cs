using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
//using KANO.ESS.Model;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class PerformanceAppraisalController : Controller
    {
        private IConfiguration Configuration;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public PerformanceAppraisalController(IConfiguration conf)
        {
            Configuration = conf;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Performance Appraisal", URL=""}
            };
            ViewBag.Title = "Performance Appraisal";
            ViewBag.Icon = "mdi mdi-speedometer";
            return View();
        }

        public ICollection<PerformaceAppraisalRequest> GetPerformaceAppraisals()
        {
            var data = new List<PerformaceAppraisalRequest>
            {
                new PerformaceAppraisalRequest{Id = "PA-001", EmployeeID = "1001", AppraisalDate = DateTime.ParseExact("02/12/2019", "dd/MM/yyyy", null), Status = "Submit"},
                new PerformaceAppraisalRequest{Id = "PA-002", EmployeeID = "1002", AppraisalDate = DateTime.ParseExact("06/12/2019", "dd/MM/yyyy", null), Status = "Approved"},
                new PerformaceAppraisalRequest{Id = "PA-003", EmployeeID = "1001", AppraisalDate = DateTime.ParseExact("07/12/2019", "dd/MM/yyyy", null), Status = "Approved"},
                new PerformaceAppraisalRequest{Id = "PA-004", EmployeeID = "1001", AppraisalDate = DateTime.ParseExact("08/12/2019", "dd/MM/yyyy", null), Status = "Submit"},
            };
            return data;
        }
    }
}