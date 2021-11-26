using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using KANO.Core.Service;

namespace KANO.Core.Model
{
    [Collection("Tickets")]
    [BsonIgnoreExtraElements]
    public class TicketRequest : BaseDocumentVerification, IMongoPreSave<TicketRequest>
    {
        public string Subject { get; set; }
        public string TicketCategory { get; set; }
        public string EmailCC { get; set; }
        public List<string> EmailTo { get; set; }
        public string EmailFrom { get; set; }
        public TicketType TicketType { get; set; }
        public string FullName { get; set; }
        public TicketMedia TicketMedia { get; set; }
        public string Description { get; set; }
        public DateTime TicketDate { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public FieldAttachment Attachments { get; set; } = new FieldAttachment();
        public TicketRequest() : base() { }
        public TicketRequest(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public new void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);
            // var sequenceNo = this.EmployeeID + "/" + SequenceNo.Get(db, "TicketRequest").ClaimAsInt(db).ToString("000000");
            // this.Id = sequenceNo;
        }

        public List<TicketResult> Get(string EmployeeID)
        {
            var tasks = new List<Task<TaskRequest<List<TicketRequest>>>> {
            Task.Run(() =>
            {
                return TaskRequest<List<TicketRequest>>.Create("DB", this.MongoDB.GetCollection<TicketRequest>()
                .Find(x => x.EmployeeID == EmployeeID)
                .ToList());
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

            var result = new List<TicketResult>();

            if (t.Status == TaskStatus.RanToCompletion)
            {
                var Ticket = new List<TicketRequest>();
                var TicketUpdateRequest = new List<TicketRequest>();

                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        Ticket = r.Result;
                    else
                        TicketUpdateRequest = r.Result;

                foreach (var f in Ticket)
                {
                    result.Add(new TicketResult
                    {
                        Ticket = f,
                    });
                }

                foreach (var fur in TicketUpdateRequest)
                {
                    result.Add(new TicketResult
                    {
                        UpdateRequest = fur,
                    });
                }

            }

            return result;
        }

        public TicketResult GetByAXID(long AXID, bool essInternalOnly = true)
        {
            var result = new TicketResult();
            var tasks = new List<Task<TaskRequest<TicketRequest>>> {
// Fetch data from AX

// Fetch data from DB
Task.Run(() => {
var ticketrequest = this.MongoDB.GetCollection<TicketRequest>()
.Find(x => x.AXID == AXID)
.FirstOrDefault();
return TaskRequest<TicketRequest>.Create("DB", ticketrequest);
})
};

            // Run process concurently
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
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        result.Ticket = r.Result;
                    else
                        result.UpdateRequest = r.Result;
            }

            return result;
        }

        public TicketRequest GetByAXRequestID(string EmployeeID, string AXRequestID)
        {
            return this.MongoDB.GetCollection<TicketRequest>()
            .Find(x => x.EmployeeID == EmployeeID && x.AXRequestID == AXRequestID)
            .FirstOrDefault();
        }

        public TicketRequest GetByID(string ID)
        {
            return this.MongoDB.GetCollection<TicketRequest>()
            .Find(x => x.Id == ID)
            .FirstOrDefault();
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

                // Update Tickets
                tasks.Add(Task.Run(() =>
                {
                    var ticket = MongoDB.GetCollection<TicketRequest>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).FirstOrDefault();
                    if (ticket == null) return TaskRequest<object>.Create("ticket", false);

                    var newFilepath = Tools.ArchiveFile(ticket.Filepath);
                    var result = MongoDB.GetCollection<TicketRequest>().UpdateOne(
                    x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                    Builders<TicketRequest>.Update
                    .Set(d => d.Status, newStatus)
                    .Set(d => d.Filepath, newFilepath),
                    updateOptions
                    );
                    return TaskRequest<object>.Create("ticket", result.IsAcknowledged);

                }));

                // Update Tickets
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
                    Console.WriteLine($"Unable to update ticket request data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    var displayedStatus = newStatus;

                    if (newStatus == UpdateRequestStatus.Approved)
                    {
                        displayedStatus = UpdateRequestStatus.Rejected;
                    }

                    if (newStatus == UpdateRequestStatus.Rejected)
                    {
                        displayedStatus = UpdateRequestStatus.Approved;
                    }

                    new Notification(Configuration, MongoDB).Create(
                    request.EmployeeID,
                    $"Complaint has been {displayedStatus.ToString()}",
                    Notification.DEFAULT_SENDER,
                    NotificationModule.COMPLAINT,
                    NotificationAction.NONE,
                    Notification.MapUpdateRequestStatus(displayedStatus) // New Status
                    ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }
    }
    public enum TicketStatus : int
    {
        Open = 0,
        Progress = 1,
        Closed = 2
    }
    public enum TicketMedia : int
    {
        Email = 0,
        Telephone = 1,
        WalkInCustomer = 2,
        Other = 3
    }
    public enum TicketType : int
    {
        Complaint = 0,
        Question = 1,
        Incident = 2,
        FutureRequest = 3,
        // Other = 4
    }
    public class TicketResult
    {
        public TicketRequest Ticket { get; set; } = null;
        public TicketRequest UpdateRequest { get; set; } = null;
    }
    public class TicketForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    [Collection("MailTemplate")]
    [BsonIgnoreExtraElements]
    public class ComplaintMailTemplate
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        public ComplaintMailTemplate() : base() { }
        public ComplaintMailTemplate(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
        }

        [BsonId]
        public string Id { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime TimeUpdate { get; set; } = Tools.ToUTC(DateTime.Now);
        public string UserIDUpdate { get; set; }
        public string UserNameUpdate { get; set; }

        public ComplaintMailTemplate GetTemplate()
        {
            return DB.GetCollection<ComplaintMailTemplate>().Find(x => x.Id == "ComplaintTemplate").FirstOrDefault();
        }
    }
}