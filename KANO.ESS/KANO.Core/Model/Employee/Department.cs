using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Department")]
    [BsonIgnoreExtraElements]
    public class Department
    {
        [BsonId]
        public string Id { get; set; }
        public string EmployeeName { get; set; }
        public string OperationUnitNumber { get; set; }
        public string DepartmentName { get; set; }
        public object Description { get; set; }

        public DateTime LastUpdate { get; set; }

        //public void PreSave(IMongoDatabase db)
        //{
        //    if (string.IsNullOrEmpty(this.Id))
        //        this.Id = ObjectId.GenerateNewId().ToString();

        //    this.LastUpdate = DateTime.Now;
        //}
    }
}
