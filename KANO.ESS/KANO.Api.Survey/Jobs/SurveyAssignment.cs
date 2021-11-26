using Aspose.Words.Lists;
using KANO.Api.BatchJob.Jobs;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KANO.Api.Survey.Jobs
{
    class SurveyAssignment : BackgroundService, IJobService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private ComplaintAdapter _adapter;

        public SurveyAssignment(IMongoManager mongo, IConfiguration configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = configuration;
            _adapter = new ComplaintAdapter(Configuration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            Console.WriteLine("Running survey assignment service");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    this.Run();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error occured while running service {e}");
                }

                await Task.Delay(10000, stoppingToken);
            }
        }

        public void Run()
        {
            // get published survey for the day
            var today = Tools.ToUTC(DateTime.Now);
            var date = Tools.normalizeFilter(new DateRange { Start = today, Finish = today });
            var surveys = this.DB.GetCollection<Core.Model.Survey>()
                .Find(x =>
                    ((x.Schedule.Start >= date.Start && x.Schedule.Start <= date.Finish)
                    ||
                    //(x.Schedule.Finish >= date.Finish && x.Schedule.Finish <= date.Finish))
                    (date.Start >= x.Schedule.Start && date.Start <= x.Schedule.Finish))
                    &&
                    x.Published
                 )
                .ToList();

            if (surveys.Count == 0) return;

            var options = new ParallelOptions() { MaxDegreeOfParallelism = 10 };
            Parallel.ForEach(surveys, options, survey =>
            {
                var pipelines = new List<BsonDocument>();
                var surveySchedule = new SurveySchedule();
                surveySchedule.SurveyDate = today;
                var surveyRecurrentString = "";

                //Console.WriteLine($"Survey Recurrent: {survey.Recurrent}");
                // get scheduled userIDs
                BsonDocument match = null;
                switch (survey.Recurrent)
                {
                    case SurveyRecurrent.Once:
                        match = new BsonDocument{
                            {"SurveyID", survey.Id},
                        };
                        break;
                    case SurveyRecurrent.Daily:
                        surveyRecurrentString = $" {Format.StandarizeDate(today)}";
                        match = new BsonDocument{
                            {"SurveyID", survey.Id},
                            {"Day", surveySchedule.Day},
                            {"Week", surveySchedule.Week},
                            {"Month", surveySchedule.Month},
                            {"Year", surveySchedule.Year},
                        };
                        //Console.WriteLine($"Daily Proccess {surveyRecurrentString}, {match}");
                        break;
                    case SurveyRecurrent.Weekly:
                        surveyRecurrentString = $" week #{surveySchedule.Week} {surveySchedule.Year}";
                        match = new BsonDocument{
                            {"SurveyID", survey.Id},
                            {"Week", surveySchedule.Week},
                            {"Month", surveySchedule.Month},
                            {"Year", surveySchedule.Year},
                        };
                        break;
                    case SurveyRecurrent.Monthly:
                        surveyRecurrentString = $" {Format.StandarizeMonth(today)} {surveySchedule.Year}";
                        match = new BsonDocument{
                            {"SurveyID", survey.Id},
                            {"Month", surveySchedule.Month},
                            {"Year", surveySchedule.Year},
                        };
                        break;
                    case SurveyRecurrent.Yearly:
                        surveyRecurrentString = $" {surveySchedule.Year}";
                        match = new BsonDocument{
                            {"SurveyID", survey.Id},
                            {"Year", surveySchedule.Year},
                        };
                        break;
                }

                if (match != null)
                {
                    pipelines.Add(new BsonDocument{
                        {
                            "$match", match
                        }
                    });
                }
                //foreach (BsonDocument doc in pipelines)
                //{
                //    Console.WriteLine($"$match: {doc.GetValue("$match")}");
                //}

                pipelines.Add(new BsonDocument{
                    {
                        "$group", new BsonDocument{
                            {
                                "_id","$ParticipantID"
                            }
                        }
                    }
                });
                //foreach (BsonDocument doc in pipelines)
                //{
                //    Console.WriteLine($"$match2: {doc}");
                //}

                var scheduleAggr = DB.GetCollection<dynamic>("SurveySchedule").Aggregate<BsonDocument>(pipelines).ToList();
                var scheduledUser = new List<string>();
                foreach (var r in scheduleAggr)
                {
                    BsonValue participantID = null;
                    if (r.TryGetValue("_id", out participantID))
                    {
                        scheduledUser.Add(participantID.AsString);
                    }
                }
                //Console.WriteLine($"Schedule Count: {scheduleAggr.Count}, participant type: {survey.ParticipantType}");
                //Console.WriteLine($"Schedule User: {scheduledUser.Count}");


                // check if there is un-notified user
                switch (survey.ParticipantType)
                {
                    case ParticipantType.All:
                        var filters = new List<FilterDefinition<User>>();
                        var filterBuilder = Builders<User>.Filter;
                        filters.Add(filterBuilder.Nin("_id", scheduledUser));
                        //filters.Add(filterBuilder.ElemMatch(x => x.Roles, x => x != "CanteenMerchant"));
                        survey.Participants = DB.GetCollection<User>().Find(filterBuilder.And(filters)).Project(x => x.Username).ToList();
                        break;
                    case ParticipantType.Department:
                        // TODO : Get participant from department in AX
                        survey.Participants = survey.Participants.FindAll(x => !scheduledUser.Contains(x));
                        break;
                    case ParticipantType.Selected:
                        survey.Participants = survey.Participants.FindAll(x => !scheduledUser.Contains(x));
                        break;
                    default:
                        break;
                }

                if (survey.Participants.Count > 0)
                {
                    Console.WriteLine($"Sending survey `{survey.Title}`{surveyRecurrentString} to {survey.Participants.Count} participant(s)");
                }

                foreach (var user in survey.Participants)
                {
                    if (string.IsNullOrWhiteSpace(user)) continue;

                    var schedule = new SurveySchedule
                    {
                        CreatedDate = DateTime.Now,
                        OdooID = survey.OdooID,
                        Done = false,
                        ParticipantID = user,
                        Recurrent = survey.Recurrent,
                        Title = survey.Title,
                        SurveyDate = today,
                        SurveyID = survey.Id,
                        Mandatory = survey.Mandatory,
                    };

                    DB.Save(schedule);

                    new Notification(Configuration, DB).Create(
                                user,
                                $"Survey \"{schedule.Title}\"{surveyRecurrentString}",
                                Notification.DEFAULT_SENDER,
                                NotificationModule.SURVEY,
                                NotificationAction.APPROVE,
                                (schedule.Mandatory) ? NotificationType.Warning : NotificationType.Info
                            ).Send();
                }
            });
        }
    }
}
