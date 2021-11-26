using KANO.Core.Lib.Extension;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("City")]
    [BsonIgnoreExtraElements]
    public class City: IMongoPreSave<City>
    {
        public string Id { set; get; }
        public long AXID { set; get; }
        public string Name { set; get; }
        public string Description { set; get; }

        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id)) {
                this.Id = ObjectId.GenerateNewId().ToString();
            }
        }
    }
}
