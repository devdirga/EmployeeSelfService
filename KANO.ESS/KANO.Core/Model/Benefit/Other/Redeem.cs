using KANO.Core.Lib.Extension;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Redeem")]
    [BsonIgnoreExtraElements]
    public class Redeem : BaseT, IMongoPreSave<Redeem>
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime RedeemedAt { get; set; } = DateTime.Now;
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string CanteenID { get; set; }
        public string CanteenName { get; set; }
        public int RedeemedVoucherTotal { get; set; } = 0;
        public int CurrentTotal { get; set; } = 0;
        public string ClaimId { get; set; }
        public DateTime? ClaimDate { get; set; }
        public string PaymentId { get; set; }
        public DateTime? DatePaid { get; set; }
        public RedeemStatus Status { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            if (String.IsNullOrEmpty(this.EmployeeID))
                throw new Exception("Employee ID Cannot empty!");

            if (String.IsNullOrEmpty(this.CanteenID))
                throw new Exception("Canteen ID Cannot empty!");

            if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (string.IsNullOrEmpty(this.CanteenName))
            {
                var canteen = db.GetCollection<Canteen>().Find(x => x.Id == this.CanteenID).Limit(1).FirstOrDefault();
                if (canteen != null)
                {
                    this.CanteenName = canteen.Name;
                }
            }

            this.LastUpdate = DateTime.Now;
        }
    }

    public class RedeemGroup
    {
        public string RedeemDate { get; set; }
        public int SubTotal { get; set; }
    }
    public enum RedeemStatus
    {
        [Description("Request Claim")]
        Claim = 1,
        [Description("Voucher Paid")]
        Paid = 2
    }
}
