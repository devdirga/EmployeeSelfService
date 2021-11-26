using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace KANO.Api.Employee.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Core.Model.Employee _employee;
        private User _user;
        private Family _family;
        private Identification _identification;
        private BankAccount _bankAccount;
        private Tax _tax;
        private ElectronicAddress _electronicAddress;
        private Certificate _certificate;
        private Document _document;
        private Address _address;
        private UpdateRequest _updateRequest;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public EmployeeController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;

            _employee = new Core.Model.Employee(DB, Configuration);
            _user = new User(DB, Configuration);
            _family = new Family(DB, Configuration);
            _identification = new Identification(DB, Configuration);
            _bankAccount = new BankAccount(DB, Configuration);
            _tax = new Tax(DB, Configuration);
            _electronicAddress = new ElectronicAddress(DB, Configuration);
            _certificate = new Certificate(DB, Configuration);
            _document = new Document(DB, Configuration);
            _address = new Address(DB, Configuration);
            _updateRequest = new UpdateRequest(DB, Configuration);
        }

        /**
         * Employee
         */

        [HttpGet("{employeeID}")]
        public IActionResult Get(string employeeID)
        {           
            Core.Model.Employee data;
            try 
            {             
                var adapter = new EmployeeAdapter(Configuration);
                data = adapter.GetDetail(employeeID);                
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<Core.Model.Employee>.Ok(data);

        }

        [HttpGet("get/authInfo/{employeeID}")]
        public IActionResult GetAuthInfo(string employeeID)
        {            
            Core.Model.Employee data;
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                data = adapter.Get(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<Core.Model.Employee>.Ok(data);
        }

        [HttpGet("get/fullDetail/{employeeID}")]
        public IActionResult GetFullInfo(string employeeID)
        {
            try
            {
                var result = _employee.GetFullDetail(employeeID);
                return ApiResult<EmployeeResult>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }                                                          
        }

        [HttpGet("get/detail/{employeeID}")]
        public IActionResult GetDetail(string employeeID)
        {
            try
            {
                var result = _employee.GetDetail(employeeID);
                return ApiResult<EmployeeResult>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpGet("get/subordinate/{employeeID}")]
        public IActionResult GetSubordinate(string employeeID)
        {
            var subordinates = new List<Core.Model.Employee>();
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                subordinates = adapter.GetSubordinate(employeeID);
                return ApiResult<List<Core.Model.Employee>>.Ok(subordinates);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee subordinate '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("check/subordinate/{employeeID}")]
        public IActionResult CheckSubordinate(string employeeID)
        {
            bool hasSubordinates;
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                hasSubordinates = adapter.HasSubordinate(employeeID);
                return ApiResult<bool>.Ok(hasSubordinates);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee subordinate '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

        }


        [HttpGet("user/{employeeID}")]
        public IActionResult GetESSUser(string employeeID)
        {
            try
            {
                var result = _user.GetEmployeeUser(employeeID);
                return ApiResult<User>.Ok(result, "");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get user for employee '{employeeID}' :\n{Format.ExceptionString(e)}");
            }   
        }

        [HttpGet("get/{employeeID}/{instanceID}")]
        public IActionResult GetEmployee(string employeeID, string instanceID)
        {
            try
            {
                var result = _employee.GetByAXRequestID(employeeID, instanceID);
                return ApiResult<Core.Model.Employee>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to employee '{employeeID}-{instanceID}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("changes/{employeeID}/{instanceID}")]
        public IActionResult GetEmployeeInternal(string employeeID, string instanceID)
        {
            try
            {
                var result = _employee.GetByAXRequestID(employeeID, instanceID, true);
                var data = result.Employee.UpdateRequest ?? new Core.Model.Employee();

                data.Address = result.Address.UpdateRequest;

                foreach (var d in result.ElectronicAddresses) {
                    if (d.UpdateRequest != null)
                    {
                        data.ElectronicAddresses.Add(d.UpdateRequest);
                    }
                }

                foreach (var d in result.Identifications)
                {
                    if (d.UpdateRequest != null)
                    {
                        data.Identifications.Add(d.UpdateRequest);
                    }
                }

                foreach (var d in result.BankAccounts)
                {
                    if (d.UpdateRequest != null)
                    {
                        data.BankAccounts.Add(d.UpdateRequest);
                    }
                }

                foreach (var d in result.Taxes)
                {
                    if (d.UpdateRequest != null)
                    {
                        data.Taxes.Add(d.UpdateRequest);
                    }
                }

                data.Reason = result.UpdateRequest.Description;

                return ApiResult<Core.Model.Employee>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to employee '{employeeID}-{instanceID}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody] Core.Model.Employee employee) {
            try
            {               
                foreach (var d in employee.BankAccounts) {
                    if (!Tools.Equals(d.NewData, d.OldData))
                    {
                        d.EmployeeID = employee.EmployeeID;
                        _bankAccount.Update(d);   
                    }                    
                }

                foreach (var d in employee.Identifications)
                {
                    if (!Tools.Equals(d.NewData, d.OldData))
                    {
                        d.EmployeeID = employee.EmployeeID;
                        _identification.Update(d);
                    }                    
                    
                }

                foreach (var d in employee.Taxes)
                {
                    if (!Tools.Equals(d.NewData, d.OldData))
                    {
                        d.EmployeeID = employee.EmployeeID;
                        _tax.Update(d);
                    }
                }

                foreach (var d in employee.ElectronicAddresses)
                {
                    if (!Tools.Equals(d.NewData, d.OldData))
                    {
                        d.EmployeeID = employee.EmployeeID;
                        _electronicAddress.Update(d);
                    }
                }

                if (employee.Address != null) {
                    if (!Tools.Equals(employee.Address.NewData, employee.Address.OldData))
                    { 
                        _address.Update(employee.Address, true);
                    }
                }

                employee.Religion = employee.Religion;
                employee.Gender = employee.Gender;
                employee.MaritalStatus = employee.MaritalStatus;
                employee.BankAccounts = new List<BankAccount>();
                employee.Identifications = new List<Identification>();

                _employee.Update(employee);
                return ApiResult<User>.Ok("Employee request draft has been saved");
            }
            catch (Exception e) 
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to request employee update :\n{Format.ExceptionString(e)}");
            }            
        }

        [HttpGet("discardChange/{employeeID}")]
        public IActionResult DiscardChange(string employeeID)
        {
            try
            {
                _employee.Discard(employeeID);
                _identification.Discard(employeeID);
                _bankAccount.Discard(employeeID);
                _tax.Discard(employeeID);
                _address.Discard(employeeID);
                _electronicAddress.Discard(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to discard employee data request :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<object>.Ok($"Employee data request has been discarded");
        }        

        [HttpPost("updateRequest/resume")]
        public IActionResult UpdateRequestResume([FromBody] UpdateRequest updateRequest)
        {            
            updateRequest.Module = UpdateRequestModule.EMPLOYEE_RESUME;

            if (string.IsNullOrWhiteSpace(updateRequest.Notes))
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Notes field could not be empty");

            if (string.IsNullOrWhiteSpace(updateRequest.EmployeeID))
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Employee ID field could not be empty");

            var employeeID = updateRequest.EmployeeID;
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var tasks = new List<Task<TaskRequest<object>>>();
            try
            {                                                                             
                // Fetch employee
                EmployeeResult employee = new EmployeeResult();
                tasks.Add(Task.Run(() =>
                {
                    var result = _employee.GetDetail(employeeID);                    
                    return TaskRequest<object>.Create("employee", result);
                }));                               

                // Fetch identification
                var identifications = new List<IdentificationResult>();
                tasks.Add(Task.Run(() => {
                    var result = _identification.GetS(employeeID);
                    return TaskRequest<object>.Create("identification", result);
                }));

                // Fetch bank account
                var bankAccounts = new List<BankAccountResult>();
                tasks.Add(Task.Run(() => {
                    var result = _bankAccount.GetS(employeeID);
                    return TaskRequest<object>.Create("bankAccount", result);
                }));

                // Fetch tax
                var taxes = new List<TaxResult>();
                tasks.Add(Task.Run(() => {
                    var result = _tax.GetS(employeeID);
                    return TaskRequest<object>.Create("tax", result);
                }));

                // Fetch address
                var address = new AddressResult();
                tasks.Add(Task.Run(() => {
                    var result = _address.Get(employeeID);
                    return TaskRequest<object>.Create("address", result);
                }));

                // Fetch electronic address
                var electronicAddresses = new List<ElectronicAddressResult>();
                tasks.Add(Task.Run(() => {
                    var result = _electronicAddress.GetS(employeeID);
                    return TaskRequest<object>.Create("electronicAddress", result);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable request update to server '{employeeID}' :\n{Format.ExceptionString(e)}");
                }

                // Combine result                
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var r in t.Result)
                        switch (r.Label)
                        {
                            case "employee":
                                employee = (EmployeeResult)r.Result;
                                break;                            
                            case "identification":
                                identifications = (List<IdentificationResult>)r.Result;
                                break;
                            case "bankAccount":
                                bankAccounts = (List<BankAccountResult>)r.Result;
                                break;
                            case "tax":
                                taxes = (List<TaxResult>)r.Result;
                                break;
                            case "address":
                                address = (AddressResult)r.Result;
                                break;
                            case "electronicAddress":
                                electronicAddresses = (List<ElectronicAddressResult>)r.Result;
                                break;
                            default:
                                break;
                        }
                }

                var AXRequestID = adapter.RequestEmployee(employee, identifications, bankAccounts, taxes, electronicAddresses, address, updateRequest.Notes);
                if (string.IsNullOrWhiteSpace(AXRequestID)) {
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to request update to AX server");
                }                

                tasks.Clear();

                // Save Update Request
                tasks.Add(Task.Run(() =>
                {
                    updateRequest.Description = $"Request Update Resume - {employee.Employee.EmployeeName} ({employee.Employee.EmployeeID})";
                    updateRequest.AXRequestID = AXRequestID;
                    DB.Save(updateRequest);

                    return TaskRequest<object>.Create("update_request", true);
                }));


                // Update Employee
                tasks.Add(Task.Run(() =>
                {
                    var result = _employee.SetAXRequestID(employeeID, AXRequestID);                                        
                    return TaskRequest<object>.Create("employee", result);
                }));                

                // Update Identification
                tasks.Add(Task.Run(() =>
                {
                    var result = _identification.SetAXRequestID(employeeID, AXRequestID);
                    return TaskRequest<object>.Create("identification", result);
                }));

                // Update Bank Account
                tasks.Add(Task.Run(() =>
                {
                    var result = _bankAccount.SetAXRequestID(employeeID, AXRequestID);
                    return TaskRequest<object>.Create("bankAccount", result);
                }));

                // Update Tax
                tasks.Add(Task.Run(() =>
                {
                    var result = _tax.SetAXRequestID(employeeID, AXRequestID);
                    return TaskRequest<object>.Create("tax", result);
                }));

                // Update Address
                tasks.Add(Task.Run(() =>
                {
                    var result = _address.SetAXRequestID(employeeID, AXRequestID);
                    return TaskRequest<object>.Create("address", result);
                }));

                // Update Electronic Address
                tasks.Add(Task.Run(() =>
                {
                    var result = _electronicAddress.SetAXRequestID(employeeID, AXRequestID);
                    return TaskRequest<object>.Create("electronicAddress", result);
                }));


                t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to update employee data '{employeeID}' :\n{Format.ExceptionString(e)}");
                }

                // Combine result                
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var r in t.Result) {
                        if (!(bool)r.Result) {
                            Console.WriteLine($"No record acknowledged while updating '${r.Label}' for employee '${employeeID}' with request id '${AXRequestID}'");
                        }
                    }                        
                }

                // Send approval notification
                new Notification(Configuration, DB).SendApprovals(employeeID, AXRequestID);

                return ApiResult<object>.Ok("Employee data has been submitted. You will be notifed once the update request has been approved/rejected.");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to request employee update :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("requestStatus/resume/{employeeID}")]
        public IActionResult CheckRequestStatus(string employeeID) {
            var request = new UpdateRequest(DB);
            try
            {
                var result = request.Current(employeeID, UpdateRequestModule.EMPLOYEE_RESUME);
                return ApiResult<UpdateRequest>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable get current employee request update status :\n{e.Message}");
            }
        }
        

        /**
         * Employee Family
         */       

        [HttpGet("families/{employeeID}")]
        public IActionResult GetFamilies(string employeeID)

        {
            try
            {
                var result = _family.GetS(employeeID);
                return ApiResult<List<FamilyResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get family '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
               
        }

        [HttpGet("family/changes/{employeeID}/{instanceID}")]
        public IActionResult GetFamilyInternal(string employeeID, string instanceID)
        {
            try
            {
                var result = _family.GetByAXRequestID(employeeID, instanceID);
                var updateRequest = _updateRequest.Get(employeeID, instanceID);
                if (result != null)
                {
                    result.Reason = updateRequest?.Notes;
                }

                return ApiResult<Family>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get family '{employeeID}-{instanceID}' :\n{Format.ExceptionString(e)}");
            }
        }


        [HttpPost("family/create")]
        public IActionResult CreateFamily([FromForm] FamilyForm param, ActionType action = ActionType.Create)
        {
            var strAction = Enum.GetName(typeof(ActionType), action);
            var updateRequest = new UpdateRequest();
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var data = JsonConvert.DeserializeObject<Family>(param.JsonData);

            data.Gender = data.Gender;
            data.Religion = data.Religion;
            data.Relationship = data.Relationship;
            data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;     
            
            var oldData = _family.Get(data.EmployeeID, data.AXID, true).UpdateRequest;                
            try
            {
                data.Upload(Configuration, oldData, param.FileUpload, x => String.Format("Families_{0}_{1}", data.Relationship, x.EmployeeID));

                // Create Update Request
                data.Purpose = Tools.ActionToPurpose(action);
                var AXRequestID = adapter.RequestFamily(data, param.Reason);
                if (!string.IsNullOrWhiteSpace(AXRequestID))
                {
                    updateRequest.Create(AXRequestID, data.EmployeeID, UpdateRequestModule.EMPLOYEE_FAMILY, $"{Format.TitleCase(strAction)} family member ({data.Name})", param.Reason);
                    updateRequest.AXRequestID = AXRequestID;
                    DB.Save(updateRequest);

                    data.AXRequestID = AXRequestID;                    
                    data.Action = action;
                    DB.Save(data);

                    // Send approval notification
                    new Notification(Configuration, DB).SendApprovals(data.EmployeeID, data.AXRequestID);

                    return ApiResult<object>.Ok($"family draft '{strAction}' request has been saved");
                }
                
                

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Unable to request update to AX");
            }
            catch (Exception e) 
            {
                return ApiResult<object>.Ok($"Family draft '{strAction}' is failed :\n{e.Message}");
            }            
        }


        [HttpPost("family/update")]
        public IActionResult UpdateFamily([FromForm] FamilyForm param)
        {
            return this.CreateFamily(param, ActionType.Update);
        }

        [HttpPost("family/delete")]
        public IActionResult DeleteFamily([FromBody] DeleteForm param)
        {
            var update = DB.GetCollection<Family>()
                        .Find(x => x.EmployeeID == param.EmployeeID && x.AXID == long.Parse(param.Id) && (x.Status == UpdateRequestStatus.InReview))
                        .FirstOrDefault();

            if (update == null) {
                var adapter = new EmployeeAdapter(Configuration);
                var data = adapter.GetFamilies(param.EmployeeID);
                update = data.Find(x => x.AXID == long.Parse(param.Id));                
            }            

            if (update == null)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to request '{ActionType.Delete.ToString()}' family member");
            }


            var _param = new FamilyForm();
            _param.JsonData = JsonConvert.SerializeObject(update);
            _param.Reason = param.Reason;
            return this.CreateFamily(_param, ActionType.Delete);
            
        }

        [HttpGet("family/discardChange/{requestID}")]
        public IActionResult DiscardFamilyChange(string requestID="")
        {
            try
            {
                _family.Discard(requestID);
            }
            catch (Exception e) 
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to discard family request :\n{Format.ExceptionString(e)}");
            }
            
            return ApiResult<object>.Ok($"Employee family data request has been discarded");
        }

        [HttpGet("family/attachment/download/{employeeID}/{axRequestID}")]
        public IActionResult DownloadFamilyAttachment(string employeeID, string axRequestID)
        {
            var family = _family.GetByAXRequestID(employeeID, axRequestID);

            // Download the data
            try
            {
                if (family.Accessible)
                {
                    var bytes = family.Download();
                    return File(bytes, "application/force-download", Path.GetFileName(family.Filepath));
                }
                else
                {
                    throw new Exception("Unable to find file path on database");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("family/document/download/{employeeID}/{axid}")]
        public IActionResult DownloadAddress(string employeeID, long axid)
        {
            var result = _family.Get(employeeID, axid, true);
            if (result.UpdateRequest == null) {
                throw new Exception($"Unable to find employee family {employeeID} - {axid}");
            }
            var family = result.UpdateRequest;

            // Download the data
            try
            {
                var bytes = family.Download();
                return File(bytes, "application/force-download", Path.GetFileName(family.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /**
         * Employee Document
         */

        [HttpGet("documents/{employeeID}")]
        public IActionResult GetDocuments(string employeeID)
        {
            try
            {
                var result = _document.GetS(employeeID);
                return ApiResult<List<DocumentResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get document '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }        

        [HttpPost("document/create")]
        public IActionResult CreateDocument([FromForm] DocumentForm param, ActionType action = ActionType.Create)
        {
            var adapter = new EmployeeAdapter(Configuration);
            var strAction = Enum.GetName(typeof(ActionType), action);            
            var document = JsonConvert.DeserializeObject<Document>(param.JsonData);
            var updateRequest = new UpdateRequest();
            try
            {
                if (document.AXID == -1) document.AXID = Tools.RandomInt() * -1;
                
                document.Action = action;

                // Get old document that is stored in DB
                Document oldDocument = null;
                if (!string.IsNullOrWhiteSpace(document.Id)){
                    oldDocument = DB.GetCollection<Document>().Find(x => x.Id == document.Id).FirstOrDefault();                    
                }

                // Compare new document and old document to see if it needs to do uploading
                var needUpload = (param.FileUpload != null && oldDocument == null)
                                || (param.FileUpload != null && oldDocument != null && !System.IO.File.Exists(oldDocument.Filepath))
                                || (param.FileUpload != null && oldDocument != null && System.IO.File.Exists(oldDocument.Filepath) && document.Checksum != Tools.CalculateMD5(oldDocument.Filepath));

                // Directory preparation
                var uploadDirectory = Tools.UploadPathConfiguration(Configuration);                

                if (needUpload)
                {
                    // Currently we limit only one file upload
                    var file = param.FileUpload.FirstOrDefault();                    

                    // New file path and name preparation
                    var newFilename = string.Format("{0}_{1}_{2}{3}",
                        document.DocumentType,
                        document.EmployeeID,
                        DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"),
                        Path.GetExtension(file.FileName)
                    );
                    var newFilepath = Path.Combine(uploadDirectory, newFilename);

                    // Upload file
                    using (var fileStream = new FileStream(newFilepath, FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    // We do not delete old, just add OLD behind the old file name 
                    if (oldDocument != null)
                    {
                        var oldFilepath = Path.Combine(uploadDirectory, oldDocument.Filepath);
                        if (System.IO.File.Exists(oldFilepath))
                        {
                            var newFileName = string.Format("OLD_{0}{1}", Path.GetFileNameWithoutExtension(oldFilepath), Path.GetExtension(oldFilepath));
                            var newFilePath = Path.Combine(uploadDirectory, newFileName);
                            System.IO.File.Move(oldFilepath, newFilePath);
                        }
                    }

                    document.Filepath = newFilepath;

                }
                else if (oldDocument != null) 
                {
                    // If it doesn't need to be uploaded and the document type change. We change the file name as {NEW_DOCUMENT_TYPE}_{EMPLOYEE_ID}
                    if (document.DocumentType != oldDocument.DocumentType)
                    {
                        var token = oldDocument.Filename.Split($"_{document.EmployeeID}_");
                        var newFilename = string.Format("{0}_{1}_{2}",
                            document.DocumentType,
                            document.EmployeeID,
                            token[1]
                        );
                        var newFilepath = Path.Combine(uploadDirectory, newFilename);

                        System.IO.File.Move(oldDocument.Filepath, newFilepath);
                        document.Filepath = newFilepath;
                        document.Filename = newFilename;
                    }
                    else if (document.Action != oldDocument.Action && document.Action == ActionType.Delete)
                    {
                        if (System.IO.File.Exists(oldDocument.Filepath)) {
                            System.IO.File.Delete(oldDocument.Filepath);
                            document.Filepath = "";
                        }
                    }
                    else
                    {
                        document.Filepath = oldDocument.Filepath;
                    }
                }

                // Create update request
                var AXRequestID = adapter.UpdateDocument(document);
                if (!string.IsNullOrWhiteSpace(AXRequestID))
                {
                    updateRequest.Create(AXRequestID, document.EmployeeID, UpdateRequestModule.EMPLOYEE_DOCUMENT, $"{Format.TitleCase(strAction)} document {document.DocumentType} ({document.Description})");
                    document.AXRequestID = AXRequestID;

                    DB.Save(document);
                    return ApiResult<Document>.Ok($"Employee document data '{strAction}' request has been submitted");
                }

                return ApiResult<Document>.Error(HttpStatusCode.BadRequest, "Unable to request update to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, Format.ExceptionString(e));
            }
            
        }


        [HttpPost("document/update")]
        public IActionResult UpdateDocument([FromForm] DocumentForm param, ActionType action = ActionType.Update)
        {
            return this.CreateDocument(param, ActionType.Update);
        }

        [HttpGet("document/delete/{employeeID}/{documentID}")]
        public IActionResult DeleteDocument(string employeeID, long documentID)
        {

            var document = DB.GetCollection<Document>()
                .Find(x => x.EmployeeID == employeeID && x.AXID == documentID && (x.Status == UpdateRequestStatus.InReview))
                .FirstOrDefault();

            if (document == null)
            {
                var adapter = new EmployeeAdapter(Configuration);
                var data = adapter.GetDocuments(employeeID);
                document = data.Find(x => x.AXID == documentID);
            }

            if (document == null)
            {
                return ApiResult<Document>.Error(HttpStatusCode.BadRequest, $"Unable to request '{ActionType.Delete.ToString()}' document");
            }

            document.EmployeeID = employeeID;

            return this.CreateDocument(new DocumentForm { JsonData = JsonConvert.SerializeObject(document) }, ActionType.Delete);
        }

        [HttpGet("document/discardChange/{requestID}")]
        public IActionResult DiscardDocument(string requestID = "")
        {
            try
            {
                _document.Discard(requestID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to discard document request :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<object>.Ok($"Employee document data request has been discarded");
        }

        [HttpGet("document/download/{employeeID}/{documentID}")]
        public IActionResult DownloadDocument(string employeeID, long documentID)
        {
            var result = new DocumentResult();
            var tasks = new List<Task<TaskRequest<Document>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(Configuration);
                var document = adapter.GetDocuments(employeeID)
                                    .Find(x => x.AXID == documentID);
                return TaskRequest<Document>.Create("AX", document);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var document = DB.GetCollection<Document>()
                                .Find(x => x.EmployeeID == employeeID && x.AXID == documentID && (x.Status == UpdateRequestStatus.InReview))
                                .FirstOrDefault();
                return TaskRequest<Document>.Create("DB", document);
            }));

            // Run process concurently
            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                throw e;
            }

            // Combine result
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Document = r.Result;
                    else
                        result.UpdateRequest = r.Result;
            }
            else 
            {
                throw new Exception("Unable to get file");
            }

            // Download the data
            try
            {
                var documentUpdateRequest = result.UpdateRequest;
                var document = result.Document;
            
                var filepath = "";
                if (documentUpdateRequest != null && !string.IsNullOrWhiteSpace(documentUpdateRequest.Filepath))
                {
                    filepath = documentUpdateRequest.Filepath;
                }
                else if (document != null && !string.IsNullOrWhiteSpace(document.Filepath))
                {
                    filepath = document.Filepath;
                }
                else 
                {
                    throw new Exception("Unable to find file path on database");
                }
            
                var bytes = System.IO.File.ReadAllBytes(filepath);
                return File(bytes, "application/force-download", Path.GetFileName(filepath));
            }
            catch (Exception e)
            {                
                throw e;
            }                        
        }

        [HttpGet("address/{employeeID}")]
        public IActionResult GetAddress(string employeeID)
        {
            try
            {
                var result = _address.Get(employeeID);
                return ApiResult<AddressResult>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get address '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("address/save")]
        public IActionResult SaveAddress([FromForm] AddressForm param)
        {
            Address oldData = new Address();
            Address data = new Address();

            data = JsonConvert.DeserializeObject<Address>(param.JsonData);
            oldData = _address.Get(data.EmployeeID, "", true).UpdateRequest;

            try
            {
                // Generate random when its new
                data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;
                data.Upload(Configuration, oldData, param.FileUpload, x => $"Address_{data.AXID}_{x.EmployeeID}");
                DB.Save(data);
                return ApiResult<Address>.Ok(data, $"Employee field attachment has been stored");
            }
            catch (Exception e)
            {
                return ApiResult<Address>.Error(HttpStatusCode.BadRequest, $"Unable to upload document :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("address/download/{employeeID}/{addressID}")]
        public IActionResult DownloadAddress(string employeeID, string addressID)
        {            
            var result = _address.Get(employeeID, long.Parse(addressID));
            var address = (result.UpdateRequest != null) ? result.UpdateRequest : result.Address;

            // Download the data
            try
            {
                var bytes = address.Download();
                return File(bytes, "application/force-download", Path.GetFileName(address.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("address/download/{id}")]
        public IActionResult DownloadAddress(string id)
        {
            var result = _address.GetByID(id);

            // Download the data
            try
            {
                var bytes = result.Download();
                return File(bytes, "application/force-download", Path.GetFileName(result.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPost("attachment/save")]
        public IActionResult SaveAttachment([FromForm] EmployeeFieldAttachmentForm param)
        {
            var oldData = new Core.Model.Employee();
            var data = new Core.Model.Employee();

            data = JsonConvert.DeserializeObject<Core.Model.Employee>(param.JsonData);
            oldData = _employee.GetDetail(data.EmployeeID).UpdateRequest;

            try
            {
                // Generate random when its new
                data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;
                if (oldData == null) 
                {
                    oldData = new Core.Model.Employee();
                }
                     
                switch (param.Field)
                {
                    case "IsExpartiarte":
                        if (data.IsExpartriateAttachment != null)
                        {
                            data.MaritalStatusAttachment = oldData.MaritalStatusAttachment;
                            data.IsExpartriateAttachment.Upload(Configuration, oldData.IsExpartriateAttachment, param.FileUpload, x => $"{param.Field}_{data.EmployeeID}");
                            DB.Save(data);
                        }
                        break;
                    case "MaritalStatus":
                        if (data.MaritalStatusAttachment != null)
                        {
                            data.IsExpartriateAttachment = oldData.IsExpartriateAttachment;
                            data.MaritalStatusAttachment.Upload(Configuration, oldData.MaritalStatusAttachment, param.FileUpload, x => $"{param.Field}_{data.EmployeeID}");
                            DB.Save(data);
                        }
                        break;
                    default:
                        break;
                }
                
                return ApiResult<Core.Model.Employee>.Ok(data, $"Employee field attachment has been stored");
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Employee>.Error(HttpStatusCode.BadRequest, $"Unable to upload document :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("attachment/download/{field}/{employeeID}/{axRequestID}")]
        public IActionResult DownloadAttachment(string field, string employeeID)
        {
            var result = _employee.GetDetail(employeeID);
            var employee = (result.UpdateRequest != null) ? result.UpdateRequest : result.Employee;

            // Download the data
            try
            {
                FieldAttachment fieldAttachment = null;
                switch (field)
                {
                    case "IsExpartiarte":
                        fieldAttachment = employee.IsExpartriateAttachment;
                        break;
                    case "MaritalStatus":
                        fieldAttachment = employee.MaritalStatusAttachment;
                        break;
                    default:
                        break;
                }

                if (fieldAttachment != null)
                {
                    var bytes = fieldAttachment.Download();
                    return File(bytes, "application/force-download", Path.GetFileName(fieldAttachment.Filepath));
                }
                throw new Exception("Unable to find file");                
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpGet("attachment/download/{field}/{id}")]
        public IActionResult DownloadAttachmentByID(string field, string id)
        {
            var result = _employee.GetByID(id);
            // Download the data
            try
            {
                FieldAttachment fieldAttachment = null;
                switch (field)
                {
                    case "IsExpartiarte":
                        fieldAttachment = result.IsExpartriateAttachment;
                        break;
                    case "MaritalStatus":
                        fieldAttachment = result.MaritalStatusAttachment;
                        break;
                    default:
                        break;
                }

                if (fieldAttachment != null)
                {
                    var bytes = fieldAttachment.Download();
                    return File(bytes, "application/force-download", Path.GetFileName(fieldAttachment.Filepath));
                }
                throw new Exception("Unable to find file");
            }
            catch (Exception)
            {
                throw;
            }            
        }


        [HttpGet("identification/{employeeID}")]
        public IActionResult GetIdentifications(string employeeID)
        {
            try
            {
                var result = _identification.GetS(employeeID);
                return ApiResult<List<IdentificationResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get identification '{employeeID}' :\n{Format.ExceptionString(e)}");
            }            
        }

        [HttpPost("identification/save")]
        public IActionResult SaveIdentification([FromForm] IdentificationForm param)
        {
            Identification oldData = new Identification();
            Identification data = new Identification();
            
            data = JsonConvert.DeserializeObject<Identification>(param.JsonData);            
            oldData = _identification.Get(data.EmployeeID, data.AXID).UpdateRequest;            

            try
            {
                // Generate random when its new
                data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;
                data.Upload(Configuration, oldData, param.FileUpload, x => $"Identities_{data.Type}_{x.EmployeeID}");
                DB.Save(data);
                return ApiResult<Identification>.Ok(data, $"Employee field attachment has been stored");
            }
            catch (Exception e)
            {
                return ApiResult<Identification>.Error(HttpStatusCode.BadRequest, $"Unable to upload document :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("identification/download/{employeeID}/{identificationID}")]
        public IActionResult DownloadIdentification(string employeeID, long identificationID)
        {
            var result = _identification.Get(employeeID, identificationID);
            var identification = (result.UpdateRequest != null)? result.UpdateRequest : result.Identification;

            // Download the data
            try
            {
                var bytes = identification.Download();
                return File(bytes, "application/force-download", Path.GetFileName(identification.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("identification/download/{id}")]
        public IActionResult DownloadIdentification(string id)
        {
            var result = _identification.GetByID(id);

            // Download the data
            try
            {
                var bytes = result.Download();
                return File(bytes, "application/force-download", Path.GetFileName(result.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("bankAccount/{employeeID}")]
        public IActionResult GetBankAccounts(string employeeID)
        {            
            try
            {
                var result = _bankAccount.GetS(employeeID);
                return ApiResult<List<BankAccountResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get bank account '{employeeID}' :\n{Format.ExceptionString(e)}");
            }                      
        }

        [HttpPost("bankAccount/save")]
        public IActionResult SaveBankAccount([FromForm] BankAccountForm param)
        {
            BankAccount oldData = new BankAccount();
            BankAccount data = new BankAccount();

            data = JsonConvert.DeserializeObject<BankAccount>(param.JsonData);
            oldData = _bankAccount.Get(data.EmployeeID, data.AXID, true).UpdateRequest;

            try
            {
                // Generate random when its new
                data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;
                data.Upload(Configuration, oldData, param.FileUpload, x => $"BankAccount_{data.Type}_{x.EmployeeID}");
                DB.Save(data);
                return ApiResult<BankAccount>.Ok(data, $"Employee field attachment has been stored");
            }
            catch (Exception e)
            {
                return ApiResult<BankAccount>.Error(HttpStatusCode.BadRequest, $"Unable to upload document :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("bankAccount/download/{employeeID}/{bankAccountID}")]
        public IActionResult DownloadBankAccount(string employeeID, long bankAccountID)
        {
            var result = _bankAccount.Get(employeeID, bankAccountID);
            var bankAccount = (result.UpdateRequest != null) ? result.UpdateRequest : result.BankAccount;

            // Download the data
            try
            {
                var bytes = bankAccount.Download();
                return File(bytes, "application/force-download", Path.GetFileName(bankAccount.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("bankAccount/download/{id}")]
        public IActionResult DownloadBankAccount(string id)
        {
            var result = _bankAccount.GetByID(id);

            // Download the data
            try
            {
                var bytes = result.Download();
                return File(bytes, "application/force-download", Path.GetFileName(result.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("tax/{employeeID}")]
        public IActionResult GetTaxes(string employeeID)
        {
            try
            {
                var result = _tax.GetS(employeeID);
                return ApiResult<List<TaxResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get tax '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("tax/save")]
        public IActionResult SaveTax([FromForm] TaxForm param)
        {
            Tax oldData = new Tax();
            Tax data = new Tax();

            data = JsonConvert.DeserializeObject<Tax>(param.JsonData);
            oldData = _tax.Get(data.EmployeeID, data.AXID, true).UpdateRequest;

            try
            {
                // Generate random when its new
                data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;
                data.Upload(Configuration, oldData, param.FileUpload, x => $"Tax_{data.Type}_{x.EmployeeID}");
                DB.Save(data);
                return ApiResult<Tax>.Ok(data, $"Employee field attachment has been stored");
            }
            catch (Exception e)
            {
                return ApiResult<Tax>.Error(HttpStatusCode.BadRequest, $"Unable to upload document :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("tax/download/{employeeID}/{taxID}")]
        public IActionResult DownloadTax(string employeeID, long taxID)
        {
            var result = _tax.Get(employeeID, taxID);
            var tax = (result.UpdateRequest != null) ? result.UpdateRequest : result.Tax;

            // Download the data
            try
            {
                var bytes = tax.Download();
                return File(bytes, "application/force-download", Path.GetFileName(tax.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("tax/download/{id}")]
        public IActionResult DownloadTax(string id)
        {
            var result = _tax.GetByID(id);

            // Download the data
            try
            {
                var bytes = result.Download();
                return File(bytes, "application/force-download", Path.GetFileName(result.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("electronicAddress/{employeeID}")]
        public IActionResult GetElectronicAddresses(string employeeID)
        {
            try
            {
                var result = _electronicAddress.GetS(employeeID);
                return ApiResult<List<ElectronicAddressResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get electronicAddress '{employeeID}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("electronicAddress/save")]
        public IActionResult SaveElectronicAddress([FromForm] ElectronicAddressForm param)
        {
            ElectronicAddress oldData = new ElectronicAddress();
            ElectronicAddress data = new ElectronicAddress();

            data = JsonConvert.DeserializeObject<ElectronicAddress>(param.JsonData);
            oldData = _electronicAddress.Get(data.EmployeeID, data.AXID, true).UpdateRequest;

            try
            {
                // Generate random when its new
                data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;
                data.Upload(Configuration, oldData, param.FileUpload, x => $"ElectronicAddress_{data.Type}_{x.EmployeeID}");
                DB.Save(data);
                return ApiResult<ElectronicAddress>.Ok(data, $"Employee field attachment has been stored");
            }
            catch (Exception e)
            {
                return ApiResult<ElectronicAddress>.Error(HttpStatusCode.BadRequest, $"Unable to upload document :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("electronicAddress/download/{employeeID}/{electronicAddressID}")]
        public IActionResult DownloadElectronicAddress(string employeeID, long electronicAddressID)
        {
            var result = _electronicAddress.Get(employeeID, electronicAddressID);
            var electronicAddress = (result.UpdateRequest != null) ? result.UpdateRequest : result.ElectronicAddress;

            // Download the data
            try
            {
                var bytes = electronicAddress.Download();
                return File(bytes, "application/force-download", Path.GetFileName(electronicAddress.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("electronicAddress/download/{id}")]
        public IActionResult DownloadElectronicAddress(string id)
        {
            var result = _electronicAddress.GetByID(id);

            // Download the data
            try
            {
                var bytes = result.Download();
                return File(bytes, "application/force-download", Path.GetFileName(result.Filepath));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /**
         * Employee Information
         */

        [HttpGet("employments/{employeeID}")]
        public IActionResult GetEmployments(string employeeID)
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var data = adapter.GetEmployments(employeeID);
                return ApiResult<List<Employment>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee employments :\n{Format.ExceptionString(e)}");
            }            
        }

        [HttpGet("certificates/{employeeID}")]
        public IActionResult GetCertificates(string employeeID)
        {
            try
            {
                var result = _certificate.GetS(employeeID);
                return ApiResult<List<CertificateResult>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get certificate '{employeeID}' :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpGet("certificate/changes/{employeeID}/{instanceID}")]
        public IActionResult GetCertificateInternal(string employeeID, string instanceID)
        {
            try
            {
                var result = _certificate.GetByAXRequestID(employeeID, instanceID);
                var updateRequest = _updateRequest.Get(employeeID, instanceID);
                if (result != null)
                {
                    result.Reason = updateRequest?.Notes;
                }

                return ApiResult<Certificate>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get certificate '{employeeID}-{instanceID}' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("certificate/create")]
        public IActionResult CreateCertificate([FromForm] CertificateForm param, ActionType action = ActionType.Create)
        {
            var updateRequest = new UpdateRequest();
            var adapter = new WorkFlowRequestAdapter(Configuration);
            var strAction = Enum.GetName(typeof(ActionType), action);            
            
            var data = JsonConvert.DeserializeObject<Certificate>(param.JsonData);
            data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;

            var oldData = _certificate.Get(data.EmployeeID, data.AXID, true).UpdateRequest;            

            try
            {
                data.Upload(Configuration, oldData, param.FileUpload, x => String.Format("Certificates_{0}", x.EmployeeID));

                // Create Update Request
                data.Purpose = Tools.ActionToPurpose(action);
                var AXRequestID = adapter.RequestCertificate(data, param.Reason);                
                if (!string.IsNullOrWhiteSpace(AXRequestID))
                {
                    var description = (!string.IsNullOrWhiteSpace(data.TypeDescription)) ? data.TypeDescription : data.TypeID;
                    updateRequest.Create(AXRequestID, data.EmployeeID, UpdateRequestModule.EMPLOYEE_CERTIFICATE, $"{Format.TitleCase(strAction)} certificate ({description})", param.Reason);
                    updateRequest.AXRequestID = AXRequestID;
                    DB.Save(updateRequest);

                    data.AXRequestID = AXRequestID;                    
                    data.Action = action;                    
                    DB.Save(data);

                    // Send approval notification
                    new Notification(Configuration, DB).SendApprovals(data.EmployeeID, data.AXRequestID);

                    return ApiResult<object>.Ok($"Certificate draft '{strAction}' request has been saved");
                }

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Unable to request update to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest,$"Certificate draft '{strAction}' is failed :\n{Format.ExceptionString(e)}");
            }
        }


        [HttpPost("certificate/update")]
        public IActionResult UpdateCertificate([FromForm] CertificateForm param)
        {
            return this.CreateCertificate(param, ActionType.Update);
        }

        [HttpPost("certificate/delete")]
        public IActionResult DeleteCertificate([FromBody] DeleteForm param)
        {
            var update = DB.GetCollection<Certificate>()
                        .Find(x => x.EmployeeID == param.EmployeeID && x.AXID == long.Parse(param.Id) && (x.Status == UpdateRequestStatus.InReview))
                        .FirstOrDefault();

            if (update == null)
            {
                var adapter = new EmployeeAdapter(Configuration);
                var data = adapter.GetCertificates(param.EmployeeID);
                update = data.Find(x => x.AXID == long.Parse(param.Id));
            }

            if (update == null)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to request '{ActionType.Delete.ToString()}' certificate member");
            }


            var _param = new CertificateForm();
            _param.JsonData = JsonConvert.SerializeObject(update);
            _param.Reason = param.Reason;
            return this.CreateCertificate(_param, ActionType.Delete);

        }

        [HttpGet("certificate/discardChange/{requestID}")]
        public IActionResult DiscardCertificateChange(string requestID = "")
        {
            try
            {
                _certificate.Discard(requestID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to discard certificate request :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<object>.Ok($"Employee certificate data request has been discarded");
        }

        [HttpGet("certificate/download/{employeeID}/{certificateID}")]
        public IActionResult DownloadCertificate(string employeeID, long certificateID)
        {
            var result = _certificate.Get(employeeID, certificateID);
            var certificate = (result.UpdateRequest != null) ? result.UpdateRequest : result.Certificate;

            // Download the data
            try
            {
                if (certificate.Accessible)
                {
                    var bytes = certificate.Download();
                    return File(bytes, "application/force-download", Path.GetFileName(certificate.Filepath));
                }
                else
                {
                    throw new Exception("Unable to find file path on database");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("certificate/attachment/download/{employeeID}/{axRequestID}")]
        public IActionResult DownloadCertificateAttachment(string employeeID, string axRequestID)
        {
            var certificate = _certificate.GetByAXRequestID(employeeID, axRequestID);            

            // Download the data
            try
            {
                if (certificate.Accessible)
                {
                    var bytes = certificate.Download();
                    return File(bytes, "application/force-download", Path.GetFileName(certificate.Filepath));
                }
                else
                {
                    throw new Exception("Unable to find file path on database");
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet("warningLetters/{employeeID}")]
        public IActionResult GetWarningLetters(string employeeID)
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var data = adapter.GetWarningLetters(employeeID);
                return ApiResult<List<WarningLetter>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee warning letters :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("warningLetter/download/{employeeID}/{certificateID}")]
        public IActionResult DownloadWarningLetter(string employeeID, long certificateID)
        {
            var adapter = new EmployeeAdapter(Configuration);
            var warningLetter = adapter.GetWarningLetters(employeeID)
                                .Find(x => x.AXID == certificateID);            

            // Download the data
            try
            {
                if (warningLetter.Accessible)
                {
                    var bytes = warningLetter.Download();
                    return File(bytes, "application/force-download", Path.GetFileName(warningLetter.Filepath));
                }
                else 
                {
                    throw new Exception("Unable to find file path on database");
                }
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        [HttpGet("medicalRecords/{employeeID}")]
        public IActionResult GetMedicalRecords(string employeeID)
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var data = adapter.GetMedicalRecords(employeeID);
                return ApiResult<List<MedicalRecord>>.Ok(data);
            }
            catch (Exception e)
            {
               
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee medical records :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("medicalRecord/download/{token}")]
        public IActionResult DownloadMedicalRecord(string token)
        {
            // Download the data
            Console.WriteLine($"{DateTime.Now} >>> {token}");
            try
            {
                var file = new FieldAttachment();
                var decodedToken = WebUtility.UrlDecode(token);
                file.Filepath = Hasher.Decrypt(decodedToken);
                Console.WriteLine($"{DateTime.Now} >>> {file.Filepath}");
                var bytes = file.Download();
                return File(bytes, "application/force-download", file.Filename);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} >>> {Format.ExceptionString(e, true)}");
                throw e;
            }
        }
       
        [HttpGet("documentRequests/{employeeID}")]
        public IActionResult GetDocumentRequests(string employeeID)
        {
            var documentRequests = new List<DocumentRequest>();
            try {
                documentRequests = DB.GetCollection<DocumentRequest>().Find(x => x.EmployeeID == employeeID).ToList();
            }
            catch (Exception e) 
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading document request :\n{Format.ExceptionString(e)}");
            }
            
            return ApiResult<List<DocumentRequest>>.Ok(documentRequests);
        }

        [HttpPost("documentRequest/save")]
        public IActionResult SaveDocumentRequest([FromForm] DocumentRequestForm param)
        {
            var updateRequest = new UpdateRequest();
            var adapter = new EmployeeAdapter(Configuration);            
            var data = JsonConvert.DeserializeObject<DocumentRequest>(param.JsonData);
            data.AXID = (data.AXID == -1) ? Tools.RandomInt() * -1 : data.AXID;

            var oldData = DB.GetCollection<DocumentRequest>()
                .Find(x => x.AXID == data.AXID && x.Id == data.Id && (x.Status == UpdateRequestStatus.InReview))
                .FirstOrDefault();

            try
            {
                var AXRequestID = adapter.UpdateDocumentRequest(data);
                if (!string.IsNullOrWhiteSpace(AXRequestID))
                {
                    updateRequest.Create(AXRequestID, data.EmployeeID, UpdateRequestModule.EMPLOYEE_DOCUMENT_REQUEST, $"Request document {data.DocumentType})");
                    data.AXRequestID = AXRequestID;

                    data.Upload(Configuration, oldData, param.FileUpload, x => String.Format("DocumentRequest_{0}_{1}", data.DocumentType, x.EmployeeID));
                    DB.Save(data);
                    return ApiResult<object>.Ok(data, "Document request has been saved successfully");
                }

                return  ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to update request to AX");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving document request :\n{Format.ExceptionString(e)}");
            }
            
        }

        [HttpGet("documentRequest/delete/{requestID}")]
        public IActionResult DeleteDocumentRequest(string requestID)
        {
            try
            {                
                DB.Delete(new DocumentRequest { Id=requestID});
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving document request :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<DocumentRequest>.Ok("Document request has been deleted successfully ");
        }
        

        [HttpGet("applicants/{employeeID}/{applicantID}")]
        public IActionResult GetApplicant(string employeeID, string applicantID)
        {
            var response = new List<Application>();
            return ApiResult<List<Application>>.Ok(response);
        }
        
        [HttpGet("applicants/{employeeID}")]
        public IActionResult GetApplicants(string employeeID)
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var data = adapter.GetApplications(employeeID);
                return ApiResult<List<Application>>.Ok(data);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee applications :\n{Format.ExceptionString(e)}");
            }           
        }

        [HttpGet("trainings/{employeeID}")]
        public IActionResult GetTrainings(string employeeID)
        {

            // Get Certificate from AX
            var trainings = new List<Training>();
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                trainings = adapter.GetTrainings(employeeID);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable get trainings data from ax:\n{Format.ExceptionString(e)}");
            }

            // Get Document Update Request from Local ESS        
            var updateRequest = new List<Training>();
            try
            {
                updateRequest = DB.GetCollection<Training>()
                        .Find(x => x.EmployeeID == employeeID)
                        .ToList();
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable get trainings data :\n{Format.ExceptionString(e)}");
            }

            trainings.AddRange(updateRequest);            
            return ApiResult<List<Training>>.Ok(trainings);
        }
        [HttpGet("document/type")]
        public IActionResult GetDocumentType()
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var result = adapter.GetDocumentTypes();
                return ApiResult<object>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Employee>.Error(HttpStatusCode.BadRequest, $"Unable to get document type list :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("documentRequest/type")]
        public IActionResult GetDocumentRequestType()
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var result = adapter.GetDocumentRequestTypes();
                return ApiResult<object>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get document request type list :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("list/city")]
        public IActionResult GetCities()
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var result = adapter.GetCities();
                return ApiResult<List<City>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get document request type list :\n{Format.ExceptionString(e)}");
            }
        }

        /**
         * Employee Data List
         */

        [HttpGet("list/familyRelationship")]        
        public IActionResult GetFamilyRelationship()
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var result = adapter.GetRelationshipType();
                return ApiResult<List<RelationshipType>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get relationship type list :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("list/certificateType")]
        public IActionResult GetCertificateType()
        {
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                var result = adapter.GetCertificateType();
                return ApiResult<List<CertificateType>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get certificate type list :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("list/religion")]
        public IActionResult GetReligion()
        {
            var response = Enum.GetNames(typeof(Religion)).ToList<string>();
            return ApiResult<List<string>>.Ok(response);
        }

        [HttpGet("list/maritalStatus")]
        public IActionResult GetMaritalStatus()
        {
            var response = Enum.GetNames(typeof(MaritalStatus)).ToList<string>();
            return ApiResult<List<string>>.Ok(response);
        }

        [HttpGet("list/gender")]
        public IActionResult GetGender()
        {
            var response = Enum.GetNames(typeof(Gender)).ToList<string>();
            return ApiResult<List<string>>.Ok(response);
        }

        [HttpGet("list/identificationType")]
        public IActionResult GetIdentificationType()
        {

            List<IdentificationType> data;
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                data = adapter.GetIdentificationTypes();
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get identification types :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<IdentificationType>>.Ok(data);
        }      

        [HttpGet("list/electronicAddressType")]
        public IActionResult GetElectronicAddressType()
        {
            List<ElectronicAddressType> data;
            try
            {
                var adapter = new EmployeeAdapter(Configuration);
                data = adapter.GetElectronicAddressTypes();
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get identification types :\n{Format.ExceptionString(e)}");
            }
            return ApiResult<List<ElectronicAddressType>>.Ok(data);            
        }

        [HttpGet("get/{employeeID}/{axRequestID}")]
        public IActionResult GetEmployeeByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _employee.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<Core.Model.Employee>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get employee data :\n{e.Message}");
            }

        }

       [HttpGet("families/{employeeID}/{axRequestID}")]
        public IActionResult GetFamiliesByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _family.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<Family>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get Families data :\n{e.Message}");
            }

        }

        [HttpGet("identification/{employeeID}/{axRequestID}")]
        public IActionResult GetIdentificationByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _identification.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<Identification>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get Identification data :\n{e.Message}");
            }

        }

        [HttpGet("bankAccount/{employeeID}/{axRequestID}")]
        public IActionResult GetBankAccountByInstanceID(string employeeID, string axRequestID)
        {
            try
            {
                var result = _bankAccount.GetByAXRequestID(employeeID, axRequestID);
                return ApiResult<BankAccount>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get BankAccount data :\n{e.Message}");
            }

        }

        [HttpGet("get/zz")]
        public IActionResult GetEmployees()
        {
            try
            {
                var result = _employee.GetEmployees();
                return ApiResult<List<Core.Model.Employee>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Unable to get all data employees :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpGet("departments/get")]
        public IActionResult GetDepartments()
        {
            
            try
            {
                //var adapter = new HRAdapter(Configuration);
                //var result = adapter.GetDepartments();

                // Update Many
                //IEnumerable<Department> enumDept;
                //var newStudents = new List<Department>();
                //foreach (var w in result)
                //{
                //    var x = new Department
                //    {
                //        Id = ObjectId.GenerateNewId().ToString(),
                //        EmployeeID = w.EmployeeID,
                //        Name = w.Name
                //    };
                //    newStudents.Add(x);
                //}
                //enumDept = newStudents;
                //var collections = DB.GetCollection<Department>();
                //collections.InsertManyAsync(enumDept);

                //Department temp = new Department();
                //Department newDepartment = new Department();
                //foreach(var a in result)
                //{
                //    temp = DB.GetCollection<Department>().Find(x => x.EmployeeID == a.EmployeeID).FirstOrDefault();
                //    if (temp == null)
                //    {
                //        newDepartment = new Department
                //        {
                //            RecId = a.RecId,
                //            EmployeeID = a.EmployeeID,
                //            EmployeeName = a.EmployeeName,
                //            Name = a.Name,
                //            NameAlias = a.NameAlias,
                //            OperationUnitNumber = a.OperationUnitNumber,
                //            OperationUnitType = a.OperationUnitType,
                //            LastUpdate = DateTime.Now
                //        };
                //        DB.Save(newDepartment);
                //    } else
                //    {
                //        newDepartment = new Department
                //        {
                //            Id = temp.Id,
                //            RecId = a.RecId,
                //            EmployeeID = a.EmployeeID,
                //            EmployeeName = a.EmployeeName,
                //            Name = a.Name,
                //            NameAlias = a.NameAlias,
                //            OperationUnitNumber = a.OperationUnitNumber,
                //            OperationUnitType = a.OperationUnitType,
                //            LastUpdate = DateTime.Now
                //        };
                //        DB.Save(newDepartment);
                //    }
                //}

                var result = DB.GetCollection<Department>().Find(x => true).ToList();
                return ApiResult<List<Department>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get relationship type list :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("departments/all")]
        public IActionResult GetDepartmentAll()
        {
            try
            {
                List<Department> departments = new List<Department>();
                departments = DB.GetCollection<Department>().Find(x => true).ToList();
                return ApiResult<List<Department>>.Ok(departments);
            } catch(Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get all data employees :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }
    }
}