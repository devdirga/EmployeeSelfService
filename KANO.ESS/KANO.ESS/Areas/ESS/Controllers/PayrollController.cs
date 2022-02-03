using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Model.Payroll;
using KANO.Core.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using RestSharp;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class PayrollController : Controller
    {

        private IConfiguration Configuration;
        private IUserSession Session;
        private readonly String Api = "api/payroll/";
        private readonly String BearerAuth = "Bearer ";

        public PayrollController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            return View("~/Areas/ESS/Views/Payroll/Payslip.cshtml");
        }
        public IActionResult Payslip()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Payroll"},
                new Breadcrumb{Title="Payslip"}
            };
            ViewBag.Title = "Payroll";
            ViewBag.Icon = "mdi mdi-coins";
            return View();
        }

        public IActionResult LoanRequest()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Loan", URL=""},
                new Breadcrumb{Title="Loan Request", URL=""}
            };
            ViewBag.Title = "Loan Request";
            ViewBag.Icon = "mdi mdi-coins";
            return View();
        }

        public async Task<IActionResult> GetPaySlip()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/payslip/{employeeID}", Method.GET);

            var response = client.Execute(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<List<PaySlip>>.Result>(response.Content);
            return new ApiResult<List<PaySlip>>(result);
        }
        
        public async Task<IActionResult> GetLatestPaySlip()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/getlatest/{employeeID}", Method.GET);

            var response = client.Execute(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<PaySlip>.Result>(response.Content);
            return new ApiResult<PaySlip>(result);
        }

        public async Task<IActionResult> GetEmployee()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/getemployee/{employeeID}", Method.GET);

            var response = client.Execute(request);


            var result = JsonConvert.DeserializeObject<ApiResult<Employee>.Result>(response.Content);
            return new ApiResult<Employee>(result);
        }

        public async Task<IActionResult> DownloadPayslip(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();                
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/payroll/payslip/download/{employeeID}/{source}")))
                {
                    return File(stream.ToArray(), "application/force-download", id);
                }
            }
            catch (Exception e)
            {
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e);
                return View("Error");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLoanRequest(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/loanrequest/{employeeID}/{token}", Method.GET);
            var response = client.Execute(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<LoanRequest>.Result>(response.Content);
            return new ApiResult<LoanRequest>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetLoanRequests()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/loanrequests/{employeeID}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<LoanRequest>>.Result>(response.Content);
            return new ApiResult<List<LoanRequest>>(result);
        }

        [HttpPost]
        public IActionResult SaveLoanRequestx([FromBody] LoanRequest loanRequest)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/loanrequest/save", Method.POST);

            loanRequest.EmployeeID = employeeID;
            request.AddJsonParameter(loanRequest);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);

        }

        [HttpPost]
        public IActionResult SaveLoanRequest([FromBody] LoanRequest param)
        {
            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/loanrequest/save", Method.POST);
            param.EmployeeID = employeeID;
            param.EmployeeName = employeeName;
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<LoanRequest>.Result>(response.Content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var res = SendUseTemplate(result.Data, employeeID);
            }
            return new ApiResult<LoanRequest>(result);
        }

        private LoanMailTemplate GetTemplate()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/loanrequest/gettemplate", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<LoanMailTemplate>.Result>(response.Content);
            //return new ApiResult<LoanMailTemplate>(result);
            return result.Data;

        }

        private List<Identification> GetIdentification()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/loanrequest/getemployee", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Identification>>.Result>(response.Content);
            //return new ApiResult<LoanMailTemplate>(result);
            return result.Data;
        }

        private IActionResult SendUseTemplate(LoanRequest param, string empId)
        {
            try
            {
                string sNiak = "-";
                var identification = GetIdentification();
                if (identification != null)
                {
                    foreach (var x in identification)
                    {
                        if (x.Type == "NIAK")
                        {
                            sNiak = x.Number;
                        }
                    }
                }
                
                var mailTemplate = GetTemplate();
                string bodyTemplate = mailTemplate.Body;
                bodyTemplate = bodyTemplate.Replace("#NIPP#", param.EmployeeID);
                bodyTemplate = bodyTemplate.Replace("#NAMA#", param.EmployeeName);
                bodyTemplate = bodyTemplate.Replace("#NIAK#", sNiak);
                bodyTemplate = bodyTemplate.Replace("#NILAIPINJAMAN#", param.LoanValue.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("id-ID")));
                bodyTemplate = bodyTemplate.Replace("#TYPE#", param.Type.Name);
                bodyTemplate = bodyTemplate.Replace("#JANGKAWAKTU#", param.PeriodeLength.ToString() + " bulan");
                bodyTemplate = bodyTemplate.ToString().Replace("#NILAIANGSURAN#", param.InstallmentValue.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("id-ID")));

                var emailEmployee = Session.Email();
                var mailer = new Mailer(Configuration);

                var template = mailer.NewTemplate();
                template.Id = "LoanTemplate";
                template.Subject = mailTemplate.Subject;
                template.Body = bodyTemplate;
                mailer.SaveTemplate(template);

                var message = new MailMessage();
                foreach(var m in param.Type.Email)
                {
                    message.To.Add(m);
                }
                message.CC.Add(emailEmployee);
                message.Subject = template.Subject;
                
                message.Body = string.Format(template.Body, param.IdSimulation);
                mailer.SendMail(message);

                //result.Message = $"Activation mail has been sent to  {param.Email}";
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error send email :\n{e.Message}");
            }
            return ApiResult<object>.Ok("Email has been saved successfully");
        }
        private IActionResult SendEmail(LoanRequest param)
        {
            try
            {
                var emailEmployee = Session.Email();
                var mailer = new Mailer(Configuration);
                string Subject = "Loan Request " + param.EmployeeName;
                string Body = "<html><head></head>"+
                    "<body>" +
                    "<h4>This is data for loan request: </h4><br />" +
                    "<table cellpadding=16>" +
                    "<tr>" +
                        "<td width='30%'><h4>ID Simulation:</h4></td>" +
                        "<td width='70%'><h4>{0}</h4></td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Request Date:</td>" +
                        "<td>" + param.RequestDate + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>NIP:</td>" +
                        "<td>"+ param.EmployeeID + "</td>" +
                    "</tr>"+
                    "<tr>" +
                        "<td>Name:</td>" +
                        "<td>" + param.Department + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Position:</td>" +
                        "<td>" + param.Position + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Departmet:</td>" +
                        "<td>" + param.Department + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Net Income:</td>" +
                        "<td>" + param.NetIncome.ToString("C3", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Loan Name:</td>" +
                        "<td>" + param.Type.Name + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Maximal Loan:</td>" +
                        "<td>" + param.Type.MaximumLoan.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Methode Name:</td>" +
                        "<td>" + param.Type.Detail.MethodeName + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Periode Name:</td>" +
                        "<td>" + param.Type.Detail.PeriodeName + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Interest %:</td>" +
                        "<td>" + param.Type.Detail.Interest + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Loan Value Rp:</td>" +
                        "<td>" + param.LoanValue.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Periode Length:</td>" +
                        "<td>" + param.PeriodeLength + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Compensation Value:</td>" +
                        "<td>" + param.CompensationValue.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Installment:</td>" +
                        "<td>" + param.InstallmentValue.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) + "</td>" +
                    "</tr>" +
                    "<tr>" +
                        "<td>Income afte installment:</td>" +
                        "<td>" + param.IncomeAfterInstallment.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("id-ID")) + "</td>" +
                    "</tr>" +
                    "</table>" +
                    "<br/><br/>" +
                    "Copyright © 2019 Terminal Petikemas Surabaya" +
                    "</body>";

                var message = new MailMessage();
                foreach(var nadia in param.Type.Email)
                {
                    message.To.Add(nadia);
                }
                
                message.CC.Add(emailEmployee);
                message.Subject = Subject;
                message.Body = string.Format(Body, param.IdSimulation);
                mailer.SendMail(message);

                //result.Message = $"Activation mail has been sent to  {param.Email}";
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error send email :\n{e.Message}");
            }
            return ApiResult<object>.Ok("Email has been saved successfully");

        }

        [HttpPost]
        public IActionResult RemoveLoanRequest(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/payroll/loanrequest/delete/{token}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public List<ConfigurationLoan> ConfigurationLoans()
        {
            var dataConfig = new List<ConfigurationLoan>
            {
                new ConfigurationLoan
                {
                    Id = "1",
                    Name = "Reguler",
                    MinimumRangePeriode = 0,
                    MaximumRangePeriode = 24,
                    MaximumLoan = 50000000,
                    Detail = new List<LoanTypeDetail>{
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Normal,
                            MethodeName = LoanMethod.Normal.ToString(),
                            PeriodeName = "Jangka waktu 1 tahun",
                            MinimumRangePeriode = 0,
                            MaximumRangePeriode = 12,
                            PeriodType = LoanPeriodType.Range,
                            Interest = (decimal) 0.01
                        },
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Normal,
                            MethodeName = LoanMethod.Normal.ToString(),
                            PeriodeName = "Jangka waktu 2 tahun",
                            MinimumRangePeriode = 13,
                            MaximumRangePeriode = 24,
                            PeriodType = LoanPeriodType.Range,
                            Interest = (decimal) 0.02
                        },
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Kompensasi,
                            MethodeName = LoanMethod.Kompensasi.ToString(),
                            PeriodeName = "Jangka waktu 1 tahun",
                            MinimumRangePeriode = 0,
                            MaximumRangePeriode = 12,
                            PeriodType = LoanPeriodType.Range,
                            Interest = (decimal) 0.01
                        },
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Kompensasi,
                            MethodeName = LoanMethod.Kompensasi.ToString(),
                            PeriodeName = "Jangka waktu 2 tahun",
                            MinimumRangePeriode = 13,
                            MaximumRangePeriode = 24,
                            PeriodType = LoanPeriodType.Range,
                            Interest = (decimal) 0.02
                        },
                    },
                    /*
                    DetailType = new LoanTypeDetail[]
                    {
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Normal,
                            MethodeName = LoanMethod.Normal.ToString(),
                            Periodename = "AA"
                        },
                    }
                    */
                },
                new ConfigurationLoan
                {
                    Id = "2",
                    Name = "Uang Tambahan",
                    MinimumRangePeriode = 0,
                    MaximumRangePeriode = 10,
                    MaximumLoan = 10000000,
                    Detail = new List<LoanTypeDetail>
                    {
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Normal,
                            MethodeName = LoanMethod.Normal.ToString(),
                            PeriodeName = "Jangka waktu 10 bulan",
                            MinimumRangePeriode = 0,
                            MaximumRangePeriode = 10,
                            PeriodType = LoanPeriodType.Fixed,
                            Interest = (decimal) 0.01
                        },
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Kompensasi,
                            MethodeName = LoanMethod.Kompensasi.ToString(),
                            PeriodeName = "Jangka waktu 10 bulan",
                            MinimumRangePeriode = 0,
                            MaximumRangePeriode = 10,
                            PeriodType = LoanPeriodType.Fixed,
                            Interest = (decimal) 0.01
                        },
                    }
                },
                new ConfigurationLoan
                {
                    Id = "3",
                    Name = "Dana Mitra",
                    MinimumRangePeriode = 0,
                    MaximumRangePeriode = 120,
                    MaximumLoan = 500000000,
                    Detail = new List<LoanTypeDetail>
                    {
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Normal,
                            MethodeName = LoanMethod.Normal.ToString(),
                            PeriodeName = "Jangka waktu 0 - 5 tahun",
                            MinimumRangePeriode = 0,
                            MaximumRangePeriode = 60,
                            PeriodType = LoanPeriodType.Range,
                            Interest = (decimal) 0.0125,
                            MinimumRangeLoanPeriode = 0,
                            MaximumRangeLoanPeriode = 60,
                            MaximumLoad = 100000000
                        },
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Normal,
                            MethodeName = LoanMethod.Normal.ToString(),
                            PeriodeName = "Jangka waktu 5 - 10 tahun",
                            MinimumRangePeriode = 61,
                            MaximumRangePeriode = 120,
                            PeriodType = LoanPeriodType.Range,
                            Interest = (decimal) 0.0125,
                            MinimumRangeLoanPeriode = 61,
                            MaximumRangeLoanPeriode = 120,
                            MaximumLoad = 500000000
                        },
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Kompensasi,
                            MethodeName = LoanMethod.Kompensasi.ToString(),
                            PeriodeName = "Jangka waktu 0 - 5 tahun",
                            MinimumRangePeriode = 0,
                            MaximumRangePeriode = 60,
                            PeriodType = LoanPeriodType.Range,
                            Interest = (decimal) 0.0133,
                            MinimumRangeLoanPeriode = 0,
                            MaximumRangeLoanPeriode = 60,
                            MaximumLoad = 100000000
                        },
                        new LoanTypeDetail
                        {
                            Methode = LoanMethod.Kompensasi,
                            MethodeName = LoanMethod.Kompensasi.ToString(),
                            PeriodeName = "Jangka waktu 5 - 10 tahun",
                            MinimumRangePeriode = 61,
                            MaximumRangePeriode = 120,
                            PeriodType = LoanPeriodType.Range,
                            Interest = (decimal) 0.0133,
                            MinimumRangeLoanPeriode = 61,
                            MaximumRangeLoanPeriode = 120,
                            MaximumLoad = 500000000
                        }
                    }
                }
            };

            return dataConfig.ToList();
        }

        public List<LoanType> ListLoanType()
        {
            var dataloan = new List<LoanType>{
                new LoanType {
                    Id = "1",//LoanTypeName.Reguler,
                    Name = LoanTypeName.Reguler.ToString(),
                    MinimumRangePeriode = 0,
                    MaximumRangePeriode = 24,
                    MaximumLoan = 50000000,
                    //Email = new string(){["dzanurano@gmail.com"]
                },
                new LoanType {
                    Id = "2",//LoanTypeName.UangTambahan,
                    Name = LoanTypeName.UangTambahan.ToString(),
                    MinimumRangePeriode = 0,
                    MaximumRangePeriode = 10,
                    MaximumLoan = 10000000,
                    //Email = [""]
                },
                new LoanType {
                    Id = "1",//LoanTypeName.DanaMitra,
                    Name = LoanTypeName.DanaMitra.ToString(),
                    MinimumRangePeriode = 0,
                    MaximumRangePeriode = 120,
                    MaximumLoan = 500000000,
                    //Email = [""]
                },
            };
            
            return dataloan.ToList();
        }

        public List<LoanTypeDetail> ListLoanTypeDetail()
        {
            var loantype = new List<LoanTypeDetail>();
            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.Reguler,
                LoanTypeName = LoanTypeName.Reguler.ToString(),
                PeriodeName = "Jangka 1 Tahun",
                Methode = LoanMethod.Normal,
                MethodeName = LoanMethod.Normal.ToString(),
                PeriodType = LoanPeriodType.Fixed,
                MinimumRangePeriode = 0,
                MaximumRangePeriode = 12,
                Interest = (decimal) 0.01
            });
            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.Reguler,
                LoanTypeName = LoanTypeName.Reguler.ToString(),
                PeriodeName = "Jangka 2 Tahun",
                Methode = LoanMethod.Normal,
                MethodeName = LoanMethod.Normal.ToString(),
                PeriodType = LoanPeriodType.Fixed,
                MinimumRangePeriode = 13,
                MaximumRangePeriode = 24,
                Interest = (decimal) 0.02
            });
            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.Reguler,
                LoanTypeName = LoanTypeName.Reguler.ToString(),
                PeriodeName = "Jangka 1 Tahun",
                Methode = LoanMethod.Kompensasi,
                MethodeName = LoanMethod.Kompensasi.ToString(),
                PeriodType = LoanPeriodType.Fixed,
                MinimumRangePeriode = 0,
                MaximumRangePeriode = 12,
                Interest = (decimal) 0.01
            });
            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.Reguler,
                LoanTypeName = LoanTypeName.Reguler.ToString(),
                PeriodeName = "Jangka 2 Tahun",
                Methode = LoanMethod.Kompensasi,
                MethodeName = LoanMethod.Kompensasi.ToString(),
                PeriodType = LoanPeriodType.Fixed,
                MinimumRangePeriode = 13,
                MaximumRangePeriode = 24,
                Interest = (decimal) 0.02
            });

            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.UangTambahan,
                LoanTypeName = LoanTypeName.UangTambahan.ToString(),
                PeriodeName = "Jangka 10 Bulan",
                Methode = LoanMethod.Normal,
                MethodeName = LoanMethod.Normal.ToString(),
                PeriodType = LoanPeriodType.Fixed,
                MinimumRangePeriode = 0,
                MaximumRangePeriode = 10,
                Interest = (decimal) 0.01
            });
            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.UangTambahan,
                LoanTypeName = LoanTypeName.UangTambahan.ToString(),
                PeriodeName = "Jangka 10 Bulan",
                Methode = LoanMethod.Kompensasi,
                MethodeName = LoanMethod.Kompensasi.ToString(),
                PeriodType = LoanPeriodType.Fixed,
                MinimumRangePeriode = 0,
                MaximumRangePeriode = 10,
                Interest = (decimal) 0.01
            });

            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.DanaMitra,
                LoanTypeName = LoanTypeName.DanaMitra.ToString(),
                PeriodeName = "Jangka Waktu 0-5 Tahun",
                Methode = LoanMethod.Normal,
                MethodeName = LoanMethod.Normal.ToString(),
                PeriodType = LoanPeriodType.Range,
                MinimumRangePeriode = 0,
                MaximumRangePeriode = 60,
                Interest = (decimal)0.0125,
                MinimumRangeLoanPeriode = 0,
                MaximumRangeLoanPeriode = 60,
                MaximumLoad = 100000000
            });
            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.DanaMitra,
                LoanTypeName = LoanTypeName.DanaMitra.ToString(),
                PeriodeName = "Jangka Waktu 6-10 Tahun",
                Methode = LoanMethod.Normal,
                MethodeName = LoanMethod.Kompensasi.ToString(),
                PeriodType = LoanPeriodType.Range,
                MinimumRangePeriode = 61,
                MaximumRangePeriode = 120,
                Interest = (decimal) 0.0125,
                MinimumRangeLoanPeriode = 61,
                MaximumRangeLoanPeriode = 120,
                MaximumLoad = 500000000
            });
            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.DanaMitra,
                LoanTypeName = LoanTypeName.DanaMitra.ToString(),
                PeriodeName = "Jangka Waktu 0-6 Tahun",
                Methode = LoanMethod.Kompensasi,
                MethodeName = LoanMethod.Kompensasi.ToString(),
                PeriodType = LoanPeriodType.Range,
                MinimumRangePeriode = 0,
                MaximumRangePeriode = 60,
                Interest = (decimal)0.0133,
                MinimumRangeLoanPeriode = 0,
                MaximumRangeLoanPeriode = 60,
                MaximumLoad = 100000000
            });
            loantype.Add(new LoanTypeDetail
            {
                IdLoanType = LoanTypeName.DanaMitra,
                LoanTypeName = LoanTypeName.DanaMitra.ToString(),
                PeriodeName = "Jangka Waktu 6-10 Tahun",
                Methode = LoanMethod.Kompensasi,
                MethodeName = LoanMethod.Kompensasi.ToString(),
                PeriodType = LoanPeriodType.Range,
                MinimumRangePeriode = 61,
                MaximumRangePeriode = 120,
                Interest = (decimal)0.0133,
                MinimumRangeLoanPeriode = 61,
                MaximumRangeLoanPeriode = 120,
                MaximumLoad = 500000000
            });

            return loantype;
        }

        public IActionResult ListTypeName()
        {
            List<LoanTypeName> loan = Enum.GetValues(typeof(LoanTypeName)).Cast<LoanTypeName>().ToList();
            //LoanTypeName[] loan = (LoanTypeName[])Enum.GetValues(typeof(LoanTypeName));
            return ApiResult<object>.Ok(loan);
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetPaySlip(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<PaySlip>>(JsonConvert.DeserializeObject<ApiResult<List<PaySlip>>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mpayslip/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetLatestPaySlip(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<PaySlip>(JsonConvert.DeserializeObject<ApiResult<PaySlip>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mgetlatest/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetEmployee(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<Employee>(JsonConvert.DeserializeObject<ApiResult<Employee>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mgetemployee/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MDownloadPayslip(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");
                }
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}{Api}mpayslip/download/{source}/{id}")))
                {
                    return File(stream.ToArray(), "application/force-download", x);
                }
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.InternalServerError, $"Well it is embarassing, internal server error : {e.Message}");
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetLoanRequest(string token, String source)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<LoanRequest>(JsonConvert.DeserializeObject<ApiResult<LoanRequest>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mloanrequest/{token}/{source}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MGetLoanRequests(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<LoanRequest>>(JsonConvert.DeserializeObject<ApiResult<List<LoanRequest>>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mloanrequests/{token}", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult MSaveLoanRequest([FromBody] LoanRequest param)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            var response = new Client(Configuration).Execute(new Request($"{Api}mloanrequest/save", Method.POST, param, "Authorization", bearerAuth));
            var result = JsonConvert.DeserializeObject<ApiResult<LoanRequest>.Result>(response.Content);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                SendUseTemplate(result.Data, param.EmployeeID);
            }
            return new ApiResult<LoanRequest>(result);
        }
        [HttpGet]
        [AllowAnonymous]
        private LoanMailTemplate MGetTemplate()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return JsonConvert.DeserializeObject<ApiResult<LoanMailTemplate>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mloanrequest/gettemplate", Method.GET, "Authorization", bearerAuth)).Content).Data;

        }
        [HttpGet]
        [AllowAnonymous]
        private List<Identification> MGetIdentification(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return JsonConvert.DeserializeObject<ApiResult<List<Identification>>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mloanrequest/getemployee/{token}", Method.GET, "Authorization", bearerAuth)).Content).Data;
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MListLoanType()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<LoanType>>(JsonConvert.DeserializeObject<ApiResult<List<LoanType>>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mloantype", Method.GET, "Authorization", bearerAuth)).Content));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult MListLoanMethod()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<MLoanMethod>>(JsonConvert.DeserializeObject<ApiResult<List<MLoanMethod>>.Result>(
                new Client(Configuration).Execute(new Request($"{Api}mloanmethod", Method.GET, "Authorization", bearerAuth)).Content));
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult MListLoanPeriod()
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<List<LoanTypeDetail>>(
                JsonConvert.DeserializeObject<ApiResult<List<LoanTypeDetail>>.Result>(
                    new Client(Configuration).Execute(new Request($"{Api}mloanperiod", Method.GET, "Authorization", bearerAuth)).Content));
        }
    }
}