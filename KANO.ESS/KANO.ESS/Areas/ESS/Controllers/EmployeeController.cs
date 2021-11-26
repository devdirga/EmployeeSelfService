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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;

namespace KANO.ESS.Areas.ESS.Controllers
{
    [Area("ESS")]
    public class EmployeeController : Controller
    {
        private IConfiguration Configuration;
        private IUserSession Session;

        public EmployeeController(IConfiguration config, IUserSession session)
        {
            Configuration = config;
            Session = session;
        }

        public IActionResult Index()
        {
            return View("~/Areas/ESS/Views/Employee/Profile.cshtml");
        }

        public IActionResult Profile()
        {
            ViewBag.Breadcrumbs = new[]{
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Employee"},
                new Breadcrumb{Title="Profile"}
            };
            ViewBag.Title = "Profile";
            ViewBag.Icon = "mdi mdi-account-box";

            return View();
        }

        public IActionResult Information()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Employee"},
                new Breadcrumb{Title="Information"}
            };

            ViewBag.Title = "Information";
            ViewBag.Icon = "mdi mdi-account-box";
            return View();
        }

        public IActionResult DocumentRequest()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Employee"},
                new Breadcrumb{Title="Document Request"}
            };

            ViewBag.Title = "Document Request";
            ViewBag.Icon = "mdi mdi-file-document";
            return View();
        }

        public IActionResult Applications()
        {
            ViewBag.Breadcrumbs = new[] {
                new Breadcrumb{Title="ESS"},
                new Breadcrumb{Title="Employee"},
                new Breadcrumb{Title="Applications"}
            };

            ViewBag.Title = "Dashboard";
            ViewBag.Icon = "mdi mdi-account-multiple";
            return View();
        }

        public async Task<IActionResult> Get()
        {            
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/get/detail/{employeeID}", Method.GET);
            var response = client.Execute(request);            
            var employee = JsonConvert.DeserializeObject<ApiResult<EmployeeResult>.Result>(response.Content);
            return new ApiResult<EmployeeResult>(employee);
        }

        public async Task<IActionResult> Update([FromBody] Employee employee)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/update", Method.POST);

            employee.EmployeeID = employeeID;
            request.AddJsonParameter(employee);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> Discard()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/discardChange/{employeeID}", Method.GET);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> UpdateRequestResume([FromBody] UpdateRequest updateRequest)
        {            
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/updateRequest/resume", Method.POST);
            
            updateRequest.EmployeeID = employeeID;
            request.AddJsonParameter(updateRequest);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> IsRequestActive()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/requestStatus/resume/{employeeID}", Method.GET);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<UpdateRequest>.Result>(response.Content);
            return new ApiResult<UpdateRequest>(result);
        }

        public async Task<IActionResult> GetFamily(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/family/{employeeID}/{token}", Method.GET);
            var response = client.Execute(request);
           

            var employee = JsonConvert.DeserializeObject<ApiResult<Family>.Result>(response.Content).Data;
            return ApiResult<Family>.Ok(employee);
        }

        public async Task<IActionResult> GetFamilies()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/families/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<FamilyResult>>.Result>(response.Content);
            return new ApiResult<List<FamilyResult>>(result);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFamilyDocument(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/family/document/download/{employeeID}/{source}")))
                {
                    return File(stream.ToArray(), "application/force-download", id);
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
        public async Task<IActionResult> DownloadFamilyAttachment(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/family/attachment/download/{source}/{id}")))
                {
                    return File(stream.ToArray(), "application/force-download", x);
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

        public async Task<IActionResult> SaveFamily([FromForm] FamilyForm param)
        {
            var family = JsonConvert.DeserializeObject<Family>(param.JsonData);

            var url = "api/employee/family/update";
            if (family.AXID <= 0)
            {
                url = "api/employee/family/create";
            }

            var employeeName = Session.DisplayName();
            var client = new Client(Configuration);
            var request = new Request(url, Method.POST);

            family.EmployeeID = Session.Id();
            family.EmployeeName = Session.DisplayName();
            family.EmployeeName = employeeName;
            request.AddFormDataParameter("Reason", param.Reason);
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(family));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult RemoveFamily([FromBody] DeleteForm param)
        {
            param.EmployeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/family/delete", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult DiscardFamilyChange(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/family/discardChange/{token}", Method.GET);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }        

        public async Task<IActionResult> GetDocument(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/document/{employeeID}/{token}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<Document>.Result>(response.Content);
            return new ApiResult<Document>(result);
        }

        public async Task<IActionResult> GetDocuments()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/documents/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<DocumentResult>>.Result>(response.Content);
            return new ApiResult<List<DocumentResult>>(result);
        }        

        [HttpGet]
        public async Task<IActionResult> DownloadDocument(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/document/download/{employeeID}/{source}")))
                {
                    return File(stream.ToArray(), "application/force-download", id);
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

        public async Task<IActionResult> SaveDocument([FromForm] DocumentForm param)
        {
            var document = JsonConvert.DeserializeObject<Document>(param.JsonData);
            var url = "api/employee/document/update";
            if (document.AXID == -1)
            {
                url = "api/employee/document/create";
            }

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request(url, Method.POST);

            document.EmployeeID = employeeID;
            document.EmployeeName = employeeName;
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(document));
            
            if (param.FileUpload != null) {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }
            

            var response = await client.Upload(request);
            
            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);

        }

        [HttpPost]
        public IActionResult RemoveDocument(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/document/delete/{employeeID}/{token}", Method.GET);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult DiscardDocumentChange(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/document/discardChange/{token}", Method.GET);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetIdentifications()
        {
            
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/identification/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<IdentificationResult>>.Result>(response.Content);
            return new ApiResult<List<IdentificationResult>>(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveIdentification([FromForm] IdentificationForm param)
        {
            var document = JsonConvert.DeserializeObject<Identification>(param.JsonData);          

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request("api/employee/identification/save", Method.POST);

            document.EmployeeID = employeeID;
            document.EmployeeName = employeeName;
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(document));

            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }


            var response = await client.Upload(request);
            
            var result = JsonConvert.DeserializeObject<ApiResult<Identification>.Result>(response.Content);
            return new ApiResult<Identification>(result);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadIdentification(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/identification/download/{employeeID}/{source}")))
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
        public async Task<IActionResult> DownloadIdentificationAttachment(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/identification/download/{source}")))
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
        public async Task<IActionResult> GetBankAccounts()
        {            
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/bankAccount/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<BankAccountResult>>.Result>(response.Content);
            return new ApiResult<List<BankAccountResult>>(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveBankAccount([FromForm] BankAccountForm param)
        {
            var document = JsonConvert.DeserializeObject<BankAccount>(param.JsonData);

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request("api/employee/bankAccount/save", Method.POST);

            document.EmployeeID = employeeID;
            document.EmployeeName = employeeName;
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(document));

            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }


            var response = await client.Upload(request);
            
            var result = JsonConvert.DeserializeObject<ApiResult<BankAccount>.Result>(response.Content);
            return new ApiResult<BankAccount>(result);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadBankAccount(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/bankAccount/download/{employeeID}/{source}")))
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
        public async Task<IActionResult> DownloadBankAccountAttachment(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/bankAccount/download/{source}")))
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
        public async Task<IActionResult> GetTaxes()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/tax/{employeeID}", Method.GET);
            var response = client.Execute(request);


            var result = JsonConvert.DeserializeObject<ApiResult<List<TaxResult>>.Result>(response.Content);
            return new ApiResult<List<TaxResult>>(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveTax([FromForm] TaxForm param)
        {
            var document = JsonConvert.DeserializeObject<Tax>(param.JsonData);

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request("api/employee/tax/save", Method.POST);

            document.EmployeeID = employeeID;
            document.EmployeeName = employeeName;
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(document));

            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }


            var response = await client.Upload(request);

            var result = JsonConvert.DeserializeObject<ApiResult<Tax>.Result>(response.Content);
            return new ApiResult<Tax>(result);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadTax(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/tax/download/{employeeID}/{source}")))
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
        public async Task<IActionResult> DownloadTaxAttachment(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/tax/download/{source}")))
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
        public async Task<IActionResult> GetElectronicAddresses()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/electronicAddress/{employeeID}", Method.GET);
            var response = client.Execute(request);


            var result = JsonConvert.DeserializeObject<ApiResult<List<ElectronicAddressResult>>.Result>(response.Content);
            return new ApiResult<List<ElectronicAddressResult>>(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveElectronicAddress([FromForm] ElectronicAddressForm param)
        {
            var document = JsonConvert.DeserializeObject<ElectronicAddress>(param.JsonData);

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request("api/employee/electronicAddress/save", Method.POST);

            document.EmployeeID = employeeID;
            document.EmployeeName = employeeName;
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(document));

            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }


            var response = await client.Upload(request);

            var result = JsonConvert.DeserializeObject<ApiResult<ElectronicAddress>.Result>(response.Content);
            return new ApiResult<ElectronicAddress>(result);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadElectronicAddress(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/electronicAddress/download/{employeeID}/{source}")))
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
        public async Task<IActionResult> DownloadElectronicAddressAttachment(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/electronicAddress/download/{source}")))
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
        public async Task<IActionResult> GetAddress()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/address/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<AddressResult>.Result>(response.Content);
            return new ApiResult<AddressResult>(result);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAddress([FromForm] AddressForm param)
        {
            var document = JsonConvert.DeserializeObject<Address>(param.JsonData);

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request("api/employee/address/save", Method.POST);

            document.EmployeeID = employeeID;
            document.EmployeeName = employeeName;
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(document));

            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }


            var response = await client.Upload(request);
            
            var result = JsonConvert.DeserializeObject<ApiResult<Address>.Result>(response.Content);
            return new ApiResult<Address>(result);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAddress(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/address/download/{employeeID}/{source}")))
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
        public async Task<IActionResult> DownloadAddressAttachment(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/address/download/{source}")))
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

        [HttpPost]
        public async Task<IActionResult> SaveEmployeeAttachment([FromForm] EmployeeFieldAttachmentForm param)
        {
            var employee = JsonConvert.DeserializeObject<Employee>(param.JsonData);

            var employeeID = Session.Id();
            var employeeName = Session.DisplayName();

            var client = new Client(Configuration);
            var request = new Request("api/employee/attachment/save", Method.POST);

            employee.EmployeeID = employeeID;
            employee.EmployeeName = employeeName;
            request.AddFormDataParameter("Field", param.Field);
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(employee));

            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            var result = JsonConvert.DeserializeObject<ApiResult<Employee>.Result>(response.Content);
            return new ApiResult<Employee>(result);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadEmployeeAttachment(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/attachment/download/{source}/{employeeID}/{id}")))
                {
                    return File(stream.ToArray(), "application/force-download", x);
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
        public async Task<IActionResult> DownloadEmployeeFieldAttachment(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/attachment/download/{source}/{id}")))
                {
                    return File(stream.ToArray(), "application/force-download", x);
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
        public async Task<IActionResult> GetEmployments()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/employments/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<Employment>>.Result>(response.Content);
            return new ApiResult<List<Employment>>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCertificates()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/certificates/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<CertificateResult>>.Result>(response.Content);
            return new ApiResult<List<CertificateResult>>(result);
        }

        public async Task<IActionResult> SaveCertificate([FromForm] CertificateForm param)
        {
            var certificate = JsonConvert.DeserializeObject<Certificate>(param.JsonData);

            var url = "api/employee/certificate/update";            
            if (certificate.AXID == -1)
            {
                url = "api/employee/certificate/create";
            }
            
            var client = new Client(Configuration);
            var request = new Request(url, Method.POST);

            certificate.EmployeeID = Session.Id();
            certificate.EmployeeName = Session.DisplayName();
            request.AddFormDataParameter("Reason", param.Reason);
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(certificate));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult RemoveCertificate([FromBody] DeleteForm param)
        {
            param.EmployeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/certificate/delete", Method.POST);
            request.AddJsonParameter(param);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public IActionResult DiscardCertificateChange(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/certificate/discardChange/{token}", Method.GET);

            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        public async Task<IActionResult> DownloadCertificate(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/certificate/download/{employeeID}/{source}")))
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
        public async Task<IActionResult> DownloadCertificateAttachment(string source, string id, string x)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/certificate/attachment/download/{source}/{id}")))
                {
                    return File(stream.ToArray(), "application/force-download", x);
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
        public async Task<IActionResult> GetInstallments()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/installments/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<Installment>>.Result>(response.Content);
            return new ApiResult<List<Installment>>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetWarningLetters()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/warningLetters/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<WarningLetter>>.Result>(response.Content);
            return new ApiResult<List<WarningLetter>>(result);
        }

        public async Task<IActionResult> DownloadWarningLetter(string source, string id)
        {
            try
            {
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration");

                // Fetching file info
                var employeeID = Session.Id();

                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/download/warningLetter/{employeeID}/{source}")))
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
        public async Task<IActionResult> GetMedicalRecords()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/medicalRecords/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<MedicalRecord>>.Result>(response.Content);
            return new ApiResult<List<MedicalRecord>>(result);
        }

        public async Task<IActionResult> DownloadMedicalRecord()
        {
            var token = Request.Form["token"];
            try
            {
                Console.WriteLine($"{DateTime.Now} >>> {token}");
                var baseUrl = Configuration["Request:GatewayUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl)) { return ApiResult<object>.Error(HttpStatusCode.InternalServerError, "Unable to find gateway url configuration"); }

                var decodedToken = WebUtility.UrlDecode(token);
                var filepath = Hasher.Decrypt(decodedToken);
                var filename = Path.GetFileName(filepath);

                // Fetching file info
                WebClient wc = new WebClient();
                using (MemoryStream stream = new MemoryStream(wc.DownloadData($"{baseUrl}api/employee/medicalRecord/download/{token}")))
                {
                    return File(stream.ToArray(), "application/force-download", filename);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} <<< {Format.ExceptionString(e)}");
                ViewBag.ErrorCode = 500;
                ViewBag.ErrorDescription = "Well it is embarassing, internal server error";
                ViewBag.ErrorDetail = Format.ExceptionString(e);
                return View("Error");
            }
        }        

        [HttpGet]
        public async Task<IActionResult> GetDocumentRequest(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/documentRequest/{employeeID}/{token}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<DocumentRequest>.Result>(response.Content).Data;
            return ApiResult<DocumentRequest>.Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentRequests()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/documentRequests/{employeeID}", Method.GET);
            var response = client.Execute(request);
           

            var result = JsonConvert.DeserializeObject<ApiResult<List<DocumentRequest>>.Result>(response.Content);
            return new ApiResult<List<DocumentRequest>>(result);
        }        

        [HttpPost]
        public async Task<IActionResult> SaveDocumentRequest([FromForm] DocumentRequestForm param)
        {
            var documentRequest = JsonConvert.DeserializeObject<DocumentRequest>(param.JsonData);
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/documentRequest/save", Method.POST);

            documentRequest.EmployeeID = employeeID;
            request.AddFormDataParameter("JsonData", JsonConvert.SerializeObject(documentRequest));
            if (param.FileUpload != null)
            {
                request.AddFormDataFile("FileUpload", param.FileUpload.FirstOrDefault());
            }

            var response = await client.Upload(request);
            

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        [HttpPost]
        public IActionResult RemoveDocumentRequest(string token)
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/documentRequest/delete/{token}", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetApplicant(string token)
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/applicant/{employeeID}/{token}", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<Application>.Result>(response.Content);
            return new ApiResult<Application>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetApplicants()
        {
            var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request($"api/employee/applicants/{employeeID}", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<List<Application>>.Result>(response.Content);
            return new ApiResult<List<Application>>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentType()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/document/type", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDocumentRequestType()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/documentRequest/type", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetFamilyRelationship()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/list/familyRelationship", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<List<RelationshipType>>.Result>(response.Content);
            return new ApiResult<List<RelationshipType>>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCertificateType()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/list/certificateType", Method.GET);
            var response = client.Execute(request);



            var result = JsonConvert.DeserializeObject<ApiResult<List<CertificateType>>.Result>(response.Content);
            return new ApiResult<List<CertificateType>>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetReligion()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/list/religion", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetElectronicAddressType()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/list/electronicAddressType", Method.GET);
            var response = client.Execute(request);



            var result = JsonConvert.DeserializeObject<ApiResult<List<ElectronicAddressType>>.Result>(response.Content);
            return new ApiResult<List<ElectronicAddressType>>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetGender()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/list/gender", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }


        [HttpGet]
        public async Task<IActionResult> GetMaritalStatus()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/list/maritalStatus", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<object>.Result>(response.Content);
            return new ApiResult<object>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetIdentificationType()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/list/identificationType", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<List<IdentificationType>>.Result>(response.Content);
            return new ApiResult<List<IdentificationType>>(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetCity()
        {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/list/city", Method.GET);
            var response = client.Execute(request);

           

            var result = JsonConvert.DeserializeObject<ApiResult<List<City>>.Result>(response.Content);
            return new ApiResult<List<City>>(result);
        }

        public async Task<IActionResult> GetByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/employee/changes/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<Core.Model.Employee>.Result>(resp.Content);
            return new ApiResult<Core.Model.Employee>(result);
        }

        public async Task<IActionResult> GetCertificateByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/employee/certificate/changes/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<Certificate>.Result>(resp.Content);
            return new ApiResult<Certificate>(result);
        }

        public async Task<IActionResult> GetFamilyByInstanceID(string source, string id)
        {
            string employeeID = Session.Id();
            var cl = new Client(Configuration);
            var req = new Request($"api/employee/family/changes/{source}/{id}", Method.GET);

            var resp = cl.Execute(req);

            var result = JsonConvert.DeserializeObject<ApiResult<Family>.Result>(resp.Content);
            return new ApiResult<Family>(result);
        }

        public async Task<IActionResult> GetEmployees()
        {
            //var employeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request("api/employee/get/zz", Method.GET);
            var response = client.Execute(request);
            var employee = JsonConvert.DeserializeObject<ApiResult<List<Employee>>.Result>(response.Content);
            return new ApiResult<List<Employee>>(employee);
        }

        public async Task<IActionResult> GetDepartments()
        {
            var client = new Client(Configuration);
            //var request = new Request("api/employee/departments/get", Method.GET);
            var request = new Request("api/employee/departments/all", Method.GET);
            var response = client.Execute(request);
            var dept = JsonConvert.DeserializeObject<ApiResult<List<Department>>.Result>(response.Content);

            return new ApiResult<List<Department>>(dept);
        }
    }
}