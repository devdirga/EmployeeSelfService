using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System.Net;
using System.IO;
using KANO.Core.Lib.Helper;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class BenefitController : Controller
    {
        private readonly IConfiguration Configuration;
        private readonly IUserSession Session;

        public BenefitController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Reimburse()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Benefit", URL=""},
                new Breadcrumb{Title="Reimburse", URL=""}
            };
            ViewBag.Title = "Benefit";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        public IActionResult Voucher()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Benefit", URL=""},
                new Breadcrumb{Title="Voucher", URL=""}
            };

            ViewBag.Title = "Benefit";
            ViewBag.Icon = "mdi mdi-code-array";
            return View();
        }

        [HttpPost]
        public IActionResult GetReimburse([FromBody] GridDateRange param)
        {
            param.Username = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/reimburse/", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<MedicalBenefit>>.Result>(response.Content);
            return new ApiResult<List<MedicalBenefit>>(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveReimburse([FromForm]IdentificationForm param)
        {
            var reimburse = JsonConvert.DeserializeObject<MedicalBenefit>(param.JsonData);
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/reimburse/save", Method.POST);

            reimburse.EmployeeID = employeeID;
            reimburse.EmployeeName = Session.DisplayName();
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(reimburse));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var res = await(new Client(Configuration)).Upload(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);

            return new ApiResult<object>(result);
        }

        [HttpPost]
        public IActionResult RemoveReimburse(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/reimburse/delete/{token}", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult GetMedicalType()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/list/medicalType", Method.GET);
            var response = client.Execute(request);

            var result = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(response.Content);
            return new ApiResult<List<string>>(result);
        }

        public IActionResult GetFamilies()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/families/{employeeID}", Method.GET);
            //var request = new Request("api/benefit/list/families", Method.GET);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<Family>>.Result>(response.Content);
            return new ApiResult<List<Family>>(result);
        }

        public async Task<IActionResult> ReimburseRequest([FromForm]IdentificationForm param)
        {
            var teReq = JsonConvert.DeserializeObject<MedicalBenefit>(param.JsonData);
            var req = new Request("api/benefit/reimburse/save", Method.POST);

            teReq.RequestDate = DateTime.Now;

            req.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(teReq));

            if (param.FileUpload != null)
            {
                req.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var res = await (new Client(Configuration)).Upload(req);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(res.Content);

            return new ApiResult<object>(result);
        }

        public IActionResult GetDocumentType()
        {
            var client = new Client(Configuration);
            var request = new Request("api/benefit/documenttype", Method.GET);
            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<DocumentType>>.Result>(response.Content);
            return new ApiResult<List<DocumentType>>(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveBenefitAttachment([FromForm] MedicalFieldAttachmentForm param)
        {
            var medicalbenefitdetail = JsonConvert.DeserializeObject<MedicalBenefitDetail>(param.JsonData);

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request("api/benefit/attachment/save", Method.POST);

            request.AddFormDataParameter("Field", param.Field);
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(medicalbenefitdetail));

            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            var result = JsonConvert.DeserializeObject<ApiResult<MedicalBenefitDetail>.Result>(response.Content);
            return new ApiResult<MedicalBenefitDetail>(result);
        }

        [HttpGet]
        public IActionResult DownloadMedicalDocument(string source,string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl)) {
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");
                }
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/benefit/document/download/{source}/{id}")))
                {
                    return File(stream.ToArray(), "application/force-download", x);
                }
            }
            catch (Exception e)
            {
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e).Replace("\n", "<br/>");
                return View("Error");
            }
        }

        [HttpGet]
        public IActionResult DownloadMedicalDocumentByEmployee(string source, string id, string x, string y)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                {
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");
                }
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/benefit/document/download/{source}/{id}/{x}")))
                {
                    return File(stream.ToArray(), "application/force-download", y);
                }
            }
            catch (Exception e)
            {
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e).Replace("\n", "<br/>");
                return View("Error");
            }
        }

        public IActionResult GetLimitBenefit() {
            //var client = new Client(Configuration);
            //var request = new Request($"api/benefit/limitbenefit/", Method.GET);

            //var response = client.Execute(request);
            //var result = JsonConvert.DeserializeObject<ApiResult<List<BenefitLimit>>.Result>(response.Content);
            //return new ApiResult<List<BenefitLimit>>(result);

            var client = new Client(Configuration);
            var request = new Request($"api/benefit/limitbenefit/", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult GetCreditLimitList()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/limit/list", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<List<BenefitLimit>>.Result>(response.Content);
            return new ApiResult<List<BenefitLimit>>(result);
        }

        public IActionResult GetCreditLimit()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/benefit/limit/employee/{employeeID}", Method.GET);

            var response = client.Execute(request);
            var result = JsonConvert.DeserializeObject<ApiResult<EmployeeBenefitLimit>.Result>(response.Content);
            return new ApiResult<EmployeeBenefitLimit>(result);
        }

        public async Task<IActionResult> GetByInstanceID(string source, string id)
        {
            var cl = new Client(Configuration);
            var req = new Request($"api/benefit/get/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<MedicalBenefit>.Result>(resp.Content);
            return new ApiResult<MedicalBenefit>(result);
        }


        public async Task<IActionResult> IsRequestActive()
        {
            var employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/benefit/requestStatus/{employeeID}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<UpdateRequest>.Result>(resp.Content);
            return new ApiResult<UpdateRequest>(result);
        }
    }
}