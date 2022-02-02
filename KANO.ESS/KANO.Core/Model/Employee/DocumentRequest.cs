using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace KANO.Core.Model
{         
    [Collection("DocumentRequest")]
    [BsonIgnoreExtraElements]
    public class DocumentRequest : Document, IMongoPreSave<DocumentRequest>
    {                
        public DateTime RequestDate { get; set; }        
        public DateRange ValidDate { get; set; }

        public DocumentRequest() : base() { }

        public DocumentRequest(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public void StatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
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
                    var result = MongoDB.GetCollection<DocumentRequest>().UpdateMany(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                        Builders<DocumentRequest>.Update
                            .Set(d => d.Status, newStatus),
                        updateOptions
                    );
                    return TaskRequest<object>.Create("documentRequest", result.IsAcknowledged);
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update document request data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"Document request is {newStatus.ToString()}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.EMPLOYEE,
                            NotificationAction.OPEN_EMPLOYEE_CERTIFICATE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }

        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id)) {
                var sequenceNo = "DRQ-" + this.EmployeeID + "-" + SequenceNo.Get(db, $"DocumentRequest-{this.EmployeeID}").ClaimAsInt(db).ToString("00000");
                this.Id = sequenceNo;
            }

            if (CreatedDate.Year == 1) 
                this.CreatedDate = DateTime.Now;

            this.LastUpdate = DateTime.Now;
        }

        public DocumentRequest GetByID(string id)
        {
            var res = MongoDB.GetCollection<DocumentRequest>().Find(a => a.Id == id).FirstOrDefault();
            return res;
        }
    }

    public class DocumentRequestForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

}
