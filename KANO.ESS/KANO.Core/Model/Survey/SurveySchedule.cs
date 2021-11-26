
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
    [Collection("SurveySchedule")]
    [BsonIgnoreExtraElements]
    public class SurveySchedule : IMongoPreSave<SurveySchedule>
    {        
        [BsonId]
        public string Id { get; set; }
        public string OdooID { get; set; }
        public string SurveyID { get; set; }
        public string Title { get; set; }
        public string ParticipantID { get; set; } // User._id
        public SurveyRecurrent Recurrent { get; set; } // User._id
        public bool Done { get; set; }
        public bool Mandatory { get; set; }
        private DateTime _surveyDate { get; set;}
        public DateTime SurveyDate { set { 
                this._surveyDate = Tools.normalize(value);
                this.Week = Tools.WeekOfYearISO8601(value);
                this.Day = value.Day;
                this.Month = value.Month;
                this.Year = value.Year;
            } 

            get {
                return this._surveyDate;
            }
        }
        public int Week { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }        
        public DateTime CreatedDate { get; set; }

        public void PreSave(IMongoDatabase db)
        {

            if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;
        }
    }    
    
}
