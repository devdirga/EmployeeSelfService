using KANO.Core.Lib.Extension;
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
    [Collection("Approval")]
    [BsonIgnoreExtraElements]
    public class Approval : BaseT, IMongoPreSave<Approval>
    {
        private IMongoDatabase MongoDB;
        private IConfiguration Configuration;

        [BsonId]
        public string Id { set; get; }
        public string EmployeeID { set; get; }
        public InternalInformation Meta { set; get; } = new InternalInformation();
        public UpdateRequestStatus Status { set; get; } = UpdateRequestStatus.InReview;
        public bool Done { set; get; } = false;
        public DateRange Validity { set; get; }
        public DateTime CreatedDate { set; get; }

        public Approval(IMongoDatabase db, IConfiguration config) {
            MongoDB = db;
            Configuration = config;
        }

        public Approval Create(string employeeID, string AXRequestID, string ESSID, long AXID, string CollectionName) {
            this.EmployeeID = employeeID;
            this.Meta = new InternalInformation {
                AXRequestID = AXRequestID,
                ESSID = ESSID,
                AXID = AXID,
                C = CollectionName,
        };
            
            return this;
        }

        public Approval Approved()
        {
            this.SetStatus(UpdateRequestStatus.Approved);
            return this;
        }

        public Approval Rejected()
        {
            this.SetStatus(UpdateRequestStatus.Rejected);
            return this;
        }

        public Approval SetStatus(UpdateRequestStatus status)
        {
            this.Status = status;
            this.Done = true;
            return this;
        }

        public Approval Save()
        {
            this.MongoDB.Save(this);
            return this;
        }

        public Approval RequestApproval(string employeeID, string message, string module)
        {
            var notification = new Notification(Configuration, MongoDB);
            notification.Meta = this.Meta;
            notification.Create(employeeID, message, module, NotificationType.Warning);
            notification.Send();

            return this;
        }

        public Approval Get(string employeeID, string AXRequestID)
        {
            var approval = this.MongoDB.GetCollection<Approval>().Find(x => x.EmployeeID == employeeID && x.Meta != null && x.Meta.AXRequestID == AXRequestID).FirstOrDefault();
            return approval;
        }

        public bool IsValid(string employeeID, string AXRequestID)
        {
            var approval = this.Get(employeeID, AXRequestID);
            return approval.Validity.Start >= DateTime.Now && approval.Validity.Finish <= DateTime.Now;
        }

        public bool IsDone(string employeeID, string AXRequestID)
        {
            var approval = this.Get(employeeID, AXRequestID);
            return approval.Done;
        }

        public void PreSave(IMongoDatabase db)
        {
            if (String.IsNullOrEmpty(this.EmployeeID))
                throw new Exception("Employee ID Cannot empty!");           

            if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.LastUpdate = DateTime.Now;
        }
    }
}
