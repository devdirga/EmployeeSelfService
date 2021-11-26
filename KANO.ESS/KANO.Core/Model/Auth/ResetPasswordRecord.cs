using System;
using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace KANO.Core.Model
{
    [Collection("ResetPasswordRecord")]
    [BsonIgnoreExtraElements]
    public class ResetPasswordRecord
    {
        [BsonId]
        public string Id { get; set; }
        public string Email { get; set; }
        public User Users { get; set; }
        public DateTime ExpiredOn { get; set; }        
    }
}
