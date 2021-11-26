using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Events")]
    [BsonIgnoreExtraElements]
    public class Events : IMongoPreSave<Events>
    {
        [BsonId]
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string LocationID { get; set; }
        public Attendees[] Attendees { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.UpdatedDate = DateTime.Now;
        }
    }

    public class Attendees
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
