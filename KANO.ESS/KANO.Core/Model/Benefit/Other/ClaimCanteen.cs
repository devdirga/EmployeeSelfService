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
    [Collection("ClaimCanteen")]
    [BsonIgnoreExtraElements]
    public class ClaimCanteen : IMongoPreSave<ClaimCanteen>
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime ClaimDate { get; set; } = DateTime.Now;
        public string CanteenUserID { get; set; }
        public string CanteenName { get; set; }
        public List<RedeemGroup> DataRedeem { get; set; }
        public string PaymentId { get; set; }
        public DateTime? DatePaid { get; set; }
        public RedeemStatus Status { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id))
            {
                var sequenceNo = "CL-" + DateTime.Now.ToString("yyyyMM") + SequenceNo.Get(db, "ClaimCanteen-"+ DateTime.Now.ToString("yyyyMM")).ClaimAsInt(db).ToString("000");
                this.Id = sequenceNo;
            }
        }

    }

    public class ClaimCanteenInfo
    {
        public long TotalClaimed { get; set; }
        public long TotalPaid { get; set; }
    }

    [Collection("PaymentClaim")]
    [BsonIgnoreExtraElements]
    public class PaymentClaim : IMongoPreSave<PaymentClaim>
    {
        [BsonId]
        public string Id { get; set; }
        public DateTime? DatePaid { get; set; }
        public List<ClaimCanteen> DataClaim { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id))
            {
                var sequenceNo = "PY-" + DateTime.Now.ToString("yyyyMM") + SequenceNo.Get(db, "PaymentClaim-" + DateTime.Now.ToString("yyyyMM")).ClaimAsInt(db).ToString("000");
                this.Id = sequenceNo;
            }
        }
    }
}
