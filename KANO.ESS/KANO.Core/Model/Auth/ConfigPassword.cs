using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using KANO.Core.Lib.Extension;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model.Auth
{
    [Collection("ConfigPassword")]
    [BsonIgnoreExtraElements]
    public class ConfigPassword : IMongoPreSave<ConfigPassword>
    {
        [BsonId]
        public string Id { set; get; }
        public int MinimumLength { get; set; }
        public int MustChangeDays { get; set; }
        public bool ContainUppercase { get; set; }
        public bool ContainLowercase { get; set; }
        public bool ContainNumeric { get; set; }
        public bool ContainSpecialCharacter { get; set; }
        public int Published { get; set; }

        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrWhiteSpace(this.Id))
            {
                this.Id = ObjectId.GenerateNewId().ToString();
            }
        }

    }
}
