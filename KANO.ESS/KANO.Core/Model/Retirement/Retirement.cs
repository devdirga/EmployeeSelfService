
using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Retirement")]
    [BsonIgnoreExtraElements]
    public class Retirement : BaseDocumentVerification, IMongoPreSave<Retirement>
    {
        public string MPPID { get; set; }
        public DateTime BirthDate { get; set; }
        public DateRange Schedule { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }
        public MPPType MPPType { get; set; }
        public DateRange MPPDate { get; set; }
        public CBType CBType { get; set; }
        public DateRange CBDate { get; set; }
        public List<Retirement> Histories { get; set; } = new List<Retirement>();
        public KESSMPPServices.KESSMPPStatus MPPStatus { get; set; }
        [BsonIgnore]
        public MPPFlag Flag { get; set; }
        public string MPPStatusDescription { 
            get {
                return this.MPPStatus.ToString();
            } 
        }
        public string Description { get; set; }
        public List<FieldAttachment> Attachments { get; set; } = new List<FieldAttachment>();

        public Retirement() : base() { }

        public Retirement(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);
        }

        public Retirement Get(string employeeID)
        {
            var tasks = new List<Task<TaskRequest<Retirement>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new RetirementAdapter(this.Configuration);
                var retirement = adapter.Get(employeeID);
                return TaskRequest<Retirement>.Create("AX", retirement);
            }));

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var retirement = this.MongoDB.GetCollection<Retirement>().Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview || x.Status == UpdateRequestStatus.Approved)).FirstOrDefault();
                return TaskRequest<Retirement>.Create("MongoDB", retirement);
            }));

            // Fetch for initial 
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(Configuration);
                var employee = adapter.GetDetail(employeeID);
                // //FTP
                // if (employeeID == "7612050060")
                // {
                //     employee.LastEmploymentDate = DateTime.ParseExact("2021-08-01", "yyyy-MM-dd", null);
                // }

                var request = new Retirement
                {
                    EmployeeID = employee.EmployeeID,
                    EmployeeName = employee.EmployeeName,
                    Department = employee.Department,
                    Position = employee.Position,
                    BirthDate = employee.Birthdate,
                    MPPDate = new DateRange(default, employee.LastEmploymentDate),
                };

                return TaskRequest<Retirement>.Create("Init", request);
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
            Retirement initRequest = null, axRequest = null, outstandingRequest = null;
            if (t.Status == TaskStatus.RanToCompletion)
            {

                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        axRequest = r.Result;
                    else if (r.Label == "MongoDB")
                        outstandingRequest = r.Result;
                    else
                        initRequest = r.Result;

            }

            if (axRequest != null)
            {
                if (outstandingRequest != null) {
                    axRequest.AXRequestID = outstandingRequest.AXRequestID;
                }

                if (axRequest.MPPStatus != KESSMPPServices.KESSMPPStatus.Rejected)
                {
                    axRequest.Flag = MPPFlag.Requested;
                    return axRequest;
                }
            }

            if (outstandingRequest != null)
            {
                if (axRequest == null || (axRequest != null && axRequest.MPPStatus != KESSMPPServices.KESSMPPStatus.Rejected))
                {
                    outstandingRequest.Flag = MPPFlag.Outstanding;
                    return outstandingRequest;
                }
            }

            if (axRequest != null && axRequest.MPPStatus != KESSMPPServices.KESSMPPStatus.Rejected) {
                initRequest.MPPStatus = axRequest.MPPStatus;
            }
            initRequest.Flag = MPPFlag.Initialization;
            return initRequest;
        }

        public void StatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                Retirement retirement = null;
                var tasks = new List<Task<TaskRequest<object>>>();
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    retirement = MongoDB.GetCollection<Retirement>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).FirstOrDefault();
                    if (retirement == null) return TaskRequest<object>.Create("retirement", false);

                    var newFilePath = Tools.ArchiveFile(retirement.Filepath);
                    var result = MongoDB.GetCollection<Retirement>().UpdateOne(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                        Builders<Retirement>.Update
                            .Set(d => d.Status, newStatus)
                            .Set(d => d.Filepath, newFilePath),
                        updateOptions
                    );
                    return TaskRequest<object>.Create("retirement", result.IsAcknowledged);
                }));

                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    var updateRequest = MongoDB.GetCollection<UpdateRequest>()
                              .Find(x => x.AXRequestID == AXRequestID && x.EmployeeID == employeeID)
                              .FirstOrDefault();
                    updateRequest.Status = newStatus;
                    MongoDB.Save(updateRequest);
                    return TaskRequest<object>.Create("updateRequest", true);
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update retirement data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"MPP & CB request is {newStatus.ToString()}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.RETIREMENT,
                            NotificationAction.NONE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }                
                
                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }

        public Retirement GetByAXRequestID(string employeeID, string axRequestID)
        {

            var tasks = new List<Task<TaskRequest<object>>>();
            var adapter = new RetirementAdapter(this.Configuration);

            // Fetch data Travel
            tasks.Add(Task.Run(() =>
            {
                var retirement = this.MongoDB.GetCollection<Retirement>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();

                if (retirement == null)
                {
                    return TaskRequest<object>.Create("Retirement", null);
                }

                return TaskRequest<object>.Create("Retirement", retirement);
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
            if (t.Status == TaskStatus.RanToCompletion)
            {
                Retirement retirement = null;

                foreach (var r in t.Result)
                {
                    if (r.Label == "Retirement" && r.Result != null)
                    {
                        retirement = (Retirement)r.Result;
                    }
                }

                return retirement;
            }

            return null;

        }        

    }

    public enum MPPStatus : int
    {
        Rejected = 0,
        Approved = 1,
    }

    public enum MPPType : int
    {
        Bulan6 = 6,
        Bulan12 = 12,
    }
    
    public enum MPPFlag : int
    {
        Initialization = 1,
        Outstanding = 2,
        Requested = 3,
    }

    public enum CBType : int
    {
        Bulan2 = 2,
        Bulan3 = 3,
    }
        
    public class RetirementForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }    

}
