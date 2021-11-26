using KANO.Core.Lib.Extension;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Document")]
    [BsonIgnoreExtraElements]
    public class Document: BaseDocumentVerification, IMongoPreSave<Document>
    {                
        public string DocumentType { get; set; }
        public string Description { get; set; }        
        public string Notes { get; set; }

        public Document() : base() { }

        public Document(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public List<DocumentResult> GetS(string employeeID)
        {
            var tasks = new List<Task<TaskRequest<List<Document>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new EmployeeAdapter(this.Configuration);
                var documents = adapter.GetDocuments(employeeID);
                return TaskRequest<List<Document>>.Create("AX", documents);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var documents = this.MongoDB.GetCollection<Document>()
                                .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview))
                                .ToList();
                return TaskRequest<List<Document>>.Create("DB", documents);
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
            var result = new List<DocumentResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var Families = new List<Document>();
                var FamiliesUpdateRequest = new List<Document>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        Families = r.Result;
                    else
                        FamiliesUpdateRequest = r.Result;

                // Merge documents data
                foreach (var f in Families)
                {
                    result.Add(new DocumentResult
                    {
                        Document = f,
                    });
                }

                foreach (var fur in FamiliesUpdateRequest)
                {
                    if (fur.AXID > 0)
                    {
                        var f = result.Find(x => x.Document.AXID == fur.AXID);
                        if (f != null)
                            f.UpdateRequest = fur;
                    }
                    else
                    {
                        result.Add(new DocumentResult
                        {
                            UpdateRequest = fur,
                        });
                    }
                }
            }
            return result;
        }

        public DocumentResult Get(string employeeID, long axID, bool essInternalOnly = false)
        {
            var result = new DocumentResult();
            var tasks = new List<Task<TaskRequest<Document>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                if (essInternalOnly)
                {
                    return TaskRequest<Document>.Create("AX", new Document());
                }
                var adapter = new EmployeeAdapter(Configuration);
                var document = adapter.GetDocuments(employeeID)
                                    .Find(x => x.AXID == axID);
                return TaskRequest<Document>.Create("AX", document);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var document = this.MongoDB.GetCollection<Document>()
                                .Find(x => x.EmployeeID == employeeID && x.AXID == axID && (x.Status == UpdateRequestStatus.InReview))
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

            return result;
        }

        public void Discard(string requestID)
        {
            var document = this.MongoDB.GetCollection<Document>().Find(x => x.Id == requestID).FirstOrDefault();
            if (document != null)
            {
                this.MongoDB.Delete(document);
                if (System.IO.File.Exists(document.Filepath))
                {
                    System.IO.File.Delete(document.Filepath);
                }
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

                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    var result = MongoDB.GetCollection<Document>().UpdateMany(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                        Builders<Document>.Update
                            .Set(d => d.Status, newStatus),
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
                            $"Document update is {newStatus.ToString()}",
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
    }

    public class DocumentResult
    {
        public Document Document { get; set; } = null;
        public Document UpdateRequest { get; set; } = null;
    }

    public class DocumentForm
    {
        public string JsonData { get; set; } = null;
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }
}
