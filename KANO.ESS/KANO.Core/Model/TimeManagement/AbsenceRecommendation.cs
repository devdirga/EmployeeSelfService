using KANO.Core.Lib.Extension;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace KANO.Core.Model
{
    [Collection("AbsenceRecommendation")]
    [BsonIgnoreExtraElements]
    public class AbsenceRecommendation : BaseDocumentVerification, IMongoPreSave<AbsenceRecommendation>
    {
       
        public string AbsenceCode { get; set; }
        public string EmployeeId { get; set; }
        public DateTime Date { get; set; }
        public string Clockin { get; set; }
        public string Clockout { get; set; }
        public string Start { get; set; }
        public string Finish { get; set; }

        public void PreSave(IMongoDatabase db)
        {
            if (String.IsNullOrEmpty(EmployeeId))
                throw new Exception("Employee ID Cannot empty!");

            if (string.IsNullOrWhiteSpace(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();
            
        }
        public class IdentificationForm
        {
            public string JsonData { get; set; }
            public IEnumerable<IFormFile> FileUpload { get; set; }
        }
    }
}
