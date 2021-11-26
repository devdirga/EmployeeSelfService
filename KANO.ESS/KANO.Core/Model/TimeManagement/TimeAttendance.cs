using KANO.Core.Lib.Extension;
using KANO.Core.Service.AX;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using System.Reflection;

namespace KANO.Core.Model
{
    [Collection("TimeAttendance")]
    [BsonIgnoreExtraElements]
    public class TimeAttendance : BaseDocumentVerification, IMongoPreSave<TimeAttendance>
    {
        public TimeAttendance Old { get; set; }
        public string AbsenceCode { get; set; }
        public string ReportToEmployeeID { get; set; }
        public string Name { get; set; }
        public int Days { get; set; }
        public DateTime LoggedDate { get; set; }
        public DateRange ScheduledDate { get; set; }
        public DateRange ActualLogedDate { get; set; }
        public bool Absent { get; set; }
        public string AbsenceCodeDescription { get; set; }
        public bool IsLeave { get; set; }
        public TimeAttendance() : base() { }

        public TimeAttendance(IMongoDatabase mongoDB, IConfiguration configuration) : base(mongoDB, configuration) { }

        public List<TimeAttendanceResult> GetS(string employeeID, DateRange range)
        {
            var tasks = new List<Task<TaskRequest<List<TimeAttendance>>>>();

            // Fetch data from AX
            tasks.Add(Task.Run(() =>
            {
                var adapter = new TimeManagementAdapter(this.Configuration);
                var timeAttendances = adapter.Get(employeeID, range);
                return TaskRequest<List<TimeAttendance>>.Create("AX", timeAttendances);
            }));

            // Fetch data from DB
            tasks.Add(Task.Run(() => {
                var timeAttendances = this.MongoDB.GetCollection<TimeAttendance>()
                                .Find(x => x.EmployeeID == employeeID && (x.Status == UpdateRequestStatus.InReview) && (x.LoggedDate >= range.Start && x.LoggedDate <= range.Finish))
                                .ToList();
                return TaskRequest<List<TimeAttendance>>.Create("DB", timeAttendances);
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
            var result = new List<TimeAttendanceResult>();
            if (t.Status == TaskStatus.RanToCompletion)
            {
                var TimeAttendances = new List<TimeAttendance>();
                var TimeAttendancesUpdateRequest = new List<TimeAttendance>();
                foreach (var r in t.Result)
                    if (r.Label == "AX")
                        TimeAttendances = r.Result;
                    else
                        TimeAttendancesUpdateRequest = r.Result;

                // Merge families data
                foreach (var f in TimeAttendances)
                {
                    result.Add(new TimeAttendanceResult
                    {
                        TimeAttendance = f,
                    });
                }

                foreach (var fur in TimeAttendancesUpdateRequest)
                {
                    if (fur.AXID > 0)
                    {
                        var f = result.Find(x => x.TimeAttendance.AXID == fur.AXID);
                        if (f != null)
                            f.UpdateRequest = fur;
                    }
                    else
                    {
                        result.Add(new TimeAttendanceResult
                        {
                            UpdateRequest = fur,
                        });
                    }
                }

                
            }

            return result;

        }

        public TimeAttendance GetByAXRequestID(string employeeID, string axRequestID)
        {
            return this.MongoDB.GetCollection<TimeAttendance>()
                                .Find(x => x.EmployeeID == employeeID && x.AXRequestID == axRequestID)
                                .FirstOrDefault();
        }

        public TimeAttendance GetByID(string ID)
        {
            return this.MongoDB.GetCollection<TimeAttendance>()
                                .Find(x => x.Id == ID)
                                .FirstOrDefault();
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
                    var timeAttendance = MongoDB.GetCollection<TimeAttendance>().Find(x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status).FirstOrDefault();
                    if (timeAttendance == null) return TaskRequest<object>.Create("timeAttendance", false);

                    var newFilepath = Tools.ArchiveFile(timeAttendance.Filepath);
                    var result = MongoDB.GetCollection<TimeAttendance>().UpdateOne(
                        x => x.EmployeeID == employeeID && x.AXRequestID == AXRequestID && x.Status == request.Status,
                        Builders<TimeAttendance>.Update
                            .Set(d => d.Status, newStatus)
                            .Set(d => d.Filepath, newFilepath),
                        updateOptions
                    );
                    return TaskRequest<object>.Create("timeAttendance", result.IsAcknowledged);

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
                    Console.WriteLine($"Unable to update time attendance data '{employeeID}' :\n{e.Message}");
                }


                if ((int)newStatus >= 0)
                {
                    new Notification(Configuration, MongoDB).Create(
                            request.EmployeeID,
                            $"Absence recomendation request is {newStatus.ToString()}",
                            Notification.DEFAULT_SENDER,
                            NotificationModule.TIME_MANAGEMENT,
                            NotificationAction.OPEN_TIME_MANAGEMENT,
                            Notification.MapUpdateRequestStatus(newStatus) // New Status
                        ).Send();
                }

                request.Status = newStatus;
                MongoDB.Save(request);
            }
        }

    }

    public class TimeAttendanceResult
    {
        public TimeAttendance TimeAttendance { get; set; } = null;
        public TimeAttendance UpdateRequest { get; set; } = null;
    }

    public class TimeAttendanceForm
    {
        public string JsonData { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
    }

    public class GridDateRange
    {
        public string Username { get; set; }
        public DateRange Range { get; set; }
        public int Status { get; set; }
        public string CanteenUserID { get; set; }
        public int Take { get; set; }
        public int Skip { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
    
}
