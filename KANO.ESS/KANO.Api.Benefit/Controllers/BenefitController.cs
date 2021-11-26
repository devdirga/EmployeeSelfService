
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace KANO.Api.Benefit.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BenefitController : Controller
    {
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase DB;
        private readonly IConfiguration Configuration;
        private Family _family;
        private MedicalBenefit _medicalBenefit;
        private EmployeeAdapter _employeeAdapter;
        private BenefitAdapter _benefitAdapter;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public BenefitController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _family = new Family(DB, Configuration);
            _medicalBenefit = new MedicalBenefit(DB, Configuration);
            _benefitAdapter = new BenefitAdapter(Configuration);
            _employeeAdapter = new EmployeeAdapter(Configuration);
        }

        [HttpPost("reimburse")]
        public IActionResult GetReimburse([FromBody] GridDateRange param)
        {
            var range = Tools.normalizeFilter(param.Range);
            var medicalBenefits = new List<MedicalBenefit>();
            try
            {                
                medicalBenefits = _medicalBenefit.GetS(param.Username, param.Range);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading medical benefits :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<MedicalBenefit>>.Ok(medicalBenefits);
        }

        [HttpPost("reimburse/save")]
        public IActionResult SaveReimburse([FromForm] MedicalBenefitForm param)
        {
            var updateRequest = new UpdateRequest();
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var data = JsonConvert.DeserializeObject<MedicalBenefit>(param.JsonData);
            data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;

            var oldData = DB.GetCollection<MedicalBenefit>()
                .Find(x => x.AXID == data.AXID && x.Id == data.Id && (x.Status == UpdateRequestStatus.InReview))
                .FirstOrDefault();
            data.RequestDate = Tools.normalize(data.RequestDate);
            try
            {
                var totalAmount = 0.0;
                foreach(var detail in data.Details)
                {                    
                    detail.AXID = Tools.RandomInt()*-1;
                    detail.Attachment.AXID = detail.AXID;
                    detail.Attachment.Filepath = Path.Join(Tools.UploadPathConfiguration(Configuration), detail.Attachment.Filename);
                    totalAmount += detail.Amount;
                }

                data.TotalAmount = totalAmount;
                var AXRequestID = adapter.RequestBenefit(data);
                if (!string.IsNullOrWhiteSpace(AXRequestID))
                {
                    string name = $"self";
                    if (data.Family.AXID > 0) name = data.Family.Name;

                    updateRequest.Create(AXRequestID, data.EmployeeID, UpdateRequestModule.MEDICAL_BENEFIT, $"Medical benefit request for {name}");
                    data.AXRequestID = AXRequestID;                    

                    DB.Save(data);
                    DB.Save(updateRequest);
                    return ApiResult<object>.Ok(data, "Medical benefit has been saved successfully");
                }

                // Send approval notification
                new Notification(Configuration, DB).SendApprovals(data.EmployeeID, data.AXRequestID);

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to update request to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving medical benefit :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpGet("reimburse/delete/{requestID}")]
        public IActionResult DeleteReimburse(string requestID)
        {
            try
            {
                DB.Delete(new MedicalBenefit { Id = requestID });
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error delete medical benefit :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<MedicalBenefit>.Ok("Medical benefit has been deleted successfully ");
        }

        [HttpGet("get/{employeeID}/{axRequestID}")]
        public IActionResult GetByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _medicalBenefit.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<MedicalBenefit>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get travel data :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("list/medicalType")]
        public IActionResult GetMedicalType()
        {            
            return ApiResult<List<string>>.Ok(BenefitAdapter.GetTypes());
        }

        [HttpGet("documenttype")]
        public IActionResult GetDocumentType()
        {
            List<DocumentType> documenttype = new List<DocumentType>(new DocumentType[] {
                new DocumentType{TypeID = 0, Description = "Copy Resep Obat"},
                new DocumentType{TypeID = 1, Description = "Kuitansi Dokter"},
                new DocumentType{TypeID = 2, Description = "Kuitansi Obat"},
                new DocumentType{TypeID = 3, Description = "Laboratorium"},
                new DocumentType{TypeID = 4, Description = "USG"},
                new DocumentType{TypeID = 5, Description = "Rontgen"},
                new DocumentType{TypeID = 6, Description = "Lain-lain"}
            });
            return ApiResult<List<DocumentType>>.Ok(documenttype.ToList());
        }

        [HttpGet("families/{employeeID}")]
        public IActionResult GetFamilies(string employeeID)
        {
            try
            {
                var families = _employeeAdapter.GetFamilies(employeeID);
                return ApiResult<List<Family>>.Ok(families.FindAll(x=>x.Relationship== "Child" || x.Relationship=="Spouse"));
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get family '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpPost("attachment/save")]
        public IActionResult SaveAttachment([FromForm] MedicalFieldAttachmentForm param)
        {
            var oldData = new MedicalBenefitDetail();
            var data = new MedicalBenefitDetail();
            data = JsonConvert.DeserializeObject<MedicalBenefitDetail>(param.JsonData);
            try
            {
                // Generate random when its new
                data.AXID =  Tools.RandomInt() ;
                if (oldData == null)
                {
                    oldData = new MedicalBenefitDetail();
                }

                switch (param.Field)
                {                    
                    case "MedicalBenefit":
                        if (data.Attachment != null)
                        {
                            data.Attachment.Upload(Configuration, oldData.Attachment, param.FileUpload, x => $"{param.Field}_{data.AXID}");
                        }
                        break;
                    default:
                        break;
                }

                return ApiResult<MedicalBenefitDetail>.Ok(data, "Medical field attachment has been stored");
            }
            catch (Exception e)
            {
                return ApiResult<MedicalBenefitDetail>.Error(HttpStatusCode.BadRequest, $"Unable to upload document :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("document/download/{axid}/{axDocumentID}")]
        public IActionResult Download(string axid, string axDocumentID)
        {
            var bytes = new byte[]{};
            var filepath = "";
            
            try
            {                
                var medicalbenefit = _benefitAdapter.GetByRecID(long.Parse(axid));
                if (medicalbenefit == null)
                {
                    throw new Exception($"Unable to find benefit with rec id {axid}");
                }

                var document = medicalbenefit.Details.Find(x => x.AXID == long.Parse(axDocumentID));
                if (document == null) {
                    throw new Exception($"Unable to find document with rec id {axid}");
                }

                if (!document.Attachment.Accessible || string.IsNullOrWhiteSpace(document.Attachment.Filepath)) {
                    throw new Exception($"Unable to find accessible file");
                }

                bytes = document.Attachment.Download();
                var filename = document.Attachment.Filename;
                
                return File(bytes, "application/force-download", filename);
            }
            catch (Exception e)
            {
                throw e;
            }
        }
        
        [HttpGet("document/download/{employeeID}/{axid}/{axDocumentID}")]
        public IActionResult Download(string employeeID, string axid, string axDocumentID)
        {
            var bytes = new byte[]{};
            var filepath = "";
            
            try
            {
                var medicalbenefit = DB.GetCollection<MedicalBenefit>().Find(x => x.EmployeeID == employeeID && x.AXID == long.Parse(axid)).FirstOrDefault();
                if (medicalbenefit == null)
                {
                    throw new Exception($"Unable to find benefit with rec id {axid}");
                }

                var document = medicalbenefit.Details.Find(x => x.AXID == long.Parse(axDocumentID));
                if (document == null) {
                    throw new Exception($"Unable to find document with rec id {axid}");
                }

                if (!document.Attachment.Accessible || string.IsNullOrWhiteSpace(document.Attachment.Filepath)) {
                    throw new Exception($"Unable to find accessible file");
                }

                bytes = document.Attachment.Download();
                var filename = document.Attachment.Filename;
                return File(bytes, "application/force-download", filename);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("limit/list")]
        public IActionResult GetListLimit()
        {
            try
            {
                var adapter = new BenefitAdapter(Configuration);
                var results = adapter.GetListLimit();
                return ApiResult<List<BenefitLimit>>.Ok(results);
            }
            catch (Exception e)
            {
                return ApiResult<List<BenefitLimit>>.Error(HttpStatusCode.BadRequest, $"Error while loading leave :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("limit/employee/{employeeID}")]
        public IActionResult GetLimit(string employeeID)
        {
            try
            {
                var adapter = new BenefitAdapter(Configuration);
                var results = adapter.GetLimit(employeeID);
                return ApiResult<EmployeeBenefitLimit>.Ok(results);
            }
            catch (Exception e)
            {
                return ApiResult<EmployeeBenefitLimit>.Error(HttpStatusCode.BadRequest, $"Error while loading leave :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("requestStatus/{employeeID}")]
        public IActionResult CheckRequestStatus(string employeeID)
        {
            var request = new UpdateRequest(DB);
            try
            {                
                var result = this.DB.GetCollection<UpdateRequest>()
                    .Find(x => x.EmployeeID == employeeID && x.Module == UpdateRequestModule.MEDICAL_BENEFIT && x.Status==UpdateRequestStatus.InReview).FirstOrDefault();
                return ApiResult<UpdateRequest>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable get current mefdical benefit request update status :\n{e.Message}");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
            }

    }
}