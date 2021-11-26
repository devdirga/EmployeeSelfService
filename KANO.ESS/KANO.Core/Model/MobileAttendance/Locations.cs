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
    [Collection("Location")]
    [BsonIgnoreExtraElements]
    public class Locations: IMongoPreSave<Locations>
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public bool IsVirtual { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public string Tags { get; set; }
        public float Radius { get; set; }
        public LocationStatus Status { get; set; }
        public string CreatedByID { get; set; }

        [BsonIgnore]
        [JsonIgnore]
        public ObjectId CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string UpdatedByID { get; set; }
        public DateTime UpdatedDate { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrEmpty(this.Id.ToString()))
                this.Id = ObjectId.GenerateNewId();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.UpdatedDate = DateTime.Now;
        }
    }

    public enum LocationStatus
    {
        Active,
        InActive,
    }
}
