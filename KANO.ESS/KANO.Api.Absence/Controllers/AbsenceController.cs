using System;
using System.IO;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Claims;
using System.Globalization;
using Newtonsoft.Json;
using KANO.Core.Lib;
using System.Net;
using System.Collections.Generic;

namespace KANO.Api.Absence.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AbsenceController : ControllerBase
    {
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase DB;
        private readonly IConfiguration Configuration;
        private readonly User _user;
        private readonly ActivityType _activityType;
        private readonly Location _location;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public AbsenceController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _user = new User(DB, Configuration);
            _activityType = new ActivityType(DB, Configuration);
            _location = new Location(DB, Configuration);
        }

        [HttpPost("docheckinout")]
        public IActionResult CheckInOut([FromBody] AbsenceForm absence)
        {
            //return ApiResult<object>.Error(HttpStatusCode.BadRequest, "Please use up-to-date mobile apps");            
            User user = _user.GetEmployeeUser(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            DateTime absenceClock = Core.Lib.Helper.Configuration.GetStageApp(Configuration).Equals("dev") ? DateTime.UtcNow.AddHours(-7) : DateTime.Now;
            AbsenceInOut inout = new AbsenceInOut() {
                EmplIdField = user.Id,
                EmplNameField = user.FullName,
                InOutField = absence.InOut,
                Clock = 0,
                PresenceDateField = absenceClock,
                RecIdField = 1,
                TermNo = absence.LocationID
            };
            try {
                string result = String.Empty;
                if (absence.typeID.Equals("Photo")) {
                    string newPath = Upload(Configuration, user.Username, absence.LogoContent);
                }
                result = new AbsenceAdapter(Configuration).DoAbsenceClockInOut(inout);
                if (result.Equals("Failed"))
                    throw new Exception(result);
                DB.Save(new ActivityLog() {
                    EntityID = ObjectId.Parse(absence.EntityID),
                    ActivityTypeID = ObjectId.Parse(absence.ActivityTypeID),
                    LocationID = absence.LocationID,
                    UserID = user.Id,
                    Latitude = absence.Latitude,
                    Longitude = absence.Longitude,
                    DateTime = absenceClock,
                    CreatedBy = user.Id,
                    UpdateBy = user.Id,
                    SubmittedBy = user.Id,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    Status = "active",
                    IsOldApps = true
                });
                CreateAbsenceFile(absence.InOut, inout);
                return Ok(new { data = result, message = String.Empty, success = true });
            }
            catch (Exception e) { return Ok(new { data = (object)null, message = Format.ExceptionString(e), success = false }); }
        }
        
        [HttpPost("doinoutdev")]
        public IActionResult DoInOutDev([FromForm] TicketForm p)
        {
            Absences abs = JsonConvert.DeserializeObject<Absences>(p.JsonData);
            User user = _user.GetEmployeeUser(abs.EmployeeID);
            DateTime absenceClock = Core.Lib.Helper.Configuration.GetStageApp(Configuration).Equals("dev") ? DateTime.UtcNow.AddHours(-7) : DateTime.UtcNow.AddHours(7);
            AbsenceInOut inout = new AbsenceInOut() {
                EmplIdField = user.Id, EmplNameField = user.FullName,
                InOutField = abs.InOut, Clock = 0,
                PresenceDateField = absenceClock,
                RecIdField = 1, TermNo = abs.LocationID
            };
            try {
                string result = String.Empty;
                if (abs.typeID.Equals("Photo")) {
                    abs.UploadAbsence(Configuration, null, p.FileUpload, x => String.Format("absence{0}{1}{2}", abs.EntityID, x.EmployeeID, DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)));
                }
                if (!abs.Temporary) {
                    result = new AbsenceAdapter(Configuration).DoAbsenceClockInOut(inout);
                    if (result.Equals("Failed")) {
                        throw new Exception(result);
                    }
                }
                DB.Save(new ActivityLog() {
                    EntityID = ObjectId.Parse(abs.EntityID), 
                    ActivityTypeID = ObjectId.Parse(abs.ActivityTypeID),
                    LocationID = abs.LocationID, UserID = user.Id, Latitude = abs.Latitude, Longitude = abs.Longitude,
                    DateTime = absenceClock,  CreatedBy = user.Id, UpdateBy = user.Id, SubmittedBy = user.Id,
                    CreatedDate = absenceClock, LastUpdatedDate = absenceClock, Status = "active", Temporary = abs.Temporary, IsOldApps = false
                });
                if (!abs.Temporary) {
                    CreateAbsenceFile(abs.InOut, inout);
                }
                return ApiResult<object>.Ok("success");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpGet("updatedoinoutdev")]
        public IActionResult UpdateDoInOutDev()
        {
            User user = _user.GetEmployeeUser(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var activitylogs = DB.GetCollection<ActivityLog>().Find(a => a.Temporary == true && a.UserID == user.Username).ToList();
            try {
                Double absenceTimeLimit = Core.Lib.Helper.Configuration.GetUpdateAbsenceTimeLimit(Configuration);
                Console.WriteLine($"AbsenceTimeLimit = {absenceTimeLimit}");
                foreach (var activitylog in activitylogs)
                    if ((DateTime.Now - activitylog.DateTime).TotalHours < absenceTimeLimit) {
                        activitylog.Temporary = false;
                        DB.Save(activitylog);
                        var activitytype = DB.GetCollection<ActivityType>().Find(a => a.Id == activitylog.ActivityTypeID).FirstOrDefault();
                        AbsenceInOut inout = new AbsenceInOut() {
                            EmplIdField = user.Id, EmplNameField = user.FullName,
                            InOutField = activitytype.Name.Equals("Checkin") ? "IN" : "OUT",
                            Clock = 0, PresenceDateField = activitylog.DateTime,
                            RecIdField = 1, TermNo = activitylog.LocationID
                        };
                        string result = new AbsenceAdapter(Configuration).DoAbsenceClockInOut(inout);
                        CreateAbsenceFileNew(activitytype.Name.Equals("Checkin") ? "IN" : "OUT", inout);
                    }
                return ApiResult<object>.Ok("success");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        public string Upload(IConfiguration configuration, string username, string blobstr)
        {
            var uploadDirectory = Core.Lib.Helper.Configuration.UploadPath(configuration);
            var newFilename = Tools.SanitizeFileName(String.Format("{0}_{1}{2}", username, DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), Path.GetExtension("img.jpg")));
            newFilename = newFilename.Replace(" ", "_");
            var newFilepath = Path.Combine(uploadDirectory, newFilename);
            string base64string = blobstr;
            byte[] blob = Convert.FromBase64String(base64string);
            System.IO.File.WriteAllBytes(newFilepath, blob);
            return newFilepath;
        }

        public void CreateAbsenceFile(String io, AbsenceInOut inout)
        {
            int intTimeNow = Int32.Parse(DateTime.Now.ToString("ss"));
            String per10Minutes = ((intTimeNow / 10) * 10).ToString();
            per10Minutes = (per10Minutes.Length == 1) ? $"0{per10Minutes}" : per10Minutes;
            String serverAdrress = Core.Lib.Helper.Configuration.GetIPAddressServer(Configuration);
            String absenceFile = $"{DateTime.Now.ToString("yyyyddMM", CultureInfo.InvariantCulture)}_{DateTime.Now.ToString("HHmm", CultureInfo.InvariantCulture)}{per10Minutes}_{inout.TermNo}_{serverAdrress}.txt";
            String absenceFilePath = Path.Combine(Core.Lib.Helper.Configuration.GetAbsenceFilePath(Configuration), absenceFile);
            if (!System.IO.File.Exists(absenceFilePath)) {
                using (StreamWriter sw = System.IO.File.CreateText(absenceFilePath)) {
                    sw.WriteLine($"{inout.EmplIdField}\t{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\t7\t{io}");
                }
            }
            else
            {
                using (StreamWriter sw = System.IO.File.AppendText(absenceFilePath)) {
                    sw.WriteLine($"{inout.EmplIdField}\t{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\t7\t{io}");
                }
            }
        }

        public void CreateAbsenceFileNew(String io, AbsenceInOut inout)
        {
            int intTimeNow = Int32.Parse(DateTime.Now.ToString("ss"));
            String per10Minutes = ((intTimeNow / 10) * 10).ToString();
            per10Minutes = (per10Minutes.Length == 1) ? $"0{per10Minutes}" : per10Minutes;
            String serverAdrress = Core.Lib.Helper.Configuration.GetIPAddressServer(Configuration);
            String absenceFile = $"{DateTime.Now.ToString("yyyyddMM", CultureInfo.InvariantCulture)}_{DateTime.Now.ToString("HHmm", CultureInfo.InvariantCulture)}{per10Minutes}_{inout.TermNo}_{serverAdrress}.txt";
            String absenceFilePath = Path.Combine(Core.Lib.Helper.Configuration.GetAbsenceFilePath(Configuration), absenceFile);
            if (!System.IO.File.Exists(absenceFilePath)){
                using (StreamWriter sw = System.IO.File.CreateText(absenceFilePath)){
                    //sw.WriteLine($"{inout.EmplIdField}\t{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\t7\t{io}");
                    sw.WriteLine($"{inout.EmplIdField}\t{inout.PresenceDateField.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\t7\t{io}");

                }
            } else {
                using (StreamWriter sw = System.IO.File.AppendText(absenceFilePath)) {
                    //sw.WriteLine($"{inout.EmplIdField}\t{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\t7\t{io}");
                    sw.WriteLine($"{inout.EmplIdField}\t{inout.PresenceDateField.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\t7\t{io}");
                }
            }
        }

        [HttpGet("getabsencetemporary")]
        public IActionResult GetAbsenceTemporary()
        {
            try {
                User u = _user.GetEmployeeUser(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                var acts = DB.GetCollection<ActivityLog>().Find(a => a.Temporary == true && a.UserID == u.Username).ToList();
                double lmt = Core.Lib.Helper.Configuration.GetUpdateAbsenceTimeLimit(Configuration);
                List<ActivityLogMap> res = new List<ActivityLogMap>();
                foreach (var ac in acts)
                    if ((DateTime.Now - ac.DateTime).TotalHours < lmt)
                        res.Add(MapFromLog(ac));
                return Ok(new{data = res,message = String.Empty,success = true});
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }            
        }

        /*
        [HttpPost("doinout")]
        public IActionResult DoInOut([FromForm] TicketForm p)
        {
            Absences abs = JsonConvert.DeserializeObject<Absences>(p.JsonData);
            User user = _user.GetEmployeeUser(abs.EmployeeID);
            DateTime absenceClock = Core.Lib.Helper.Configuration.GetStageApp(Configuration).Equals("development") ? DateTime.UtcNow.AddHours(7) : DateTime.Now;
            AbsenceInOut inout = new AbsenceInOut() {
                EmplIdField = user.Id, EmplNameField = user.FullName,
                InOutField = abs.InOut, Clock = 0,
                PresenceDateField = absenceClock,
                RecIdField = 1, TermNo = abs.LocationID
            };
            try {
                string result = String.Empty;
                if (abs.typeID.Equals("Photo")) {
                    abs.UploadAbsence(Configuration, null, p.FileUpload, x => String.Format("absence{0}{1}{2}", abs.EntityID, x.EmployeeID, DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)));
                }
                DB.Save(new ActivityLog() {
                    EntityID = ObjectId.Parse(abs.EntityID), ActivityTypeID = ObjectId.Parse(abs.ActivityTypeID),
                    LocationID = abs.LocationID, UserID = user.Id, Latitude = abs.Latitude, Longitude = abs.Longitude,
                    DateTime = absenceClock, CreatedBy = user.Id, UpdateBy = user.Id, SubmittedBy = user.Id,
                    CreatedDate = absenceClock, LastUpdatedDate = absenceClock, Status = "active", Temporary = abs.Temporary
                });
                CreateAbsenceFile(abs.InOut, inout);
                return ApiResult<object>.Ok("success");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }
        */

        public ActivityLogMap MapFromLog(ActivityLog activity)
        {
            User u = _user.GetEmployeeUser(activity.UserID);
            Location l = _location.GetByCode(activity.LocationID);
            ActivityType actype = _activityType.GetByID(activity.ActivityTypeID.ToString());
            return new ActivityLogMap {
                Id = activity.Id.ToString(),
                EntityID = activity.EntityID.ToString(),
                ActivityType = new ActivityTypeMap { ActivityTypeId = actype.Id.ToString(), ActivityTypeName = actype.Name },
                Location = new LocationMap { LocationID = l.Id.ToString(), LocationName = l.Name },
                User = new UserMap { Email = u.Email, FirstName = u.FullName, LastName = u.FullName, UserId = u.Username, Username = u.Username },
                CreatedBy = activity.CreatedBy.ToString(),
                CreatedDate = (DateTime)activity.CreatedDate,
                DateTime = (DateTime)activity.DateTime,
                Latitude = activity.Latitude,
                Longitude = activity.Longitude
            };
        }

    }
}