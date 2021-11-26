using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using RestSharp;
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using KANO.Core.Lib.Helper;

namespace KANO.Core.Model
{
    [Collection("Voucher")]
    [BsonIgnoreExtraElements]
    public class Voucher : BaseT, IMongoPreSave<Voucher>
    {
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        public Voucher() { }

        public Voucher(IMongoDatabase mongoDB, IConfiguration configuration)
        {
            MongoDB = mongoDB;
            Configuration = configuration;
        }


        [BsonId]
        public string Id { get; set; }
        public DateTime ExpiredDate { get; set; } 
        public string EmployeeID { get; set; } 
        public string EmployeeName { get; set; } 
        public string CanteenID { get; set; } = null;
        public bool Used { get; set; } = false;
        public DateTime? UsedDate { get; set; }
        public string Note { get; set; }
        public DateTime GeneratedForDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            if (String.IsNullOrEmpty(this.EmployeeID))
                throw new Exception("Employee ID Cannot empty!");

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            // Check generated for date
            var now = DateTime.Now;
            var nowDate = new DateTime(now.Year, now.Month, now.Day,0,0,0, DateTimeKind.Local);
            if (GeneratedForDate.Year == 1)
                this.GeneratedForDate = nowDate;
            //else if (GeneratedForDate > nowDate)
            //    throw new Exception("Cannot generate voucher for future date");
            //else if (Math.Abs(GeneratedForDate.Subtract(nowDate).Days) > 30)
            //    throw new Exception("Cannot generate 30 days older voucher");
            


            // Generate voucher based on date generated for and employee ID
            // Therefore it wouldn't be generated twice
            var strDate = this.GeneratedForDate.ToUniversalTime().ToString("dd-MM-yyyy");
            var id = Hasher.Encrypt($"{strDate}_{this.EmployeeID}");
            //var id = $"{strDate}_{this.EmployeeID}";
            var voucher = db.GetCollection<Voucher>().Find(x=>x.Id == id).FirstOrDefault();
            if (voucher != null) {
                throw new Exception($"Voucher has been already generated for {strDate}");
            }

            this.Id = id;
            this.ExpiredDate = this.GeneratedForDate.AddMonths(1);
            this.LastUpdate = DateTime.Now;
        }
        
        public static void ClaimVoucher(IMongoDatabase db, string canteenID, int total)
        {
        //    Voucher sn = db.GetCollection<Voucher>().Find(x => x.Id == Id).FirstOrDefault();  
        //    if (!String.IsNullOrEmpty(sn.EmployeeID))
        //    {
        //        if (commit)
        //        {
        //            sn.EmployeeID = employeeId;
        //            db.Save<Voucher>(sn);
        //            return true;
        //        }

        //    }
        //    return false;
        }

        public static void GenerateVoucher(IMongoDatabase db, string employeeID, string employeeName, DateTime expirationDate, string note = "")
        {
            var voucher = new Voucher();
            voucher.EmployeeID = employeeID;
            voucher.EmployeeName = employeeName;
            voucher.ExpiredDate = expirationDate;
            voucher.Note = note;
            db.Save(voucher);
        }
    }

    public class VoucherInfo
    {
        public long VoucherUsed { get; set; }
        public long VoucherRemaining { get; set; }
        public long VoucherExpired { get; set; }
        public long VoucherAlmostExpired { get; set; }
    }
    public class NoVoucher
    {
        public DateTime EventDate { get; set; }
        public ActionVoucher ActionOnEvent { get; set; }
    }

    public enum ActionVoucher
    {
        GenerateVoucher = 1,
        RequestVoucher = 2
    }

    public class Absent
    {
        public string EmployeeID { get; set; }
        public DateTime LoggedDate { get; set; }
        public string AbsenceCode { get; set; }
    }
}
