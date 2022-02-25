
using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Survey")]
    [BsonIgnoreExtraElements]
    public class Survey :  IMongoPreSave<Survey>
    {
        [BsonIgnore]
        [JsonIgnore]
        private IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        private IConfiguration Configuration;

        [BsonId]
        public string Id { get; set; }
        public string OdooID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateRange Schedule { get; set; }
        public SurveyRecurrent Recurrent { get; set; }
        public bool Mandatory { get; set; }
        public ParticipantType ParticipantType { get; set; }
        public List<string> Participants { get; set; } = new List<string>();
        public string Department { get; set; }

        public List<string> Departments { get; set; } = new List<string>();
        public bool Published { get; set; }
        public Uri SurveyUrl { get; set; }
        public Uri ReviewUrl { get; set; }
        public DateTime LastUpdate { get; set; }
        public string UpdateBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public bool AlreadyFilled { get; set; }
        public bool IsRequired { get; set; }
        public void PreSave(IMongoDatabase db)
        {            

            if (string.IsNullOrEmpty(this.Id))
                this.Id = ObjectId.GenerateNewId().ToString();

            if (CreatedDate.Year == 1)
                this.CreatedDate = DateTime.Now;

            this.LastUpdate = DateTime.Now;
            this.Schedule = Tools.NormalizeDateRangeUtc(this.Schedule);
        }

         public Survey() : base() { }

        public Survey(IMongoDatabase mongoDB, IConfiguration configuration) { 
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public List<Survey> Get()
        {
            var tasks = new List<Task<TaskRequest<List<Survey>>>>();
            tasks.Add(Task.Run(() => {
                var result = this.MongoDB.GetCollection<Survey>()
                .Find(x => true)
                .ToList();
                Console.WriteLine("Get Survey MongoDB:" + result.ToJson());
                return TaskRequest<List<Survey>>.Create("DB", result);
            }));
            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                throw e;
            }
            var results = new List<Core.Model.Survey>();
            if (t.Status == TaskStatus.RanToCompletion)
            {   
                foreach (var r in t.Result)
                    if (r.Label == "DB")
                        results = r.Result;
            }
            return results;
        }

        public List<Survey> GetWithFilter(String EmployeeID)
        {
            var tasks = new List<Task<TaskRequest<List<Survey>>>>();
            tasks.Add(Task.Run(() => {
                var result = this.MongoDB.GetCollection<Survey>()
                .Find(x => x.IsRequired == true && x.Participants.Contains(EmployeeID) && x.Published == true)
                .ToList();
                Console.WriteLine("Get Survey MongoDB:" + result.ToJson());
                return TaskRequest<List<Survey>>.Create("DB", result);
            }));
            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                throw e;
            }
            var results = new List<Core.Model.Survey>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                    if (r.Label == "DB")
                        results = r.Result;
            }
            return results;
        }

        // Currenly not used
        public List<Survey> GetS(DateRange range) {
            var newRange = Tools.normalizeFilter(range);
            var tasks = new List<Task<TaskRequest<List<Survey>>>>();
            
            
            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new SurveyAdapter(Configuration);
                var result =  adapter.GetS(newRange.Start, newRange.Finish);
                return TaskRequest<List<Survey>>.Create("Odoo", result);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {                
                var result= this.MongoDB.GetCollection<Core.Model.Survey>()
                .Find(x => (x.Schedule.Start >= newRange.Start && x.Schedule.Start <= newRange.Finish)
                 || (x.Schedule.Finish >= newRange.Start && x.Schedule.Finish <= newRange.Finish))
                .ToList();
                return TaskRequest<List<Survey>>.Create("DB", result);
            }));

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception e)
            {
                throw e;
            }

            // Combine result
            var results = new List<Core.Model.Survey>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var odooSurveys = new List<Survey>();
                var surveys = new List<Survey>();
                foreach (var r in t.Result)
                    if (r.Label == "Odoo")
                        odooSurveys = r.Result;
                    else
                        surveys = r.Result;

                //foreach (var result in t.Result)
                //{
                //    results.AddRange(result);
                //}
            }

            return results;
        }

        public List<SurveySchedule> Get(String employeeID)
        {
            List<SurveySchedule> result = this.MongoDB.GetCollection<Core.Model.SurveySchedule>()
                .Find(x => (x.Done == false) && x.ParticipantID == employeeID)
                .ToList();

            return result;
        }

        public List<SurveySchedule> GetOne(String emp) {
            DateTime today = DateTime.Now;
            return this.MongoDB.GetCollection<SurveySchedule>()
                .Find(x => (x.Done == false) && x.ParticipantID == emp && x.SurveyDate >= today.AddDays(-1))
                .ToList();
        }


        public List<SurveySchedule> GetRange(String employeeID, DateRange dateRange)
        {
            NormalDateRange range = Tools.normalizeFilter(dateRange);
            List<SurveySchedule> result = this.MongoDB.GetCollection<Core.Model.SurveySchedule>()
                .Find(x => (x.Done == false) && (x.SurveyDate >= dateRange.Start && x.SurveyDate <= dateRange.Finish) && x.ParticipantID == employeeID)
                .ToList();
            return result;
        }
    }

    public enum SurveyRecurrent : int
    {
        Once = 1, Daily = 2, Weekly = 3, Monthly = 4, Yearly = 5
    }
    
    public enum ParticipantType : int
    {
        All = 1, Department = 2, Selected = 3
    }

    public class SurveyForm
    {
        public string Id { get; set; }
        public int Picker { get; set; }
    }

    public class PackageSurveyObject
    {
        [JsonProperty(PropertyName = "survey.survey")]
        public OdooSurvey DataSurvey { get; set; }
    }
    public class PackageSurvey
    {
        [JsonProperty(PropertyName = "survey.survey")]
        public List<OdooSurvey> DataSurvey { get; set; }
    }

    public class OdooSurvey
    {
        public int Id { get; set; }
        public string DBId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public bool Active { get; set; }

        public bool Certificate { get; set; }

        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }

        [JsonProperty(PropertyName = "access_mode")]
        public string AccessMode { get; set; }

        [JsonProperty(PropertyName = "public_url")]
        public System.Uri PublicUrl { get; set; }

        public System.Uri SurveyUrl { get; set; }
        public int Port { get; set; }

        public string State { get; set; }

        public bool IsRequired { get; set; }

        [JsonProperty(PropertyName = "answer_count")]
        public int AnswerCount { get; set; }

        [JsonProperty(PropertyName = "answer_done_count")]
        public int AnswerDoneCount { get; set; }

        [JsonProperty(PropertyName = "scoring_type")]
        public string ScoringType { get; set; }

        [JsonProperty(PropertyName = "passing_score_type")]
        public string PassingScoreType { get; set; }

        [JsonProperty(PropertyName = "thank_you_message")]
        public string ThankMessage { get; set; }

        [JsonProperty(PropertyName = "create_date")] 
        public DateTime OdooCreatedDate { get; set; }

        public DateTime DBCreatedDate { get; set; }

        public DateRange Schedule { get; set; }
        public SurveyRecurrent Recurrent { get; set; }
        public ParticipantType ParticipantType { get; set; }
        public List<string> Participants { get; set; } = new List<string>();
        public string Department { get; set; }
        public List<string> Departments { get; set; }
        public bool Published { get; set; }

        //public int TodayAnswers { get; set; }
    }

    public class ListSurveyAnswer
    {
        [JsonProperty(PropertyName = "survey.user_input_line")]
        public List<OdooSurveyAnswer> OdooSurveyAnswers { get; set; }
    }

    public class OdooSurveyAnswer
    {
        [JsonProperty(PropertyName = "Id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "survey_id")]
        public ArrayList SurveyId { get; set; }

        [JsonProperty(PropertyName = "user_input_id")]
        public ArrayList UserInputId { get; set; }

        [JsonProperty(PropertyName = "question_id")]
        public ArrayList QuestionId { get; set; }

        [JsonProperty(PropertyName = "page_id")]
        public bool PageId { get; set; }

        [JsonProperty(PropertyName = "value_text")]
        public bool ValueText { get; set; }

        [JsonProperty(PropertyName = "value_free_text")]
        public bool ValueFreeText { get; set; }

        [JsonProperty(PropertyName = "value_suggested")]
        public ArrayList ValueSugested { get; set; }

        [JsonProperty(PropertyName = "value_suggested_row")]
        public bool valueSuggestedRow { get; set; }

        [JsonProperty(PropertyName = "answer_score")]
        public decimal AnswerScore { get; set; }

        [JsonProperty(PropertyName = "answer_is_correct")]
        public bool AnswerIsCorrect { get; set; }

        [JsonProperty(PropertyName = "create_date")]
        public DateTime CretaeDate { get; set; }
    }

    public class GridDateRangeSurvey
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string OdooId { get; set; }
        public DateRange Range { get; set; }

    }

}
