using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("ActivationRecord")]
    [BsonIgnoreExtraElements]
    public class ActivationRecord
    {
        [BsonId]
        public string Id { get; set; }
        public string Email { get; set; }
        public Employee Employee { get; set; }
        public DateTime ExpiredOn { get; set; }
    }
}
