using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace KANO.Core.Model
{
    [Collection("Subscription")]
    [BsonIgnoreExtraElements]
    public class Subscription : BaseT, IMongoPreSave<Location>, IMongoExtendedPostSave<Location>, IMongoExtendedPostDelete<Location>
    {
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        [BsonId]
        public ObjectId Id { get; set; }
        public string EntityID { get; set; }
        public ObjectId LicenseID { get; set; }
        public Nullable<DateTime> ExpiredDate { get; set; }
        public string PONumber { get; set; }
        public string SubscriptionStatus { get; set; }

        public string CreatedBy { get; set; }

        public Nullable<DateTime> CreatedDate { get; set; }

        public Nullable<DateTime> LastUpdatedDate { get; set; }
        public string Status { get; set; }

        public Subscription() { }

        public Subscription(IMongoDatabase mongoDB, IConfiguration configuration)
        {
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public void PreSave(IMongoDatabase db)
        {
        }

        public void PostSave(IMongoDatabase db, Location originalObject)
        {
        }

        public void PostDelete(IMongoDatabase db, Location originalObject)
        {
        }
        public class SubscriptionMap
        {
            [JsonProperty(PropertyName = "expiredDate")]
            public Nullable<DateTime> ExpiredDate { get; set; }
            [JsonProperty(PropertyName = "maxEntity")]
            public int MaxEntity { get; set; }
            [JsonProperty(PropertyName = "createdEntity")]
            public int CreatedEntity { get; set; }
        }
    }
}
