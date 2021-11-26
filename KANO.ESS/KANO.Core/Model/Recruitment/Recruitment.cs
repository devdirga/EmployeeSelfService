using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("Recruitment")]
    [BsonIgnoreExtraElements]
    public class Recruitment : BaseDocumentVerification, IMongoPreSave<Recruitment>
    {
        public string RecruitmentID { get; set; }
        public DateTime RequisitionDate { get; set; }
        public RecruitmentType RecruitmentType { get; set; }
        public KESSHRMServices.HRMRecruitingStatus RecruitmentStatus { get; set; } = KESSHRMServices.HRMRecruitingStatus.Planned;
        [BsonIgnore]
        public string RecruitmentStatusDescription
        {
            get
            {
                return this.RecruitmentStatus.ToString();
            }
        }
        public string Department { get; set; }
        public string RecruiterID { get; set; }
        public string RecruiterName { get; set; }
        public string PositionID { get; set; }
        public string PositionDescription { get; set; }
        public string JobID { get; set; }
        public string JobDescription { get; set; }
        public string Description { get; set; }
        public int NumberOfOpenings { get; set; }
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public DateTime Deadline { get; set; }
        public DateTime EstimationStartedDate { get; set; }
        public DateTime RequisitionApprovalDate { get; set; }
        [BsonIgnore]
        public Application Application { get; set; }

        public Recruitment() : base() { }

        public Recruitment(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public void PreSave(IMongoDatabase db)
        {
            base.PreSave(db);            
        }

        public List<Recruitment> GetRequisitions(string employeeID)
        {
            var tasks = new List<Task<TaskRequest<object>>>();
            // Fetch recruitment
            tasks.Add(Task.Run(() =>
            {
                var adapter = new RecruitmentAdapter(Configuration);
                var data = adapter.GetRequests(employeeID);
                return TaskRequest<object>.Create("Recruitment", data);
            }));

            // Fetch positions
            tasks.Add(Task.Run(() =>
            {
                var adapter = new RecruitmentAdapter(Configuration);
                var data = adapter.GetPositions();
                return TaskRequest<object>.Create("Positions", data);
            }));

            // Fetch jobs
            tasks.Add(Task.Run(() =>
            {
                var adapter = new RecruitmentAdapter(Configuration);
                var data = adapter.GetJobs();
                return TaskRequest<object>.Create("Jobs", data);
            }));

            tasks.Add(Task.Run(() =>
            {
                var data = MongoDB.GetCollection<Recruitment>().Find(x => x.RecruiterID == employeeID && x.Status == UpdateRequestStatus.InReview).ToList();
                return TaskRequest<object>.Create("DB", data);
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
            var positionsMap = new Dictionary<string, string>();
            var jobsMap = new Dictionary<string, string>();
            var result = new List<Recruitment>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                {
                    switch (r.Label)
                    {
                        case "DB":
                            result.AddRange((List<Recruitment>)r.Result);
                            break;
                        case "Recruitment":
                            result.AddRange((List<Recruitment>)r.Result);
                            break;
                        case "Positions":
                            foreach (var p in (List<Position>)r.Result)
                            {
                                positionsMap[p.PositionID] = p.Description;
                            }
                            break;
                        case "Jobs":
                            foreach (var j in (List<Job>)r.Result)
                            {
                                jobsMap[j.JobID] = j.Description;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            foreach (var r in result)
            {
                if (r.AXID <= 0) continue;

                var val = "";

                if (!string.IsNullOrWhiteSpace(r.PositionID))
                {
                    if (positionsMap.TryGetValue(r.PositionID, out val))
                    {
                        r.PositionDescription = val;
                    }
                }

                if (!string.IsNullOrWhiteSpace(r.JobID))
                {
                    if (jobsMap.TryGetValue(r.JobID, out val))
                    {
                        r.JobDescription = val;
                    }
                }
            }

            return result;
        }

        public Recruitment GetByAXRequestID(string employeeID, string axRequestID)
        {

            var tasks = new List<Task<TaskRequest<object>>>();
            var adapter = new TravelAdapter(this.Configuration);

            // Fetch data Travel
            tasks.Add(Task.Run(() =>
            {
                var recruitment = this.MongoDB.GetCollection<Recruitment>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();

                if (recruitment == null)
                {
                    return TaskRequest<object>.Create("Recruitment", null);
                }

                return TaskRequest<object>.Create("Recruitment", recruitment);
            }));

            var t = Task.WhenAll(tasks);
            try
            {
                t.Wait();
            }
            catch (Exception)
            {
                throw;
            }

            // Combine result
            if (t.Status == TaskStatus.RanToCompletion)
            {
                Recruitment recruitment = null;

                foreach (var r in t.Result)
                {
                    if (r.Label == "Recruitment" && r.Result != null)
                    {
                        recruitment = (Recruitment)r.Result;
                    }
                }

                return recruitment;
            }

            return null;

        }

        public List<Recruitment> GetOpenings(string employeeID, DateRange range)
        {
            var tasks = new List<Task<TaskRequest<object>>>();
            // Fetch recruitment
            tasks.Add(Task.Run(() =>
            {
                var adapter = new RecruitmentAdapter(Configuration);
                var data = adapter.GetOpenings(employeeID, range);
                return TaskRequest<object>.Create("Recruitment", data);
            }));

            // Fetch positions
            tasks.Add(Task.Run(() =>
            {
                var adapter = new RecruitmentAdapter(Configuration);
                var data = adapter.GetPositions();
                return TaskRequest<object>.Create("Positions", data);
            }));

            // Fetch jobs
            tasks.Add(Task.Run(() =>
            {
                var adapter = new RecruitmentAdapter(Configuration);
                var data = adapter.GetJobs();
                return TaskRequest<object>.Create("Jobs", data);
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
            var positionsMap = new Dictionary<string, string>();
            var jobsMap = new Dictionary<string, string>();
            var result = new List<Recruitment>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var r in t.Result)
                {
                    switch (r.Label)
                    {
                        case "Recruitment":
                            result = (List<Recruitment>)r.Result;
                            break;
                        case "Positions":
                            foreach (var p in (List<Position>)r.Result)
                            {
                                positionsMap[p.PositionID] = p.Description;
                            }
                            break;
                        case "Jobs":
                            foreach (var j in (List<Job>)r.Result)
                            {
                                jobsMap[j.JobID] = j.Description;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            foreach (var r in result)
            {
                var val = "";
                if (positionsMap.TryGetValue(r.PositionID, out val))
                {
                    r.PositionDescription = val;
                }

                if (jobsMap.TryGetValue(r.JobID, out val))
                {
                    r.JobDescription = val;
                }
            }

            return result;
        }

        public void StatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                var tasks = new List<Task<TaskRequest<object>>>();
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    var recruitment = MongoDB.GetCollection<Recruitment>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).FirstOrDefault();
                    if (recruitment == null) return TaskRequest<object>.Create("recruitment", false);

                    var newFilepath = Tools.ArchiveFile(recruitment.Filepath);
                    var result = MongoDB.GetCollection<Recruitment>().UpdateOne(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                        Builders<Recruitment>.Update
                            .Set(d => d.Status, newStatus)
                            .Set(d => d.Filepath, newFilepath),
                        updateOptions
                    );
                    return TaskRequest<object>.Create("recruitment", result.IsAcknowledged);

                }));

                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    var updateRequest = MongoDB.GetCollection<UpdateRequest>()
                              .Find(x => x.AXRequestID == AXRequestID && x.EmployeeID == employeeID)
                              .FirstOrDefault();
                    updateRequest.Status = newStatus;
                    MongoDB.Save(updateRequest);
                    return TaskRequest<object>.Create("updateRequest", true);
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update recruitment request data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"Recruitment request is {newStatus.ToString()}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.RECRUITMENT_REQUEST,
                            NotificationAction.NONE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }
    }

    public class Job{
        public string JobTypeID {get; set;}
        public int MaxPositions {get; set;}
        public string TitleID {get; set;}
        public string JobID {get; set;}
        public string Description {get; set;}
        public string CompensationLevelID {get; set;}
        public string Objectives {get; set;}
        public long AXID {get; set;}
    }

    public class Position{
        public DateTime AvailableForAssignment {get; set;}
        public string CompLocationID {get; set;}
        public string Department {get; set;}
        public string JobID {get; set;}
        public string PositionTypeID {get; set;}
        public string TitleID {get; set;}
        public string PositionID {get; set;}
        public string GradePosition {get; set;}
        public string Description {get; set;}
        public long AXID {get; set;}
    }

    public enum RecruitmentType : int { 
        Job = 1,
        Position = 2,
    }

    public enum RecruitmentStatus : int
    {
        Approval = -1,
        Scheduled = 0,
        Start = 1,        
        Finish = 3,
        Canceled = 4
    }

    public class RecruitmentForm{
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }
}
