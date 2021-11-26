using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class SurveyHistory
    {
        [BsonId]
        public string Id { get; set; }
        public string SurveyID { get; set; }
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public string Title { get; set; }
        public SurveyFillingStatus FillingStatus { get; set; }
        public DateTime LastUpdate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
    }

    public enum SurveyFillingStatus : int
    {
        None = 0, Completed = 1, Partial = 2
    }
}
