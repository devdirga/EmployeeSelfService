using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using KANO.Core.Lib;
using KANO.Core.Lib.Auth;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using RestSharp;
using Newtonsoft.Json;
using System.Net;
using System.Net.Mail;
using KANO.Core.Lib.Helper;
using Newtonsoft.Json.Linq;
using System.IO;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class CanteenController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;

        public CanteenController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Canteen", URL=""},
                //new Breadcrumb{Title="Medical Benefit ", URL=""},
                new Breadcrumb{Title="Voucher", URL=""}
            };

            ViewBag.Title = "Canteen";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        public IActionResult Manage()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Canteen", URL=""},
                //new Breadcrumb{Title="Medical Benefit ", URL=""},
                new Breadcrumb{Title="Manage", URL=""}
            };

            ViewBag.Title = "Manage Canteen";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        public IActionResult Report()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Canteen", URL=""},
                //new Breadcrumb{Title="Medical Benefit ", URL=""},
                new Breadcrumb{Title="Report", URL=""}
            };

            ViewBag.Title = "Report Canteen";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        public IActionResult ClaimVoucher()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Canteen", URL=""},
                //new Breadcrumb{Title="Medical Benefit ", URL=""},
                new Breadcrumb{Title="Claim", URL=""}
            };

            ViewBag.Title = "Claim Voucher";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        public IActionResult PaymentClaim()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Canteen", URL=""},
                //new Breadcrumb{Title="Medical Benefit ", URL=""},
                new Breadcrumb{Title="Payment Claim", URL=""}
            };

            ViewBag.Title = "Payment Claim";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        public IActionResult Profile()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Canteen", URL=""},
                //new Breadcrumb{Title="Medical Benefit ", URL=""},
                new Breadcrumb{Title="Profile", URL=""}
            };

            ViewBag.Title = "Profile Canteen";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        public async Task<IActionResult> Get()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/get", Method.GET);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Canteen>>.Result>(response.Content);
            return new ApiResult<List<Canteen>>(result);
        }

        public async Task<IActionResult> GetUserDetail(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/get/user/{token}", Method.GET);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<User>.Result>(response.Content);
            return new ApiResult<User>(result);
        }
        

        public async Task<IActionResult> Save([FromForm] CanteenForm param)
        {
            var canteen = JsonConvert.DeserializeObject<Canteen>(param.JsonData);
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/save", Method.POST);

            canteen.CreatedByID = Session.Id();
            canteen.CreatedByName = Session.DisplayName();
            //request.AddJsonParameter(param);

            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(canteen));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);            
            if (result.StatusCode == HttpStatusCode.OK && !string.IsNullOrWhiteSpace(result.Data.ToString()) && canteen.CanteenUser==CanteenUser.NewUser)
            {
                try
                {
                    var mailer = new Mailer(Configuration);
                    var Subject = "User Canteen";
                    var linkLogin = $"{this.Request.Scheme.ToString()}://{this.Request.Host.ToString()}/Site/Auth/";
                    var Body = $"Hi {canteen.PICName},<br/><br/>" +
                                        "Here is your username and password in order to manage your merchant in TPS canteen <br/><br/>" +
                                        $"\tUsername : {canteen.User.Username} <br/>" +
                                        $"\tPassword : {result.Data} <br/><br/>"+
                                        $"<a href=\"{linkLogin}\" target=\"_blank\" style=\"background-color:#F00;padding:5px;text-align:center;color:white;font-weight:bold;width:100px;\">Login</a><br/><br/>" +
                                        "If the button didn't work in your browser, try this link instead:<br/>" +
                                        $"<a href='{linkLogin}'>{linkLogin}</a><br/><br/>" +
                                        "Copyright © 2019 Terminal Petikemas Surabaya";                    

                    var message = new MailMessage();
                    message.To.Add(canteen.User.Email);
                    message.Subject = Subject;
                    message.Body = Body;
                    mailer.SendMail(message);

                    result.Message = $"User credential has been sent to {canteen.User.Email}";
                    return new ApiResult<object>(result);
                } catch (Exception e)
                {
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error while sending email to :\n{Format.ExceptionString(e)}");
                }
                
            }
            
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Image(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/benefit/canteen/image/{source}")))
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

        public async Task<IActionResult> Delete(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/delete/{token}", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);

            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> EmployeeRedeemHistory()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/redeem/history/employee/{employeeID}", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<Redeem>>.Result>(response.Content);
            return new ApiResult<List<Redeem>>(result);
        }

        public async Task<IActionResult> Redeem([FromBody] Redeem param)
        {            
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/redeem", Method.POST);

            param.EmployeeID = Session.Id();
            param.EmployeeName = Session.DisplayName();
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> CanteenRedeemHistory([FromBody] GridDateRange param)
        {
            param.Username = Session.Thumbprint();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/redeem/history", Method.POST);

            request.AddJsonParameter(param);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<Redeem>>.Result>(response.Content);
            return new ApiResult<List<Redeem>>(result);
        }

        public async Task<IActionResult> GetVoucher()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/voucher/info/{employeeID}", Method.GET);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<VoucherInfo>.Result>(response.Content);
            return new ApiResult<VoucherInfo>(result);
        }

        public async Task<IActionResult> GetClaimInfo()
        {
            string userID = Session.Thumbprint();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/claim/info/{userID}", Method.GET);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<ClaimCanteenInfo>.Result>(response.Content);
            return new ApiResult<ClaimCanteenInfo>(result);
        }

        public IActionResult HistoryClaim([FromBody] GridDateRange param)
        {
            param.Username = Session.Thumbprint();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/claim/history", Method.POST);

            request.AddJsonParameter(param);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<ClaimCanteen>>.Result>(response.Content);
            return new ApiResult<List<ClaimCanteen>>(result);
        }

        public IActionResult CanteenClaim([FromBody] GridDateRange param)
        {
            param.Username = Session.Thumbprint();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/claim", Method.POST);

            request.AddJsonParameter(param);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<RedeemGroup>>.Result>(response.Content);
            return new ApiResult<List<RedeemGroup>>(result);
        }

        public IActionResult SaveClaim([FromBody] ClaimCanteen param)
        {
            param.CanteenUserID = Session.Thumbprint();
            param.CanteenName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/claim/save", Method.POST);

            request.AddJsonParameter(param);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<RedeemGroup>>.Result>(response.Content);
            return new ApiResult<List<RedeemGroup>>(result);
        }

        public IActionResult HistoryPaymentClaim([FromBody] GridDateRange param)
        {
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/history/paymentclaim", Method.POST);

            request.AddJsonParameter(param);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<ClaimCanteen>>.Result>(response.Content);
            return new ApiResult<List<ClaimCanteen>>(result);
        }

        public IActionResult SavePaid([FromBody] List<ClaimCanteen> param)
        {
            //param.CanteenId = Session.Id();
            //param.CanteenName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/payment/save", Method.POST);

            request.AddJsonParameter(param);

            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> GetHistoryRedeem([FromBody] GridDateRange param)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/redeem/history/all", Method.POST);

            request.AddJsonParameter(param);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<Redeem>>.Result>(response.Content);
            return new ApiResult<List<Redeem>>(result);
        }

        public IActionResult RequestVoucher()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/voucher/request/{employeeID }", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<NoVoucher>>.Result>(response.Content);
            return new ApiResult<List<NoVoucher>>(result);
        }

        public async Task<IActionResult> SaveProfile([FromForm] CanteenForm param)
        {
            Canteen canteen = JsonConvert.DeserializeObject<Canteen>(param.JsonData);
            Client client = new Client(Configuration);
            Request request = new Request($"api/benefit/canteen/saveprofile", Method.POST);
            canteen.CreatedByID = Session.Id();
            canteen.CreatedByName = Session.DisplayName();
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(canteen));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            var response = await client.Upload(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> GetProfile()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/canteen/getprofile", Method.POST);

            Canteen canteen = new Canteen() {
                UserID = Session.Thumbprint()
            };
            request.AddJsonParameter(canteen);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<Canteen>>.Result>(response.Content);
            return new ApiResult<List<Canteen>>(result);
        }
    }
}