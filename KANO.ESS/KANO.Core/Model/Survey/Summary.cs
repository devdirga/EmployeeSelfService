using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class SurveySummary
    {
        [BsonId]
        public string Id { get; set; }
        public string SurveyID { get; set; }        
        public string Title { get; set; }
        public DateRange Schedule { get; set; }
        public int Recurrent { get; set; }
        public string RecurrentDescription { get; set; }
        public bool Mandatory { get; set; }
        public int ParticipantType { get; set; }
        public string ParticipantTypeDescription { get; set; }
        public Participant[] Participants { get; set; }
        public string Department { get; set; }
        public bool Published { get; set; }
        public int TotalParticipants { get; set; }
        public int TotalParticipantsTakenSurvey { get; set; }
    }

    public class Participant
    {
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public bool Done { get; set; }
    }


}
