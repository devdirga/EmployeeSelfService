using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Training")]
    [BsonIgnoreExtraElements]
    public class Training : BaseDocumentVerification, IMongoPreSave<Training>
    {        
        public string TrainingID { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string TermAndCondition { get; set; }
        public string Room { get; set; }
        public string Purpose { get; set; }
        public string Description { get; set; }
        public DateRange Schedule { get; set; }
        public KESSHRMServices.HrmCourseEvent Event { get; set; }
        public string EventDescription { 
            get{
                return this.Event.ToString();
            }  
        }
        public bool AbroadTraining { get; set; }
        public double Cost { get; set; }
        public int MaxAttendees { get; set; }
        public int MinAttendees { get; set; }
        public string TypeID { get; set; }
        public string TypeDescription { get; set; }
        public string SubTypeID { get; set; }
        public string SubTypeDescription { get; set; }
        public string Vendor { get; set; }
        public KESSHRMServices.HRMCourseTableStatus TrainingStatus {set; get;}
        public string TrainingStatusDescription {
            get{
                return this.TrainingStatus.ToString();
            }
        }
        public DateTime RegistrationDeadline { get; set; }
        [BsonIgnore]
        public TrainingRegistration TrainingRegistration { get; set; }
        public string Note { get; set; }
        public List<FileUpload> Attachments { get; set; } = new List<FileUpload>();

        public Training() : base() { }

        public Training(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);
        }

        public List<TrainingReference> GetReferences(string employeeID) {
            var tasks = new List<Task<TaskRequest<List<TrainingReference>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(this.Configuration);               
                var certificate = adapter.GetCertificates(employeeID, true);
                var references = new List<TrainingReference>();
                foreach (var c in certificate) {
                    var description = $"{c.TypeDescription} - {c.Note}";
                    if (string.IsNullOrWhiteSpace(c.Note)) {
                        description = $"{c.TypeDescription}";
                    }
                    references.Add(new TrainingReference
                    {
                        Description = description,
                        AXID = c.AXID,
                        Type = ReferenceType.Certificate,
                        Validity = c.Validity,
                        Attachment = new FieldAttachment { 
                            AXID = c.AXID,
                            Filepath = c.Filepath,
                        }
                    });
                }

                return TaskRequest<List<TrainingReference>>.Create("Certificate", references);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var adapter = new EmployeeAdapter(this.Configuration);
                var documents = adapter.GetDocuments(employeeID);
                var references = new List<TrainingReference>();
                foreach (var d in documents)
                {
                    references.Add(new TrainingReference
                    {
                        Description = d.Description,
                        AXID = d.AXID,
                        Type = ReferenceType.Document,
                        Validity = null,
                        Attachment = new FieldAttachment
                        {
                            AXID = d.AXID,
                            Filepath = d.Filepath,
                        }
                    });
                }
                return TaskRequest<List<TrainingReference>>.Create("Document", references);
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
            var result = new List<TrainingReference>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    result.AddRange(r.Result);                
            }

            return result;
        }
    
        public List<Training> GetS(DateRange range, int courseStatus){
            var tasks = new List<Task<TaskRequest<object>>>();

            tasks.Add(Task.Run(() =>
            {
                var adapter = new TrainingAdapter(this.Configuration);
                if (courseStatus == -1 || courseStatus == 1)
                {
                    var rs = adapter.GetBasicS(range);
                    var data = rs.FindAll(x => x.TrainingStatus == KESSHRMServices.HRMCourseTableStatus.Reopen 
                        || x.TrainingStatus == KESSHRMServices.HRMCourseTableStatus.Open);
                    return TaskRequest<object>.Create("Trainings", data);
                }
                else 
                {
                    var data = adapter.GetBasicS(range).FindAll(x => x.TrainingStatus == (KESSHRMServices.HRMCourseTableStatus)courseStatus);
                    return TaskRequest<object>.Create("Trainings", data);
                }
            }));

            tasks.Add(Task.Run(() => {
                var adapter = new TrainingAdapter(this.Configuration);
                var data = adapter.GetTypes();                
                var dataMap = new Dictionary<string, string>();

                foreach(var d in data){
                    dataMap[d.AXID.ToString()]=d.Description;
                }

                return TaskRequest<object>.Create("Types", dataMap);
            }));

            tasks.Add(Task.Run(() => {
                var adapter = new TrainingAdapter(this.Configuration);
                var data = adapter.GetSubTypes();                
                var dataMap = new Dictionary<string, string>();

                foreach(var d in data){
                    dataMap[d.SubType]=d.Description;
                }

                return TaskRequest<object>.Create("SubTypes", dataMap);
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
            var result = new List<Training>();
            var types = new Dictionary<string, string>();
            var subTypes = new Dictionary<string, string>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    switch (r.Label)
                    {
                        case "Trainings":
                            result = (List<Training>) r.Result;
                            break;
                        case "Types":
                            types = (Dictionary<string, string>) r.Result;
                            break;
                        case "SubTypes":
                            subTypes = (Dictionary<string, string>) r.Result;
                            break;
                        default:
                            break;
                    }              
            }

            foreach(var r in result){
                string value;                
                if(types.TryGetValue(r.TypeID, out value)) r.TypeDescription = value;
                if(subTypes.TryGetValue(r.SubTypeID, out value)) r.SubTypeDescription = value;
            }

            return result;
        }

        public List<Training> GetSWithRegistrationDetail(string employeeID, DateRange range, int status){
            var trainings = this.GetS(range, status);
            var tasks = new List<Task<TaskRequest<Dictionary<string, TrainingRegistration>>>>();

            tasks.Add(Task.Run(() =>
            {
                var adapter = new TrainingAdapter(this.Configuration);
                var options = new ParallelOptions() { MaxDegreeOfParallelism = 10 };
                var registrationMap = new Dictionary<string, TrainingRegistration>();
                Parallel.ForEach(trainings, options, training => {
                    var registration = adapter.GetRegistration(training.EmployeeID, training.TrainingID);
                    if(registration != null){
                        registrationMap[registration.TrainingID]=registration;
                    }                            
                });
                return TaskRequest<Dictionary<string, TrainingRegistration>>.Create("AX", registrationMap);
            }));
            

            tasks.Add(Task.Run(() => {
                var trainigIDs = trainings.Select(x=>x.TrainingID).Distinct();
                var registeredTraining = this.MongoDB.GetCollection<TrainingRegistration>().Find(x => x.EmployeeID == employeeID && trainigIDs.Contains(x.TrainingID)).ToList();
                var registeredTrainingMap = new Dictionary<string, TrainingRegistration>();
                
                foreach(var rt in registeredTraining){
                    registeredTrainingMap[rt.TrainingID] = rt;
                }

                return TaskRequest<Dictionary<string, TrainingRegistration>>.Create("DB", registeredTrainingMap);
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
            var registrationAX = new Dictionary<string, TrainingRegistration>();
            var registrationDB = new Dictionary<string, TrainingRegistration>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    switch (r.Label)
                    {                        
                        case "AX":
                            registrationAX = (Dictionary<string, TrainingRegistration>) r.Result;
                            break;
                        case "DB":
                            registrationDB = (Dictionary<string, TrainingRegistration>) r.Result;
                            break;
                        default:
                            break;
                    }              
            }

            foreach(var r in trainings){
                TrainingRegistration registration;
                r.TrainingRegistration = null;
                if(registrationDB.TryGetValue(r.TrainingID, out registration)){
                    r.TrainingRegistration = registration;
                }

                if(registrationAX.TryGetValue(r.TrainingID, out registration)){
                    if(r.TrainingRegistration == null){
                        r.TrainingRegistration = registration;
                    }else{
                        r.TrainingRegistration.RegistrationStatus = registration.RegistrationStatus; 
                    }
                }
            }

            return trainings;
        }
    }

    public class TrainingType{
        public int MinAttendees {get; set;}
        public long TypeGroup {get; set;}
        public int NumberOfDays {get; set;}
        public string TypeID {get; set;}
        public string Description {get; set;}
        public long AXID {get; set;}
    }

    public class TrainingSubType{
        public string SubType {get; set;}
        public string TypeID {get; set;}
        public string Description {get; set;}
        public long AXID {get; set;}
    }

    public enum ReferenceType : int
    {
        Document=0,
        Certificate=1,
    }

    [Collection("TrainingRegistration")]
    [BsonIgnoreExtraElements]
    public class TrainingRegistration: BaseUpdateRequest, IMongoPreSave<TrainingRegistration>
    {
        public string TrainingID { get; set; }
        public DateTime RegistrationDate { get; set; }
        public Training Training { get; set; }
        public List<TrainingReference> References { get; set; } = new List<TrainingReference>();
        public KESSHRMServices.HRMCourseAttendeeStatus RegistrationStatus { get; set; }
        public string RegistrationStatusDescription { get {
                return this.RegistrationStatus.ToString();
            } 
        }
        public void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);
        }

        public void SatusUpdater(IMongoDatabase DB, IConfiguration configuration, TrainingRegistration updatedRequest)
        {
            if (this.RegistrationStatus != updatedRequest.RegistrationStatus)
            {
                
                this.RegistrationStatus = updatedRequest.RegistrationStatus;                

                var statusDescription = "";
                var statusType = NotificationType.Info;
                switch (this.RegistrationStatus)
                {
                    case KESSHRMServices.HRMCourseAttendeeStatus.Registered:
                        statusDescription = $"You are registered to training \"{this.Training.Name}\"";
                        break;
                    case KESSHRMServices.HRMCourseAttendeeStatus.Confirmation:
                        statusDescription = $"Training registration \"{this.Training.Name}\" is confirmed";
                        break;
                    case KESSHRMServices.HRMCourseAttendeeStatus.Completed:
                        statusDescription = $"Your training \"{this.Training.Name}\" is completed";
                        statusType = NotificationType.Success;
                        break;
                    case KESSHRMServices.HRMCourseAttendeeStatus.Passed:
                        statusDescription = $"Your training \"{this.Training.Name}\" is passed";
                        statusType = NotificationType.Success;
                        break;
                    case KESSHRMServices.HRMCourseAttendeeStatus.WaitingList:
                        statusDescription = $"You are currently on waiting list for training \"{this.Training.Name}\"";                        
                        break;
                    case KESSHRMServices.HRMCourseAttendeeStatus.Cancelled:
                        statusDescription = $"Training registration \"{this.Training.Name}\" is cancelled";
                        statusType = NotificationType.Warning;
                        break;
                    case KESSHRMServices.HRMCourseAttendeeStatus.DropOut:
                        statusDescription = $"You have been dropped out from training \"{this.Training.Name}\"";
                        statusType = NotificationType.Error;
                        break;
                    default:
                        break;
                }

                new Notification(configuration, DB).Create(
                            this.EmployeeID,
                            statusDescription,
                             Notification.DEFAULT_SENDER,
                            NotificationModule.TRAINING_REGISTRATION,
                            NotificationAction.APPROVE,
                            statusType
                        ).Send();

                this.MongoDB = DB;
                this.Configuration = configuration;
                MongoDB.Save(this);
            }
        }
    }

    [BsonIgnoreExtraElements]
    public class TrainingReference {
        public long AXID { get; set; }
        public ReferenceType Type { get; set; }
        public string TypeDescription { 
            get {
                return this.Type.ToString();
            }  
        }
        public string Description { get; set; }
        public DateRange Validity { get; set; }
        public FieldAttachment Attachment { get; set; }
    }
}
