using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Globalization;
using Newtonsoft.Json;
using MongoDB.Bson.Serialization;

namespace KANO.Api.Survey.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyController : ControllerBase
    {

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Core.Model.Survey _survey;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public SurveyController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _survey = new Core.Model.Survey(DB, Configuration);
        }

        [HttpPost("range")]
        public IActionResult GetS([FromBody] DateRange range)
        {
            try
            {
                var r = Tools.normalizeFilter(range);
                var results = this.DB.GetCollection<Core.Model.Survey>().Find(x=>true)
                //.Find(x =>
                //    (x.Schedule.Start >= r.Start && x.Schedule.Start <= r.Finish)
                //    || 
                //    (x.Schedule.Finish >= r.Start && x.Schedule.Finish <= r.Finish)
                // )
                .ToList();
                return ApiResult<List<Core.Model.Survey>>.Ok(results);
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while loading survey :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("{id}")]
        public IActionResult Get(string id)
        {
            try
            {
                var result = this.DB.GetCollection<Core.Model.Survey>()
                .Find(x => x.OdooID == id || x.Id == id).FirstOrDefault();

                if(result!=null){
                    return ApiResult<Core.Model.Survey>.Ok(result);
                }

                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Unable to find survey with id {id}");
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while loading survey :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("save")]
        public IActionResult Save([FromBody] Core.Model.Survey param)
        {
            try
            {
                Core.Model.Survey temp = new Core.Model.Survey();

                if (param.ParticipantType == ParticipantType.All)
                {
                    List<Department> departments = new List<Department>();
                    departments = DB.GetCollection<Department>().Find(x => true).ToList();
                    foreach(var emp in departments)
                    {
                        param.Participants.Add(emp.Id);
                    }
                }
                temp = DB.GetCollection<Core.Model.Survey>().Find(s => s.OdooID == param.OdooID).FirstOrDefault();
                if (temp == null)
                {
                    this.DB.Save(param);
                } else
                {
                    param.Id = temp.Id;
                    param.CreatedBy = temp.CreatedBy;
                    param.CreatedDate = temp.CreatedDate;
                    this.DB.Save(param);
                }
                
                return ApiResult<object>.Ok($"Survey has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while saving survey :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("publish/{id}")]
        public IActionResult Publish(string id)
        {
            try
            {
                var result = this.DB.GetCollection<Core.Model.Survey>()
                    .Find(x => x.OdooID == id || x.Id == id).FirstOrDefault();
                
                if(result != null){
                    result.Published = true;
                    DB.Save(result);
                    return ApiResult<Core.Model.Survey>.Ok(result);
                }

                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Unable to find survey with id {id}");
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while saving survey :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("unpublish/{id}")]
        public IActionResult Unpublish(string id)
        {
            try
            {
                var result = this.DB.GetCollection<Core.Model.Survey>()
                    .Find(x => x.OdooID == id || x.Id == id).FirstOrDefault();

                if (result != null)
                {
                    result.Published = false;
                    DB.Save(result);
                    return ApiResult<Core.Model.Survey>.Ok(result);
                }

                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Unable to find survey with id {id}");
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while saving survey :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("summary")]
        public IActionResult Summary([FromBody]SurveyForm param)
        {
            int RecurrentType = 0;
            int DefaultDateNumber = 0;
            int Picker = 0;
            try
            {
                List<BsonDocument> pipe = new List<BsonDocument> {
                    new BsonDocument{{"$match", new BsonDocument{{"_id",param.Id }}}},
                    new BsonDocument {{
                            "$group", new BsonDocument{
                                { "_id", "$_id" },
                                {"Recurrent", new BsonDocument{{"$first", "$Recurrent"}}},
                                {"Schedule", new BsonDocument{{"$first", "$Schedule"}}}
                            }
                        }
                    }
                };

                var GetRecurrent = DB.GetCollection<Core.Model.Survey>().Aggregate<BsonDocument>(pipe).FirstOrDefault();
                if (GetRecurrent != null)
                {
                    if (GetRecurrent.TryGetValue("Recurrent", out BsonValue participantID))
                        RecurrentType = participantID.AsInt32;
                    if (GetRecurrent.TryGetValue("Schedule", out BsonValue schedule))
                    {
                        DateRange rate = BsonSerializer.Deserialize<DateRange>(schedule.ToJson());
                        switch (RecurrentType)
                        {
                            case (int)SurveyRecurrent.Once:
                            case (int)SurveyRecurrent.Daily:
                                DefaultDateNumber = rate.Start.Day;
                                break;
                            case (int)SurveyRecurrent.Weekly:
                                double dayofYear = rate.Start.DayOfYear;
                                int week = Convert.ToInt32(Math.Ceiling(dayofYear / 7));
                                DefaultDateNumber = week;
                                break;
                            case (int)SurveyRecurrent.Monthly:
                                DefaultDateNumber = rate.Start.Month;
                                break;
                            case (int)SurveyRecurrent.Yearly:
                                DefaultDateNumber = rate.Start.Year;
                                break;
                            default:
                                DefaultDateNumber = rate.Start.Day;
                                break;
                        }
                    }
                }

                Picker = (param.Picker == 0) ? DefaultDateNumber : param.Picker;

                var pipelines = new List<BsonDocument> {
                    new BsonDocument{{"$match", new BsonDocument{ { "_id" , param.Id } }}},
                    new BsonDocument{{
                        "$lookup", new BsonDocument{
                            { "from" , "SurveySchedule" },
                            { "localField" , "_id" },
                            { "foreignField" , "SurveyID" },
                            { "as" , "summary" }
                        }}},
                    new BsonDocument{{"$unwind", "$summary"}}
                };

                string[] SummaryKey = { "summary.Day", "summary.Day", "summary.Day", "summary.Week", "summary.Month", "summary.Year" };
                pipelines.Add(new BsonDocument { { "$match", new BsonDocument { { SummaryKey[RecurrentType], Picker } } } });

                pipelines.Add(new BsonDocument{
                    {
                        "$group", new BsonDocument{

                            {"_id", "$_id"},
                            {"Title", new BsonDocument{{"$first", "$Title"}}},
                            {"Recurrent", new BsonDocument{{"$first", "$Recurrent"}}},
                            {"Schedule", new BsonDocument{{"$first", "$Schedule" } }},
                            {"Mandatory", new BsonDocument{{"$first", "$Mandatory"}}},
                            {"ParticipantType", new BsonDocument{{"$first", "$ParticipantType"}}},
                            {"ParticipantTypeDescription", new BsonDocument{{"$first", "ParticipantTypeDescription"}}},
                            {"Department", new BsonDocument{{"$first", "$Department"}}},
                            {"Published", new BsonDocument{{"$first", "$Published"}}},
                            {"TotalParticipants", new BsonDocument{{"$sum", 1}}},
                            {"TotalParticipantsTakenSurvey", new BsonDocument{{
                                "$sum", new BsonDocument
                                {
                                    { "$cond", new BsonArray{new BsonDocument{{"$eq", new BsonArray{ "$summary.Done", true } }}, 1,0 } }
                                }
                             }}}
                        }
                    }
                 });

                var result = DB.GetCollection<Core.Model.Survey>().Aggregate<BsonDocument>(pipelines).FirstOrDefault();
                SurveySummary surveysummary = (!result.Equals(null)) ? BsonSerializer.Deserialize<SurveySummary>(result.ToJson()) : new SurveySummary();
                return ApiResult<SurveySummary>.Ok(surveysummary);
            }
            catch (Exception e)
            {
                return ApiResult<SurveySummary>.Error(HttpStatusCode.BadRequest, $"Error while loading survey :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpPost("participant")]
        public IActionResult Participant([FromBody] SurveyForm param)
        {
            int RecurrentType = 0;
            int DefaultDateNumber = 0;
            int Picker = 0;

            try
            {
                List<BsonDocument> pipe = new List<BsonDocument> {
                    new BsonDocument{{"$match", new BsonDocument{{"_id",param.Id }}}},
                    new BsonDocument {{
                            "$group", new BsonDocument{
                                { "_id", "$_id" },
                                {"Recurrent", new BsonDocument{{"$first", "$Recurrent"}}},
                                {"Schedule", new BsonDocument{{"$first", "$Schedule"}}}
                            }
                        }
                    }
                };

                var GetRecurrent = DB.GetCollection<Core.Model.Survey>().Aggregate<BsonDocument>(pipe).FirstOrDefault();
                if (GetRecurrent != null)
                {
                    if (GetRecurrent.TryGetValue("Recurrent", out BsonValue participantID))
                        RecurrentType = participantID.AsInt32;

                    if (GetRecurrent.TryGetValue("Schedule", out BsonValue schedule))
                    {
                        DateRange rate = BsonSerializer.Deserialize<DateRange>(schedule.ToJson());
                        switch (RecurrentType)
                        {
                            case (int)SurveyRecurrent.Once:
                            case (int)SurveyRecurrent.Daily:
                                DefaultDateNumber = rate.Start.Day;
                                break;
                            case (int)SurveyRecurrent.Weekly:
                                double dayofYear = rate.Start.DayOfYear;
                                int week = Convert.ToInt32(Math.Ceiling(dayofYear / 7));
                                DefaultDateNumber = week;
                                break;
                            case (int)SurveyRecurrent.Monthly:
                                DefaultDateNumber = rate.Start.Month;
                                break;
                            case (int)SurveyRecurrent.Yearly:
                                DefaultDateNumber = rate.Start.Year;
                                break;
                            default:
                                DefaultDateNumber = rate.Start.Day;
                                break;
                        }
                    }
                }

                Picker = (param.Picker == 0) ? DefaultDateNumber : param.Picker;

                List<BsonDocument> pipelines = new List<BsonDocument> {
                    new BsonDocument{{"$match", new BsonDocument{{"_id", param.Id }}}},
                    new BsonDocument{{
                        "$lookup", new BsonDocument{
                            { "from" , "SurveySchedule" },
                            { "localField" , "_id" },
                            { "foreignField" , "SurveyID" },
                            { "as" , "summary" }
                        }}},
                    new BsonDocument{{"$unwind", "$summary"}}
                };

                string[] SummaryKey = { "summary.Day", "summary.Day", "summary.Day", "summary.Week", "summary.Month", "summary.Year" };
                pipelines.Add(new BsonDocument { { "$match", new BsonDocument { { SummaryKey[RecurrentType], Picker } } } });

                pipelines.Add(new BsonDocument{
                    {
                        "$group", new BsonDocument{

                            {"_id", "$_id"},
                            {"Title", new BsonDocument{{"$first", "$Title"}}},
                            {"Recurrent", new BsonDocument{{"$first", "$Recurrent"}}},
                            {"Mandatory", new BsonDocument{{"$first", "$Mandatory"}}},
                            {"ParticipantType", new BsonDocument{{"$first", "$ParticipantType"}}},
                            {"ParticipantTypeDescription", new BsonDocument{{"$first", "ParticipantTypeDescription"}}},
                            {"Department", new BsonDocument{{"$first", "$Department"}}},
                            {"Published", new BsonDocument{{"$first", "$Published"}}},
                            {
                                "Participants", new BsonDocument{
                                    { "$push",  new BsonDocument
                                        {
                                            {"EmployeeID", "$summary.ParticipantID"},
                                            {"EmployeeName", "Example"},
                                            {"Done", "$summary.Done"}
                                        }
                                    }
                                }
                            }
                        }
                    }
                 });

                var result = DB.GetCollection<Core.Model.Survey>().Aggregate<BsonDocument>(pipelines).FirstOrDefault();
                SurveySummary surveyparticipant = (!result.Equals(null)) ? BsonSerializer.Deserialize<SurveySummary>(result.ToJson()) : new SurveySummary();
                return ApiResult<SurveySummary>.Ok(surveyparticipant);

            }
            catch (Exception e)
            {
                return ApiResult<SurveySummary>.Error(HttpStatusCode.BadRequest, $"Error while loading survey :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }

        [HttpPost("employee")]
        public IActionResult GetSurveyEmployee([FromBody] string id)
        {
            try
            {
                var results = this.DB.GetCollection<Core.Model.Survey>()
                .Find(x => x.Participants.Contains(id) && x.Published == true)
                .ToList();
                results = results.FindAll(x => x.Schedule.Start.Date <= DateTime.Now.Date && x.Schedule.Finish.Date >= DateTime.Now.Date);
                return ApiResult<List<Core.Model.Survey>>.Ok(results);
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while loading survey :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("gets")]
        public IActionResult GetSurveyAll()
        {
            try
            {
                var results = this.DB.GetCollection<Core.Model.Survey>()
                .Find(x => true)
                .ToList();
                return ApiResult<List<Core.Model.Survey>>.Ok(results);
            } catch(Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while loading survey :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("get/{Id}")]
        public IActionResult GetSurveyByID(int Id)
        {
            try
            {
                var res = DB.GetCollection<Core.Model.Survey>().Find(x => x.OdooID == Id.ToString()).FirstOrDefault();
                return ApiResult<Core.Model.Survey>.Ok(res);
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while loading employee :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("data/employee")]
        public IActionResult GetEmployee()
        {
            try
            {
                var res = DB.GetCollection<Department>().Find(x => true).ToList();
                return ApiResult<List<Department>>.Ok(res, res.Count());
            } catch(Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while loading employee :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("data/participants/{Id}")]
        public IActionResult GetParticipants(int Id)
        {
            try
            {
                var res = DB.GetCollection<Core.Model.Survey>().Find(x => x.OdooID == Id.ToString()).FirstOrDefault();
                List<string> part = res.Participants;
                return ApiResult<List<string>>.Ok(part, part.Count());
            }
            catch (Exception e)
            {
                return ApiResult<Core.Model.Survey>.Error(HttpStatusCode.BadRequest, $"Error while loading employee :\n{Format.ExceptionString(e)}");
            }
        }
    }
}
