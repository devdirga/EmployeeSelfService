using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Family")]
    [BsonIgnoreExtraElements]
    public class Family: BaseDocumentVerification, IMongoPreSave<Family>
    {        
        public Family Old { set; get; }
        private string name { set; get; }
        public string Name { 
            get {
                if (!string.IsNullOrWhiteSpace(this.name)) return this.name;

                var fullName = "";

                if (!string.IsNullOrWhiteSpace(this.FirstName)) {
                    fullName = this.FirstName;
                }

                if (!string.IsNullOrWhiteSpace(this.MiddleName))
                {
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        fullName = $"{fullName} {this.MiddleName}";
                    }
                    else 
                    {
                        fullName = this.MiddleName;
                    }                    
                }

                if (!string.IsNullOrWhiteSpace(this.LastName))
                {
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        fullName = $"{fullName} {this.LastName}";
                    }
                    else
                    {
                        fullName = this.LastName;
                    }
                }

                return fullName;

            }

            set{
                this.name = value;
            }
        }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MiddleName { get; set; }
        public string NIK { get; set; }
        public string NoTelp { get; set; }
        public string Email { get; set; }
        public string Birthplace { get; set; }
        public DateTime Birthdate { get; set; }
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
        public string Job { get; set; }
        private Religion _religion { get; set; }
        public Religion Religion
        {
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
        public KESSWRServices.KESSWorkerRequestPurpose Purpose { get; set; }
        public string Relationship { get; set; }
        public string RelationshipDescription { get; set; }
        public string NoBPJS { get; set; }
        public string NoInsurance { get; set; }

        public Family() : base() {}

        public Family(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration){ }

        public List<FamilyResult> GetS(string employeeID, string axRequestID = "") {
            var tasks = new List<Task<TaskRequest<List<Family>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(this.Configuration);
                var families = adapter.GetFamilies(employeeID);
                return TaskRequest<List<Family>>.Create("AX", families);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var families = new List<Family>();
                if (string.IsNullOrWhiteSpace(axRequestID))
                {
                    families = this.MongoDB.GetCollection<Family>()
                                    .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                    .ToList();
                }
                else 
                {
                    families = this.MongoDB.GetCollection<Family>()
                                    .Find(x => x.EmployeeID == employeeID && x.AXRequestID==axRequestID)
                                    .ToList();
                }

                return TaskRequest<List<Family>>.Create("DB", families);
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
            var result = new List<FamilyResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var Families = new List<Family>();
                var FamiliesUpdateRequest = new List<Family>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        Families = r.Result;
                    else
                        FamiliesUpdateRequest = r.Result;

                // Merge families data
                foreach (var f in Families)
                {
                    result.Add(new FamilyResult
                    {
                        Family = f,
                    });
                }

                foreach (var fur in FamiliesUpdateRequest)
                {
                    if (fur.AXID > 0)
                    {
                        var f = result.Find(x => x.Family.AXID == fur.AXID);
                        if (f != null)
                            f.UpdateRequest = fur;
                    }
                    else
                    {
                        result.Add(new FamilyResult
                        {
                            UpdateRequest = fur,
                        });
                    }
                }                
            }
            return result;
        }

        public FamilyResult Get(string employeeID, long axID, bool essInternalOnly = false)
        {
            var result = new FamilyResult();
            var tasks = new List<Task<TaskRequest<Family>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly)
                {
                    return TaskRequest<Family>.Create("AX", new Family());
                }
                var adapter = new EmployeeAdapter(Configuration);
                var family = adapter.GetFamilies(employeeID)
                                    .Find(x => x.AXID == axID);
                return TaskRequest<Family>.Create("AX", family);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var family = this.MongoDB.GetCollection<Family>()
                                .Find(x => x.EmployeeID == employeeID && x.AXID == axID && (x.Status == UpdateRequestStatus.InReview))
                                .FirstOrDefault();
                return TaskRequest<Family>.Create("DB", family);
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
                        result.Family = r.Result;
                    else
                        result.UpdateRequest = r.Result;
            }
            else
            {
                throw new Exception("Unable to get file");
            }

            return result;
        }

        public Family GetByAXRequestID(string employeeID, string axRequestID)
        {
            return this.MongoDB.GetCollection<Family>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();
        }

        public void Discard(string requestID) {            
            var family = this.MongoDB.GetCollection<Family>().Find(x => x.Id == requestID).FirstOrDefault();
            if (family != null)
            {
                this.MongoDB.Delete(family);
                if (System.IO.File.Exists(family.Filepath))
                {
                    System.IO.File.Delete(family.Filepath);
                }
            }
        }

        public bool SetAXRequestID(string employeeID, string AXRequestID)
        {
            var updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = false;

            var result = this.MongoDB.GetCollection<Family>().UpdateMany(
                        x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview),
                        Builders<Family>.Update
                            .Set(d => d.AXRequestID, AXRequestID),
                        updateOptions
                    );

            return result.IsAcknowledged;
        }        

        public void SatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                var tasks = new List<Task<TaskRequest<object>>>();
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    var family = MongoDB.GetCollection<Family>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).FirstOrDefault();
                    if (family == null) return TaskRequest<object>.Create("certificate", false);

                    var newFilepath = Tools.ArchiveFile(family.Filepath);
                    var result = MongoDB.GetCollection<Family>().UpdateOne(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                        Builders<Family>.Update
                            .Set(d => d.Status, newStatus)
                            .Set(d => d.Filepath, newFilepath),
                        updateOptions
                    );
                    return TaskRequest<object>.Create("certificate", result.IsAcknowledged);
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update certificate data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"Family request is {newStatus.ToString()}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.FAMILY,
                            NotificationAction.OPEN_EMPLOYEE_FAMILY,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }
    }    

    public class FamilyResult
    {
        public Family Family { get; set; } = null;        
        public Family UpdateRequest { get; set; } = null;
    }

    public class FamilyForm 
    {
        public string JsonData { get; set; }
        public string Reason { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class RelationshipType {
        public long AXID { get; set; }
        public string TypeID { get; set; }
        public string Description { get; set; }
    }

    public enum FamilyRelationship : int
    {
        Parent = 0,
        Child = 1,
        Spouse = 2
    }
}
