using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Model;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using RestSharp;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace KANO.Core.Model
{
    public class MedicalBenefitDetail
    {                
        public long AXID { get; set; }
        public string TypeID { get; set; }                   
        public string Description { get; set; }
        public double Amount { get; set; } = 0.0;
        public FieldAttachment Attachment { get; set; } = new FieldAttachment();        
    }
    public class MedicalBenefitDocumentType {
        public long AXID { get; set; }
        public string Description { get; set; }
        public int TypeID { get; set; }
    }
}
