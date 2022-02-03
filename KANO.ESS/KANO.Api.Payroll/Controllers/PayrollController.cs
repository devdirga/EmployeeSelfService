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
using Microsoft.AspNetCore.Authorization;

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

        [HttpGet("loanrequest/getemployee/{employeeID}")]
        public IActionResult GetEmployeeIdentification(string employeeID)
        {
            try
            {
                return ApiResult<List<Identification>>.Ok(new EmployeeAdapter(Configuration).GetIdentifications(employeeID));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee employments :\n{Format.ExceptionString(e)}");
            }
        }
        [HttpGet("loantype")]
        public IActionResult LoanType()
        {
            var dataloan = new List<LoanType> {
                new LoanType {Id = "1", Name = LoanTypeName.Reguler.ToString(),MinimumRangePeriode = 0,MaximumRangePeriode = 24,MaximumLoan = 50000000, MinimumLimitLoan = 1500000},
                new LoanType {Id = "2", Name = LoanTypeName.UangTambahan.ToString(),MinimumRangePeriode = 0,MaximumRangePeriode = 10,MaximumLoan = 10000000, MinimumLimitLoan = 1500000},
                new LoanType {Id = "3", Name = LoanTypeName.DanaMitra.ToString(),MinimumRangePeriode = 0,MaximumRangePeriode = 120,MaximumLoan = 500000000, MinimumLimitLoan = 1500000}
            };
            return ApiResult<List<LoanType>>.Ok(dataloan);
        }
        [HttpGet("loanmethod")]
        public IActionResult LoanMethod()
        {
            return ApiResult<List<String>>.Ok(new List<String> { "Normal", "Compensation" });
        }
        [HttpGet("mloanmethod")]
        public IActionResult MLoanMethod()
        {
            var loanMethod = new List<MLoanMethod>
            {
                new MLoanMethod { Id = 1 , Name = "Normal" },
                new MLoanMethod { Id = 2 , Name = "Kompensasi"}
            };
            return ApiResult<List<MLoanMethod>>.Ok(loanMethod, loanMethod.Count);
        }
        [HttpGet("loanperiod")]
        public IActionResult LoanPeriod()
        {
            var loadPeriod = new List<LoanTypeDetail>
            {
                new LoanTypeDetail{IdLoanType = LoanTypeName.Reguler,LoanTypeName = LoanTypeName.Reguler.ToString(),PeriodeName = "Jangka 1 Tahun",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Normal.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 0,MaximumRangePeriode = 12,Interest = (decimal)0.01},
                new LoanTypeDetail{IdLoanType = LoanTypeName.Reguler,LoanTypeName = LoanTypeName.Reguler.ToString(),PeriodeName = "Jangka 2 Tahun",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Normal.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 13,MaximumRangePeriode = 24,Interest = (decimal)0.02},
                new LoanTypeDetail{IdLoanType = LoanTypeName.Reguler,LoanTypeName = LoanTypeName.Reguler.ToString(),PeriodeName = "Jangka 1 Tahun",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 0,MaximumRangePeriode = 12,Interest = (decimal)0.01},
                new LoanTypeDetail{IdLoanType = LoanTypeName.Reguler,LoanTypeName = LoanTypeName.Reguler.ToString(),PeriodeName = "Jangka 2 Tahun",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 13,MaximumRangePeriode = 24,Interest = (decimal)0.02},
                new LoanTypeDetail{IdLoanType = LoanTypeName.UangTambahan,LoanTypeName = LoanTypeName.UangTambahan.ToString(),PeriodeName = "Jangka 10 Bulan",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Normal.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 0,MaximumRangePeriode = 10,Interest = (decimal)0.01},
                new LoanTypeDetail{IdLoanType = LoanTypeName.UangTambahan,LoanTypeName = LoanTypeName.UangTambahan.ToString(),PeriodeName = "Jangka 10 Bulan",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 0,MaximumRangePeriode = 10,Interest = (decimal)0.01},
                new LoanTypeDetail{IdLoanType = LoanTypeName.DanaMitra,LoanTypeName = LoanTypeName.DanaMitra.ToString(),PeriodeName = "Jangka Waktu 0-5 Tahun",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Normal.ToString(),PeriodType = LoanPeriodType.Range,MinimumRangePeriode = 0,MaximumRangePeriode = 60,Interest = (decimal)0.0125,MinimumRangeLoanPeriode = 0,MaximumRangeLoanPeriode = 60,MaximumLoad = 100000000},
                new LoanTypeDetail{IdLoanType = LoanTypeName.DanaMitra,LoanTypeName = LoanTypeName.DanaMitra.ToString(),PeriodeName = "Jangka Waktu 6-10 Tahun",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Range,MinimumRangePeriode = 61,MaximumRangePeriode = 120,Interest = (decimal)0.0125,MinimumRangeLoanPeriode = 61,MaximumRangeLoanPeriode = 120,MaximumLoad = 500000000},
                new LoanTypeDetail{IdLoanType = LoanTypeName.DanaMitra,LoanTypeName = LoanTypeName.DanaMitra.ToString(),PeriodeName = "Jangka Waktu 0-6 Tahun",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Range,MinimumRangePeriode = 0,MaximumRangePeriode = 60,Interest = (decimal)0.0133,MinimumRangeLoanPeriode = 0,MaximumRangeLoanPeriode = 60,MaximumLoad = 100000000},
                new LoanTypeDetail{IdLoanType = LoanTypeName.DanaMitra,LoanTypeName = LoanTypeName.DanaMitra.ToString(),PeriodeName = "Jangka Waktu 6-10 Tahun",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Range,MinimumRangePeriode = 61,MaximumRangePeriode = 120,Interest = (decimal)0.0133,MinimumRangeLoanPeriode = 61,MaximumRangeLoanPeriode = 120,MaximumLoad = 500000000}
            };
            return ApiResult<List<LoanTypeDetail>>.Ok(loadPeriod);
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [Authorize]
        [HttpGet("mpayslip/{employeeID}")]
        public IActionResult MGet(string employeeID)
        {
            try
            {
                return ApiResult<List<PaySlip>>.Ok(
              new PayRollAdapter(Configuration).GetPaySlips(employeeID));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"Unable to get payslips for employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mgetemployee/{employeeID}")]
        public IActionResult MGetEmployee(string employeeID)
        {
            try
            {
                return ApiResult<Employee>.Ok(
              new EmployeeAdapter(Configuration).Get(employeeID));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"Unable to get payslips for employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mgetlatest/{employeeID}")]
        public IActionResult MGetLatestPaySlip(string employeeID)
        {
            try
            {
                return ApiResult<PaySlip>.Ok(
              new PayRollAdapter(Configuration).GetLatestPayslip(employeeID));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"Unable to get payslips for employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }
        [HttpGet("mpayslip/download/{employeeID}/{processID}")]
        public IActionResult MDownloadPayslip(string employeeID, string processID)
        {
            try
            {
                var adapter = new PayRollAdapter(Configuration);
                var payslip = adapter.GetPaySlips(employeeID).Find(x => x.ProcessID == (processID));
                if (payslip != null && payslip.Accessible)
                {
                    byte[] bytes = payslip.Download();
                    return File(bytes, "application/force-download", Path.GetFileName(payslip.Filepath));
                }
                else
                {
                    payslip = new PaySlip { Filepath = adapter.GenerateReport(employeeID, processID) };
                    if (payslip.Accessible)
                    {
                        return File(payslip.Download(), "application/force-download", Path.GetFileName(payslip.Filepath));
                    }

                    throw new Exception($">>> Unable to find file path on database : {payslip.Filepath}");
                }
            }
            catch (Exception e) { return ApiResult.Error(e); }
        }
        [Authorize]
        [HttpGet("mloanrequest/{employeeID}/{documentID}")]
        public IActionResult MGetLoanRequest(string employeeID, string documentID)
        {
            return ApiResult<LoanRequest>.Ok(new LoanRequest());
        }
        [Authorize]
        [HttpGet("mloanrequests/{employeeID}")]
        public IActionResult MGetLoanRequests(string employeeID)
        {
            var tasks = new List<Task<List<LoanRequest>>>
            {
                Task.Run(() => { return new PayRollAdapter(Configuration).GetLoanRequest(employeeID); }),
                Task.Run(() => { return DB.GetCollection<LoanRequest>().Find(x => x.EmployeeID == employeeID).ToList(); })
            };
            var t = Task.WhenAll(tasks);
            try { t.Wait(); }
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
        [HttpPost("mloanrequest/save")]
        public IActionResult MSaveLoanRequest([FromBody] LoanRequest loanRequest)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loanRequest.Id))
                {
                    loanRequest.RequestDate = DateTime.Now;
                }
                DB.Save(loanRequest);
                return ApiResult<object>.Ok(loanRequest, "Loan request has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"Error saving document request :\n{e.Message}");
            }
        }
        [Authorize]
        [HttpGet("mloanrequest/delete/{requestID}")]
        public IActionResult MDeleteLoanRequest(string requestID)
        {
            try
            {
                DB.Delete(new LoanRequest { Id = requestID });
                return ApiResult<LoanRequest>.Ok("Document request has been deleted successfully ");
            }
            catch (Exception e)
            {
                return ApiResult<List<LoanRequest>>.Error(
                    HttpStatusCode.BadRequest, $"Error saving document request :\n{e.Message}");
            }
        }
        [Authorize]
        [HttpGet("mloanrequest/gettemplate")]
        public IActionResult MGetTemplate()
        {
            return ApiResult<LoanMailTemplate>.Ok(
                DB.GetCollection<LoanMailTemplate>().Find(x => x.Id == "LoanTemplate").FirstOrDefault());
        }
        [Authorize]
        [HttpGet("mloanrequest/getemployee")]
        public IActionResult MGetIdentification(string employeeID)
        {
            try
            {
                return ApiResult<List<Identification>>.Ok(
              new EmployeeAdapter(Configuration).GetIdentifications(employeeID));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"Unable to get employee employments :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mloanrequest/getemployee/{employeeID}")]
        public IActionResult MGetEmployeeIdentification(string employeeID)
        {
            try
            {
                return ApiResult<List<Identification>>.Ok(
              new EmployeeAdapter(Configuration).GetIdentifications(employeeID));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(
                    HttpStatusCode.BadRequest, $"Unable to get employee employments :\n{Format.ExceptionString(e)}");
            }
        }
        [Authorize]
        [HttpGet("mloantype")]
        public IActionResult MLoanType()
        {
            return ApiResult<List<LoanType>>.Ok(new List<LoanType> {
                new LoanType {Id = "1", Name = LoanTypeName.Reguler.ToString(),MinimumRangePeriode = 0,MaximumRangePeriode = 24,MaximumLoan = 50000000, MinimumLimitLoan = 1500000},
                new LoanType {Id = "2", Name = LoanTypeName.UangTambahan.ToString(),MinimumRangePeriode = 0,MaximumRangePeriode = 10,MaximumLoan = 10000000, MinimumLimitLoan = 1500000},
                new LoanType {Id = "3", Name = LoanTypeName.DanaMitra.ToString(),MinimumRangePeriode = 0,MaximumRangePeriode = 120,MaximumLoan = 500000000, MinimumLimitLoan = 1500000}
            });
        }
        [Authorize]
        [HttpGet("mloanperiod")]
        public IActionResult MLoanPeriod()
        {
            return ApiResult<List<LoanTypeDetail>>.Ok(new List<LoanTypeDetail>
            {
                new LoanTypeDetail{IdLoanType = LoanTypeName.Reguler,LoanTypeName = LoanTypeName.Reguler.ToString(),PeriodeName = "Jangka 1 Tahun",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Normal.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 0,MaximumRangePeriode = 12,Interest = (decimal)0.01},
                new LoanTypeDetail{IdLoanType = LoanTypeName.Reguler,LoanTypeName = LoanTypeName.Reguler.ToString(),PeriodeName = "Jangka 2 Tahun",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Normal.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 13,MaximumRangePeriode = 24,Interest = (decimal)0.02},
                new LoanTypeDetail{IdLoanType = LoanTypeName.Reguler,LoanTypeName = LoanTypeName.Reguler.ToString(),PeriodeName = "Jangka 1 Tahun",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 0,MaximumRangePeriode = 12,Interest = (decimal)0.01},
                new LoanTypeDetail{IdLoanType = LoanTypeName.Reguler,LoanTypeName = LoanTypeName.Reguler.ToString(),PeriodeName = "Jangka 2 Tahun",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 13,MaximumRangePeriode = 24,Interest = (decimal)0.02},
                new LoanTypeDetail{IdLoanType = LoanTypeName.UangTambahan,LoanTypeName = LoanTypeName.UangTambahan.ToString(),PeriodeName = "Jangka 10 Bulan",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Normal.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 0,MaximumRangePeriode = 10,Interest = (decimal)0.01},
                new LoanTypeDetail{IdLoanType = LoanTypeName.UangTambahan,LoanTypeName = LoanTypeName.UangTambahan.ToString(),PeriodeName = "Jangka 10 Bulan",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Fixed,MinimumRangePeriode = 0,MaximumRangePeriode = 10,Interest = (decimal)0.01},
                new LoanTypeDetail{IdLoanType = LoanTypeName.DanaMitra,LoanTypeName = LoanTypeName.DanaMitra.ToString(),PeriodeName = "Jangka Waktu 0-5 Tahun",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Normal.ToString(),PeriodType = LoanPeriodType.Range,MinimumRangePeriode = 0,MaximumRangePeriode = 60,Interest = (decimal)0.0125,MinimumRangeLoanPeriode = 0,MaximumRangeLoanPeriode = 60,MaximumLoad = 100000000},
                new LoanTypeDetail{IdLoanType = LoanTypeName.DanaMitra,LoanTypeName = LoanTypeName.DanaMitra.ToString(),PeriodeName = "Jangka Waktu 6-10 Tahun",Methode = Core.Model.LoanMethod.Normal,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Range,MinimumRangePeriode = 61,MaximumRangePeriode = 120,Interest = (decimal)0.0125,MinimumRangeLoanPeriode = 61,MaximumRangeLoanPeriode = 120,MaximumLoad = 500000000},
                new LoanTypeDetail{IdLoanType = LoanTypeName.DanaMitra,LoanTypeName = LoanTypeName.DanaMitra.ToString(),PeriodeName = "Jangka Waktu 0-6 Tahun",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Range,MinimumRangePeriode = 0,MaximumRangePeriode = 60,Interest = (decimal)0.0133,MinimumRangeLoanPeriode = 0,MaximumRangeLoanPeriode = 60,MaximumLoad = 100000000},
                new LoanTypeDetail{IdLoanType = LoanTypeName.DanaMitra,LoanTypeName = LoanTypeName.DanaMitra.ToString(),PeriodeName = "Jangka Waktu 6-10 Tahun",Methode = Core.Model.LoanMethod.Kompensasi,MethodeName = Core.Model.LoanMethod.Kompensasi.ToString(),PeriodType = LoanPeriodType.Range,MinimumRangePeriode = 61,MaximumRangePeriode = 120,Interest = (decimal)0.0133,MinimumRangeLoanPeriode = 61,MaximumRangeLoanPeriode = 120,MaximumLoad = 500000000}
            });
        }
    }
}
