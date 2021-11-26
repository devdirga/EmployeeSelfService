using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using RestSharp;
using System.ComponentModel;
using Microsoft.Extensions.Configuration;

namespace KANO.Core.Model
{
    [Collection("ConfigurationLoan")]
    [BsonIgnoreExtraElements]
    public class ConfigurationLoan: BaseT, IMongoPreSave<ConfigurationLoan>
    {
        [BsonId]
        public string Id { get; set; }
        public string Name { get; set; }
        public int MinimumRangePeriode { get; set; }
        public int MaximumRangePeriode { get; set; }
        public double MaximumLoan { get; set; }
        public List<LoanTypeDetail> Detail { get; set; }
        public List<string> Email { get; set; }
        //public LoanTypeDetail[] DetailType { get; set; }
        public double MinimumLimitLoan { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id))
            {
                var sequenceNo = "Conf-" + SequenceNo.Get(db, "ConfigurationLoan").ClaimAsInt(db).ToString("00");
                this.Id = sequenceNo;
            }
            var index = 0;
            foreach (var x in this.Detail)
            {
                if (string.IsNullOrEmpty(x.IdDetail))
                    this.Detail[index].IdDetail = ObjectId.GenerateNewId().ToString();
                if (string.IsNullOrEmpty(x.IdLoan))
                    this.Detail[index].IdLoan = this.Id;
                if (string.IsNullOrEmpty(x.LoanTypeName))
                    this.Detail[index].LoanTypeName = this.Name;
                index++;

            }
            //if (string.IsNullOrEmpty(this.Id))
            //    this.Id = ObjectId.GenerateNewId().ToString();
            this.LastUpdate = Tools.ToUTC(DateTime.Now);
            /*
            var sequenceNo = this.Id + "-" + SequenceNo.Get(db, "ConfigurationLoan").ClaimAsInt(db).ToString("00");
            this.Id = sequenceNo;
            */
        }
    }

    [Collection("MailTemplate")]
    [BsonIgnoreExtraElements]
    public class LoanMailTemplate
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        public LoanMailTemplate() : base(){}
        public LoanMailTemplate(IMongoManager mongo, IConfiguration conf)
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

        public LoanMailTemplate GetTemplate()
        {
            return DB.GetCollection<LoanMailTemplate>().Find(x => x.Id == "LoanTemplate").FirstOrDefault();
        }
    }

    public enum PeriodeType {
        Year,
        Month
    }
}
