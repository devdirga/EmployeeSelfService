using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Model.Payroll;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Net;
using System.Net.Mail;

namespace KANO.Api.Payroll.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayrollController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private LoanMailTemplate mailTemplate;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public PayrollController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            mailTemplate = new LoanMailTemplate(Mongo, Configuration);
        }

        [HttpGet("payslip/{employeeID}")]
        public async Task<IActionResult> Get(string employeeID)
        {
            var payslips = new List<PaySlip>();
            try
            {
                var adapter = new PayRollAdapter(Configuration);
                payslips = adapter.GetPaySlips(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get payslips for employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<PaySlip>>.Ok(payslips);
        }

        [HttpGet("getemployee/{employeeID}")]
        public async Task<IActionResult> GetEmployee(string employeeID)
        {
            var employee = new Employee();
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                employee = adapter.Get(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get payslips for employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<Employee>.Ok(employee);
        }

        [HttpGet("getlatest/{employeeID}")]
        public async Task<IActionResult> GetLatestPaySlip(string employeeID)
        {
            var employee = new PaySlip();
            try
            {
                var adapter = new PayRollAdapter(Configuration);
                employee = adapter.GetLatestPayslip(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get payslips for employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<PaySlip>.Ok(employee);
        }

        [HttpGet("payslip/download/{employeeID}/{processID}")]
        public IActionResult DownloadPayslip(string employeeID, string processID)
        {            
            // Download the data
            try
            {
                var adapter = new PayRollAdapter(Configuration);
                var payslip = adapter.GetPaySlips(employeeID).Find(x=>x.ProcessID == (processID));
                
                Console.WriteLine($"{DateTime.Now} Download payslip {employeeID} {processID}");
                if (payslip != null && payslip.Accessible)
                {
                    var bytes = payslip.Download();
                    Console.WriteLine($">>> Download File : {payslip.Filepath}");
                    return File(bytes, "application/force-download", Path.GetFileName(payslip.Filepath));
                }
                else
                {
                    payslip = new PaySlip();
                    Console.WriteLine($">>> Generating new report : {processID}");
                    payslip.Filepath = adapter.GenerateReport(employeeID, processID);
                    if (payslip.Accessible) {
                        Console.WriteLine($">>> Download file : {payslip.Filepath}");
                        var bytes = payslip.Download();
                        return File(bytes, "application/force-download", Path.GetFileName(payslip.Filepath));
                    }

                    throw new Exception($">>> Unable to find file path on database : {payslip.Filepath}");
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error while downloading payslip : {Format.ExceptionString(e)}");
                return ApiResult.Error(e);
            }
        }

        [HttpGet("loanrequest/{employeeID}/{documentID}")]
        public IActionResult GetLoanRequest(string employeeID, string documentID)
        {
            var response = new LoanRequest();
            return ApiResult<LoanRequest>.Ok(response);
        }

        [HttpGet("loanrequests/{employeeID}")]
        public IActionResult GetLoanRequests(string employeeID)
        {
            var tasks = new List<Task<List<LoanRequest>>>();

            // Get Document from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new PayRollAdapter(Configuration);
                return adapter.GetLoanRequest(employeeID);
            }));


            // Get Document Update Request from Local ESS
            tasks.Add(Task.Run(() =>
            {
                return DB.GetCollection<LoanRequest>().Find(x => x.EmployeeID == employeeID).ToList();
            }));


            // Running tasks parallel
            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while loading document requets :\n{e.Message}");
            }

            var results = new List<LoanRequest>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var result in t.Result)
                {
                    results.AddRange(result);
                }
            }


            return ApiResult<List<LoanRequest>>.Ok(results);
        }

        [HttpPost("loanrequest/save")]
        public IActionResult SaveLoanRequest([FromBody] LoanRequest loanRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loanRequest.Id))
                {
                    loanRequest.RequestDate = DateTime.Now;
                }

                DB.Save(loanRequest);

            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving document request :\n{e.Message}");
            }

            return ApiResult<object>.Ok(loanRequest, "Loan request has been saved successfully");
        }

        

        [HttpGet("loanrequest/delete/{requestID}")]
        public IActionResult DeleteLoanRequest(string requestID)
        {
            try
            {
                DB.Delete(new LoanRequest { Id = requestID });
            }
            catch (Exception e)
            {
                return ApiResult<List<LoanRequest>>.Error(HttpStatusCode.BadRequest, $"Error saving document request :\n{e.Message}");
            }

            return ApiResult<LoanRequest>.Ok("Document request has been deleted successfully ");
        }

        [HttpGet("loanrequest/gettemplate")]
        public async Task<IActionResult> GetTemplate()
        {
            mailTemplate = DB.GetCollection<LoanMailTemplate>().Find(x => x.Id == "LoanTemplate").FirstOrDefault();
            return ApiResult<LoanMailTemplate>.Ok(mailTemplate);
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }

        [HttpGet("loanrequest/getemployee")]
        public IActionResult GetIdentification(string employeeID)
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var data = adapter.GetIdentifications(employeeID);
                return ApiResult<List<Identification>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee employments :\n{Format.ExceptionString(e)}");
            }
        }
    }
}
