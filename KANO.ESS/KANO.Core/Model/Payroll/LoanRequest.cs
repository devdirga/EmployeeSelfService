using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using RestSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace KANO.Core.Model
{
    [Collection("LoanRequest")]
    [BsonIgnoreExtraElements]
    public class LoanRequest : BaseDocumentVerification, IMongoPreSave<LoanRequest>
    {
        public string IdSimulation { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public string Position { get; set; }
        public string Department { get; set; }
        public double NetIncome { get; set; }
        public LoanType Type { get; set; }
        public DateTime RequestDate { get; set; } = new DateTime();
        public double LoanValue { get; set; }
        public DateRange LoanSchedule { get; set; }
        public int PeriodeLength { get; set; }
        public double CompensationValue { get; set; }
        public double InstallmentValue { get; set; }
        public double IncomeAfterInstallment { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);

            if (string.IsNullOrWhiteSpace(this.Id)) {
                var sequenceNo = "LR-"+this.EmployeeID + "-" + SequenceNo.Get(db, $"LoanRequest-{this.EmployeeID}").ClaimAsInt(db).ToString("0000");
                this.Id = sequenceNo;
            }
            if (string.IsNullOrWhiteSpace(this.IdSimulation))
            {
                var simNo = DateTime.Now.ToString("yyyyMMdd") + "-" + this.EmployeeID + "-" + SequenceNo.Get(db, $"LoanRequest-{this.EmployeeID}").ClaimAsInt(db).ToString("000");
                this.IdSimulation = simNo;
            }
        }
    }

    public class LoanType
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int MinimumRangePeriode { get; set; }
        public int MaximumRangePeriode { get; set; }
        public double MaximumLoan { get; set; }
        public double MinimumLimitLoan { get; set; }
        public LoanTypeDetail Detail { get; set; }
        public List<string> Email { get; set; }
    }

    public class LoanTypeDetail
    {
        public string IdDetail { get; set; } 
        public string IdLoan { get; set; }
        public LoanTypeName IdLoanType { get; set; }
        public string LoanTypeName { get; set; }
        public string PeriodeName { get; set; }
        public int MinimumRangePeriode { get; set; }
        public int MaximumRangePeriode { get; set; }
        public decimal Interest { get; set; }
        public LoanMethod Methode { get; set; }
        public string MethodeName { get; set; }
        public LoanPeriodType PeriodType { get; set; }
        public int MinimumRangeLoanPeriode { get; set; }
        public int MaximumRangeLoanPeriode { get; set; }
        public double MaximumLoad { get; set; }
    }

    public enum LoanMethod
    {
        Normal = 1,
        Kompensasi = 2,
    }

    public enum LoanPeriodType
    {
        Range,
        Fixed
    }

    public enum LoanTypeName
    {
        [Description("Reguler")]
        Reguler = 1,
        [Description("Uang Tambahan")]
        UangTambahan = 2,
        [Description("Data Mitra")]
        DanaMitra = 3
    }

    public class MLoanMethod
    {
        public int Id { set; get; }
        public String Name { set; get; }

    }
}
