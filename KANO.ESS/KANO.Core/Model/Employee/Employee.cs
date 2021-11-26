using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    /// <summary>
    /// Get Employee from API and transpose to this class
    /// </summary>

    [Collection("Employee")]
    [BsonIgnoreExtraElements]
    public class Employee : BaseUpdateRequest, IMongoPreSave<Employee>
    {                            
        public Employee Old { get; set; }
        public string Birthplace { get; set; }
        public DateTime Birthdate { get; set; }
        public DateTime LastEmploymentDate { get; set; }
        public string WorkerTimeType { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        private Gender _gender { get; set; }       
        public Gender Gender
        {
            get
            {
                return _gender;
            }

            set
            {
                _gender = value;
                GenderDescription = Enum.GetName(typeof(Gender), (Gender)_gender);
            }
        }
        public string GenderDescription { get; set; }
        public FieldAttachment IsExpartriateAttachment { get; set; }
        public bool IsExpatriate { get; set; }
        private string _profilePicture;
        [JsonIgnore]
        public string ProfilePicture
        {
            get
            {
                return _profilePicture;
            }

            set
            {
                _profilePicture = value;
                if (!string.IsNullOrWhiteSpace(_profilePicture))
                {
                    AccessibleProfilePicture = File.Exists(_profilePicture);                    
                }

            }
        }
        public bool AccessibleProfilePicture { get; set; }
        [BsonIgnore]
        public Address Address { get; set; } = new Address();
        private Religion _religion { get; set; }                
        public Religion Religion {
            get
            {
                return _religion;
            }

            set
            {
                _religion = value;                
                 ReligionDescription = Enum.GetName(typeof(Religion), (Religion)_religion);
            }
        }                
        public string ReligionDescription { get; set; }
        public FieldAttachment MaritalStatusAttachment { get; set; }
        private MaritalStatus _maritalStatus { get; set; }
        public MaritalStatus MaritalStatus {
            get
            {
                return _maritalStatus;
            }

            set
            {
                _maritalStatus = value;
                MaritalStatusDescription = Enum.GetName(typeof(MaritalStatus), (MaritalStatus)_maritalStatus);
            }
        }        
        public string MaritalStatusDescription { get; set; }        
        [BsonIgnore]
        public List<Identification> Identifications { get; set; } = new List<Identification>();
        [BsonIgnore]
        public List<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
        [BsonIgnore]
        public List<Tax> Taxes { get; set; } = new List<Tax>();
        [BsonIgnore]
        public List<ElectronicAddress> ElectronicAddresses { get; set; } = new List<ElectronicAddress>();        

        public Employee() : base() { }

        public Employee(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public EmployeeResult GetFullDetail(string employeeID) {
            var tasks = new List<Task<TaskRequest<Core.Model.Employee>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(this.Configuration);
                var employee = adapter.GetFullDetail(employeeID);
                return TaskRequest<Core.Model.Employee>.Create("AX", employee);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var employee = this.MongoDB.GetCollection<Core.Model.Employee>()
                                .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                .FirstOrDefault();
                return TaskRequest<Core.Model.Employee>.Create("DB", employee);
            }));

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
            var result = new EmployeeResult();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Employee = r.Result;
                    else
                        result.UpdateRequest = r.Result;
            }

            return result;
        }

        public EmployeeResult GetDetail(string employeeID, string axRequestID = "", bool essInternalOnly = false)
        {
            var tasks = new List<Task<TaskRequest<Core.Model.Employee>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly) {
                    return TaskRequest<Core.Model.Employee>.Create("AX", new Employee());
                }

                var adapter = new EmployeeAdapter(this.Configuration);
                var employee = adapter.GetDetail(employeeID);
                return TaskRequest<Core.Model.Employee>.Create("AX", employee);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var employee = new Employee();
                if (string.IsNullOrWhiteSpace(axRequestID))
                {
                    employee = this.MongoDB.GetCollection<Core.Model.Employee>()
                                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                    .FirstOrDefault();
                }
                else
                {
                    employee = this.MongoDB.GetCollection<Core.Model.Employee>()
                                   .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                   .FirstOrDefault();
                }

                return TaskRequest<Core.Model.Employee>.Create("DB", employee);
            }));

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception)
            {
                throw;
            }

            // Combine result
            var result = new EmployeeResult();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Employee = r.Result;
                    else
                        result.UpdateRequest = r.Result;
            }

            return result;
        }

        public List<Employee> GetEmployees(bool essInternalOnly = false)
        {
            List<Employee> employees = new List<Employee>();
            List<Task<TaskRequest<List<Employee>>>> tasks = new List<Task<TaskRequest<List<Employee>>>> {
                Task.Run(() => {
                    List<Employee> employeeList = new List<Employee>();
                    employeeList = this.MongoDB.GetCollection<Core.Model.Employee>()
                                        .Find(x => (int)x.Gender == 1).ToList();
                    return TaskRequest<List<Employee>>.Create("DB", employeeList);
                })
            };
            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception)
            {
                throw;
            }
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        employees = r.Result;
                    else
                        employees = r.Result;
            }
            return employees;
        }

        public Employee GetByID(string id)
        {
            return this.MongoDB.GetCollection<Employee>()
                                .Find(x => x.Id == id)
                                .FirstOrDefault();
        }

        public Employee Update(Employee employee) {
            this.MongoDB.Save(employee);
            return employee;
        }

        public void Discard(string employeeID) {
            var employee = this.MongoDB.GetCollection<Employee>()
                    .FindOneAndDelete(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview));
            Tools.DeleteFile(employee.IsExpartriateAttachment.Filepath);
            Tools.DeleteFile(employee.MaritalStatusAttachment.Filepath);            
        }

        public bool SetAXRequestID(string employeeID, string AXRequestID) {
            var updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = false;

            var result = this.MongoDB.GetCollection<Core.Model.Employee>().UpdateMany(
                        x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview),
                        Builders<Core.Model.Employee>.Update
                            .Set(d => d.AXRequestID, AXRequestID),
                        updateOptions
                    );

            return result.IsAcknowledged;
        }
     
        public EmployeeDetail GetByAXRequestID(string employeeID, string axRequestID, bool essInternalOnly = false)
        {                        
            var tasks = new List<Task<TaskRequest<object>>>();

            try
            {
                // Fetch employee
                var employee = new EmployeeResult();
                tasks.Add(Task.Run(() =>
                {
                    employee = this.GetDetail(employeeID, axRequestID, essInternalOnly);
                    return TaskRequest<object>.Create("employee", true);
                }));

                // Fetch employee
                var updateRequest = new UpdateRequest(MongoDB);
                tasks.Add(Task.Run(() =>
                {
                    var c = updateRequest.Get(employeeID, axRequestID);
                    return TaskRequest<object>.Create("updateRequest", true);
                }));

                // Fetch identification
                var identifications = new List<IdentificationResult>();
                tasks.Add(Task.Run(() => {
                    var _identification = new Identification(MongoDB, Configuration);
                    identifications = _identification.GetS(employeeID,axRequestID, essInternalOnly);
                    return TaskRequest<object>.Create("identification", true);
                }));

                // Fetch bank account
                var bankAccounts = new List<BankAccountResult>();
                tasks.Add(Task.Run(() => {
                    var _bankAccount = new BankAccount(MongoDB, Configuration);
                    bankAccounts = _bankAccount.GetS(employeeID, axRequestID, essInternalOnly);
                    return TaskRequest<object>.Create("bankAccount", true);
                }));

                // Fetch tax
                var taxes = new List<TaxResult>();
                tasks.Add(Task.Run(() => {
                    var _tax = new Tax(MongoDB, Configuration);
                    taxes = _tax.GetS(employeeID, axRequestID, essInternalOnly);
                    return TaskRequest<object>.Create("tax", true);
                }));

                // Fetch address
                var address = new AddressResult();
                tasks.Add(Task.Run(() => {
                    var _address = new Address(MongoDB, Configuration);
                    address = _address.Get(employeeID, axRequestID, essInternalOnly);
                    return TaskRequest<object>.Create("address", true);
                }));

                // Fetch electronic address
                var electronicAddresses = new List<ElectronicAddressResult>();
                tasks.Add(Task.Run(() => {
                    var _electronicAddress = new ElectronicAddress(MongoDB, Configuration);
                    electronicAddresses = _electronicAddress.GetS(employeeID, axRequestID, essInternalOnly);
                    return TaskRequest<object>.Create("electronicAddress", true);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw new Exception($"Unable get employee '{employeeID}-{axRequestID}'", e);
                }
                
                return new EmployeeDetail { 
                    Employee = employee,
                    Address=address,
                    UpdateRequest=updateRequest,
                    BankAccounts=bankAccounts,
                    ElectronicAddresses=electronicAddresses,
                    Identifications=identifications,
                    Taxes=taxes,                    
                };
            }
            catch (Exception e)
            {
                throw new Exception($"Unable get employee '{employeeID}-{axRequestID}'", e);
            }
        }

        public void StatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                var tasks = new List<Task<TaskRequest<object>>>();
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                // Update Employee
                var employee = MongoDB.GetCollection<Employee>()
                   .Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status)
                   .FirstOrDefault();

                if (employee != null)
                {
                    // Archiving files
                    if (employee.IsExpartriateAttachment != null)
                    {
                        employee.IsExpartriateAttachment.Filepath = Tools.ArchiveFile(employee.IsExpartriateAttachment.Filepath);
                    }

                    if (employee.MaritalStatusAttachment != null)
                    {
                        employee.MaritalStatusAttachment.Filepath = Tools.ArchiveFile(employee.MaritalStatusAttachment.Filepath);
                    }

                    employee.Status = newStatus;
                    MongoDB.Save(employee);

                    if (newStatus == UpdateRequestStatus.Approved)
                    {
                        // When email is changed then update user data
                        var email = MongoDB.GetCollection<ElectronicAddress>()
                            .Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status && x.Type == KESSHCMServices.LogisticsElectronicAddressMethodType.Email)
                            .FirstOrDefault();
                        
                        var user = MongoDB.GetCollection<User>()
                            .Find(x => x.Username == employeeID)
                            .FirstOrDefault();

                        if (user != null && email != null && user.Email != email.Locator)
                        {
                            user.Email = email.Locator.Trim().ToLower();
                            MongoDB.Save(user);
                        }
                    }

                }

                // Update Identification
                tasks.Add(Task.Run(() =>
                {
                    var result = MongoDB.GetCollection<Identification>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).ToList();
                    foreach (var r in result) {
                        r.Status = newStatus;
                        r.Filepath = Tools.ArchiveFile(r.Filepath);
                        MongoDB.Save(r);
                    }
                    return TaskRequest<object>.Create("identification", true);
                }));

                // Update Bank Account
                tasks.Add(Task.Run(() =>
                {
                    var result = MongoDB.GetCollection<BankAccount>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).ToList();
                    foreach (var r in result)
                    {
                        r.Status = newStatus;
                        r.Filepath = Tools.ArchiveFile(r.Filepath);
                        MongoDB.Save(r);
                    }
                    return TaskRequest<object>.Create("bankAccount", true);
                }));

                // Update Tax
                tasks.Add(Task.Run(() =>
                {
                    var result = MongoDB.GetCollection<Tax>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).ToList();
                    foreach (var r in result)
                    {
                        r.Status = newStatus;
                        r.Filepath = Tools.ArchiveFile(r.Filepath);
                        MongoDB.Save(r);
                    }
                    return TaskRequest<object>.Create("tax", true);
                }));


                // Update Address
                tasks.Add(Task.Run(() =>
                {
                    var result = MongoDB.GetCollection<Address>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).ToList();
                    foreach (var r in result)
                    {
                        r.Status = newStatus;
                        r.Filepath = Tools.ArchiveFile(r.Filepath);
                        MongoDB.Save(r);
                    }
                    return TaskRequest<object>.Create("address", true);
                }));

                // Update Electronic Address
                tasks.Add(Task.Run(() =>
                {
                    var result = MongoDB.GetCollection<ElectronicAddress>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).ToList();
                    foreach (var r in result)
                    {
                        r.Status = newStatus;
                        r.Filepath = Tools.ArchiveFile(r.Filepath);
                        MongoDB.Save(r);
                    }
                    return TaskRequest<object>.Create("electronicAddress", true);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update employee data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID, // Receiver
                            $"Employee update request is {newStatus.ToString()}", // Message
                            Notification.DEFAULT_SENDER,
                            NotificationModule.EMPLOYEE, // Module
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }
        
        public Employee GetByAXRequestID(string employeeID, string axRequestID)
        {
            return this.MongoDB.GetCollection<Employee>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();
        }
    }
     
    public class EmployeeResult {
        public Employee Employee { get; set; }
        public Employee UpdateRequest { get; set; }
    }

    public class EmployeeDetail
    {
        public EmployeeResult Employee { get; set; }        
        public AddressResult Address { get; set; }        
        public UpdateRequest UpdateRequest { get; set; }        
        public List<IdentificationResult> Identifications { get; set; } = new List<IdentificationResult>();        
        public List<BankAccountResult> BankAccounts { get; set; } = new List<BankAccountResult>();        
        public List<TaxResult> Taxes { get; set; } = new List<TaxResult>();        
        public List<ElectronicAddressResult> ElectronicAddresses { get; set; } = new List<ElectronicAddressResult>();        
    }

    public enum Gender : int
    {
        None = 0,
        Male = 1,
        Female = 2,       
    }    

    public enum MaritalStatus : int
    {        
        None = 0,        
        Married = 1,        
        Single = 2,        
        Widowed = 3,        
        Divorced = 4,        
        Cohabiting = 5,        
        RegisteredPartnership = 6,
    }

    public enum Religion : int
    {        
        Islam = 0,        
        Katolik = 1,        
        Kristen = 2,        
        Budha = 3,        
        Hindu = 4,
    }

    public class EmployeeFieldAttachmentForm
    {
        public string Field { get; set; }
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }
    
}
