using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

using KANO.Core.Lib.Middleware.ServerSideAnalytics.Api;
using KANO.Core.Lib.Middleware.ServerSideAnalytics.Mongo;

namespace KANO.Core.Model
{

    [Collection("Leave")]
    [BsonIgnoreExtraElements]
    public class Leave : BaseDocumentVerification, IMongoPreSave<Leave>
    {
        public DateRange Schedule { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string TypeDescription { get; set; }
        public string AddressDuringLeave { get; set; }
        public string ContactDuringLeave { get; set; }
        public string SubtituteEmployeeID { get; set; }
        public string SubtituteEmployeeName { get; set; }
        public int PendingRequest { get; set; }

        public Leave() : base() { }

        public Leave(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public List<Leave> GetS(string employeeID, DateRange range) {
            var tasks = new List<Task<List<Core.Model.Leave>>>();
            var newFinish = range.Finish.AddHours(23).AddMinutes(59).AddSeconds(59);

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new LeaveAdapter(Configuration);
                return adapter.Get(employeeID).FindAll(x => (x.Schedule.Start >= range.Start && x.Schedule.Start <= newFinish)
                 || (x.Schedule.Finish >= range.Start && x.Schedule.Finish <= newFinish));
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                return this.MongoDB.GetCollection<Core.Model.Leave>()
                .Find(x => x.EmployeeID == employeeID && x.Status==UpdateRequestStatus.InReview && ((x.Schedule.Start >= range.Start && x.Schedule.Start <= newFinish) || (x.Schedule.Finish >= range.Start && x.Schedule.Finish <= newFinish)))
                .ToList();
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
            var results = new List<Core.Model.Leave>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                foreach (var result in t.Result)
                {
                    results.AddRange(result);
                }
            }

            return results;
        }

        public Leave GetByAXRequestID(string employeeID, string axRequestID)
        {
            return this.MongoDB.GetCollection<Leave>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();
        }

        public List<Leave> GetPending(string employeeID)
        {
            return this.MongoDB.GetCollection<Leave>()
                                .Find(x => x.EmployeeID == employeeID && x.Status == UpdateRequestStatus.InReview).ToList();
        }

        public void StatusUpdater(UpdateRequest request, UpdateRequestStatus newStatus)
        {
            if (request.Status != newStatus)
            {
                Leave leave = null;
                var tasks = new List<Task<TaskRequest<object>>>();                
                var employeeID = request.EmployeeID;
                var AXRequestID = request.AXRequestID;
                var updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = false;

                //Logs
                var LogID = ObjectId.GenerateNewId().ToString();
                //Logs

                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Logs
                        ApiWebRequest log1 = new ApiWebRequest()
                        {
                            Identity = LogID,
                            Method = "Start - Update Leave",
                            UserAgent = $"{employeeID} {AXRequestID} {request.Status}",
                            Request = JsonConvert.SerializeObject(request),
                            Timestamp = DateTime.Now
                        };
                        MongoDB.Save(log1);

                        leave = MongoDB.GetCollection<Leave>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).FirstOrDefault();
                        if (leave == null) return TaskRequest<object>.Create("leave", false);

                        // Logs
                        ApiWebRequest log2 = new ApiWebRequest()
                        {
                            Identity = LogID,
                            Method = "Leave Found",
                            UserAgent = $"{employeeID} {AXRequestID} {request.Status}",
                            Request = JsonConvert.SerializeObject(leave),
                            Timestamp = DateTime.Now
                        };
                        MongoDB.Save(log2);

                        var newFilePath = Tools.ArchiveFile(leave.Filepath);
                        var result = MongoDB.GetCollection<Leave>().UpdateOne(
                            x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                            Builders<Leave>.Update
                                .Set(d => d.Status, newStatus)
                                .Set(d => d.Filepath, newFilePath),
                            updateOptions
                        );

                        // Logs
                        ApiWebRequest log3 = new ApiWebRequest()
                        {
                            Identity = LogID,
                            Method = "Finish - Leave Updated",
                            UserAgent = $"{employeeID} {AXRequestID} {request.Status}",
                            Request = JsonConvert.SerializeObject(result),
                            Timestamp = DateTime.Now
                        };
                        MongoDB.Save(log3);
                        return TaskRequest<object>.Create("leave", result.IsAcknowledged);

                    }
                    catch (Exception e)
                    {
                        // Logs
                        ApiWebRequest log4 = new ApiWebRequest()
                        {
                            Identity = LogID,
                            Method = "Leave Error",
                            UserAgent = e.Message,
                            Timestamp = DateTime.Now
                        };
                        MongoDB.Save(log4);
                        throw e;

                    }                  
                }));
                
                // Update Families
                tasks.Add(Task.Run(() =>
                {
                    try
                    {
                        // Logs
                        ApiWebRequest log1 = new ApiWebRequest()
                        {
                            Identity = LogID,
                            Method = "Start - Update UpdateRequest",
                            UserAgent = $"{employeeID} {AXRequestID}",
                            Request = JsonConvert.SerializeObject(request),
                            Timestamp = DateTime.Now
                        };
                        MongoDB.Save(log1);

                        var updateRequest = MongoDB.GetCollection<UpdateRequest>()
                                  .Find(x => x.AXRequestID == AXRequestID && x.EmployeeID == employeeID)
                                  .FirstOrDefault();

                        // Logs
                        ApiWebRequest log2 = new ApiWebRequest()
                        {
                            Identity = LogID,
                            Method = "UpdateRequest Found",
                            UserAgent = $"{employeeID} {AXRequestID}",
                            Request = JsonConvert.SerializeObject(updateRequest),
                            Timestamp = DateTime.Now
                        };
                        MongoDB.Save(log2);


                        updateRequest.Status = newStatus;
                        MongoDB.Save(updateRequest);

                        // Logs
                        ApiWebRequest log3 = new ApiWebRequest()
                        {
                            Identity = LogID,
                            Method = "UpdateRequest Updated",
                            UserAgent = $"{employeeID} {AXRequestID}",
                            Request = JsonConvert.SerializeObject(updateRequest),
                            Timestamp = DateTime.Now
                        };
                        MongoDB.Save(log3);

                        return TaskRequest<object>.Create("updateRequest", true);

                    }
                    catch (Exception e)
                    {
                        // Logs
                        ApiWebRequest log4 = new ApiWebRequest()
                        {
                            Identity = LogID,
                            Method = "UpdateRequest Error",
                            UserAgent = e.Message,
                            Timestamp = DateTime.Now
                        };
                        MongoDB.Save(log4);
                        throw e;

                    }
                    
                }));


                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Unable to update leave data '{employeeID}' :\n{e.Message}");
                    ApiWebRequest log = new ApiWebRequest()
                    {
                        Method = "Exception LeaveBatchJob",
                        UserAgent = $"Unable to update leave data '{employeeID}' :\n{e.Message}"
                    };
                    MongoDB.Save(log);
                }

               


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"Leave request is {newStatus.ToString()}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.LEAVE,
                            NotificationAction.OPEN_LEAVE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();

                    if (newStatus == UpdateRequestStatus.Approved && leave != null && !string.IsNullOrWhiteSpace(leave.SubtituteEmployeeID)) {
                        var strStart = Format.StandarizeDate(leave.Schedule.Start);
                        var strFinish = Format.StandarizeDate(leave.Schedule.Finish);
                        var strLeave = "";

                        if (leave.Schedule.Start.Year > 2000)
                        {
                            if (strStart == strFinish)
                            {
                                strLeave = $" on {strStart}";
                            }
                            else
                            {
                                strLeave = $" on {strStart} - {strFinish}";
                            }
                        }

                        new Notification(Configuration, MongoDB).Create(
                            leave.SubtituteEmployeeID,
                            $"You have been assigned to subtitute \"{leave.EmployeeName}\" during his/her leave {strLeave}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.LEAVE,
                            NotificationAction.NONE,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                    }
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }
    }

    public class LeaveInfo
    {
        public List<LeaveMaintenance> Maintenances { set; get; }
        public int TotalRemainder { set; get; }
        public int TotalPending { set; get; }
    }

    public class LeaveMaintenance {
        public bool Available { get; set; }
        public DateRange AvailabilitySchedule { get; set; }
        public DateTime CFexpiredDate { get; set; }
        public string EmployeeID { get; set; }
        public bool IsClosed { get; set; }
        public int CF { get; set; }
        public string Description { get; set; }
        public int Remainder { get; set; }
        public int Rights { get; set; }
        public int Year { get; set; }
    }

    public class LeaveType
    {
        public string CategoryId { get; set; }
        public string Description { get; set; }
        public System.DateTime EffectiveDateFrom { get; set; }
        public System.DateTime EffectiveDateTo { get; set; }
        public bool IsClosed { get; set; }
        public int TypeId { get; set; }
        public int MaxDayLeave { get; set; }
        public int Remainder { get; set; }
        public KESSLMServices.KESSLeaveConsumeDay ConsumeDay { get; set; }
    }

    public class LeaveSubordinate
    {
        public UpdateRequestStatus Status { get; set; }
        public string Description { get; set; }
        public string EmplId { get; set; }
        public string EmplName { get; set; }        
        public long RecId { get; set; }
        public string ReportToEmplId { get; set; }
        public string ReportToEmplName { get; set; }
        [BsonIgnore]
        public string MonthGroup { get; set; }
        public System.DateTime StartDate { get; set; }
        public System.DateTime EndDate { get; set; }
    }

    public class LeaveHistory
    {
        public UpdateRequestStatus Status { get; set; }
        public string Description { get; set; }
        public string EmplId { get; set; }
        public string EmplName { get; set; }
        public System.DateTime EndDate { get; set; }
        public long RecId { get; set; }
        public System.DateTime StartDate { get; set; }
        public DateRange Schedule { get; set; }
    }

    public class HolidaySchedule
    {
        public string EmployeeID { get; set; }
        public DateTime LoggedDate { get; set; }
        public string AbsenceCode { get; set; }
        public bool IsLeave { get; set; }
        public long RecId { get; set; }
    }

    public class HolidayParam
    {
        public string EmployeeID { get; set; }
        public DateRange Range { get; set; }
    }    

    public class LeaveCalendar { 
        public List<Leave> Leaves { set; get; }
        public List<HolidaySchedule> Holidays { set; get; }
    }

    public class LeaveForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }
}
