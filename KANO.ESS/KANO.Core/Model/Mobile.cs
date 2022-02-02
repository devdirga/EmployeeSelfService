using System;

using KANO.Core.Lib.Extension;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace KANO.Core.Model
{
    [Collection("Mobile")]
    [BsonIgnoreExtraElements]
    public class Mobile : BaseDocumentVerification, IMongoPreSave<Mobile>
    {
        public String Type { get; set; }
        public String Version { get; set; }
        public DateTime UpdatedDate { get; set; }
        public Mobile() : base() { }
        public Mobile(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }
        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();
        }
    }
}
