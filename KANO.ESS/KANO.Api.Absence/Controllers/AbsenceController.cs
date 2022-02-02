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

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public AbsenceController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _user = new User(DB, Configuration);
        }

        [HttpPost("docheckinout")]
        public IActionResult CheckInOut([FromBody] AbsenceForm absence)
        {
            User user = _user.GetEmployeeUser(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            AbsenceInOut inout = new AbsenceInOut()
            {
                EmplIdField = user.Id,
                EmplNameField = user.FullName,
                InOutField = absence.InOut,
                Clock = 0,
                PresenceDateField = DateTime.Now,
                RecIdField = 1,
                TermNo = absence.LocationID
            };
            try
            {
                string result = String.Empty;
                if(absence.typeID == "Photo")
                {
                    string newPath = Upload(Configuration, user.Username, absence.LogoContent);
                }
                var adapter = new AbsenceAdapter(Configuration);
                result = adapter.DoAbsenceClockInOut(inout);
                if (result == "Failed")
                {
                    throw new Exception(result);
                }
                DB.Save(new ActivityLog()
                {
                    EntityID = ObjectId.Parse(absence.EntityID),
                    ActivityTypeID = ObjectId.Parse(absence.ActivityTypeID),
                    LocationID = absence.LocationID,
                    UserID = user.Id,
                    Latitude = absence.Latitude,
                    Longitude = absence.Longitude,
                    DateTime = DateTime.UtcNow,
                    CreatedBy = user.Id,
                    UpdateBy = user.Id,
                    SubmittedBy = user.Id,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    Status = "active"
                });

                // Create absence file
                CreateAbsenceFile(absence.InOut, inout);

                return Ok(new
                {
                    data = result,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }
        
        [HttpPost("doinout")]
        public IActionResult DoInOut([FromForm] TicketForm param)
        {
            Absences absence = JsonConvert.DeserializeObject<Absences>(param.JsonData);
            User user = _user.GetEmployeeUser(absence.EmployeeID);
            AbsenceInOut inout = new AbsenceInOut()
            {
                EmplIdField = user.Id,
                EmplNameField = user.FullName,
                InOutField = absence.InOut,
                Clock = 0,
                PresenceDateField = DateTime.Now,
                RecIdField = 1,
                TermNo = absence.LocationID
            };
            try
            {
                string result = String.Empty;
                if (absence.typeID == "Photo")
                {
                    absence.UploadAbsence(Configuration, null, param.FileUpload, x => String.Format("Absence{0}{1}{2}",
                        absence.EntityID, x.EmployeeID, DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)));
                }
                DateTime actionDatetime = Core.Lib.Helper.Configuration.GetStageApp(Configuration) == "development" ? DateTime.UtcNow.AddHours(7) : DateTime.UtcNow;
                DB.Save(new ActivityLog()
                {
                    EntityID = ObjectId.Parse(absence.EntityID),
                    ActivityTypeID = ObjectId.Parse(absence.ActivityTypeID),
                    LocationID = absence.LocationID,
                    UserID = user.Id,
                    Latitude = absence.Latitude,
                    Longitude = absence.Longitude,
                    DateTime = actionDatetime, // DateTime.UtcNow
                    CreatedBy = user.Id,
                    UpdateBy = user.Id,
                    SubmittedBy = user.Id,
                    CreatedDate = actionDatetime,
                    LastUpdatedDate = actionDatetime,
                    Status = "active",
                    Temporary = absence.Temporary
                });
                CreateAbsenceFile(absence.InOut, inout);
                return ApiResult<object>.Ok($"Absence has been saved");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Absence request is failed :\n{e.Message}");
            }
        }

        [HttpPost("doinoutdev")]
        public IActionResult DoInOutDev([FromForm] TicketForm param)
        {
            Absences absence = JsonConvert.DeserializeObject<Absences>(param.JsonData);
            User user = _user.GetEmployeeUser(absence.EmployeeID);
            AbsenceInOut inout = new AbsenceInOut()
            {
                EmplIdField = user.Id,
                EmplNameField = user.FullName,
                InOutField = absence.InOut,
                Clock = 0,
                PresenceDateField = DateTime.Now,
                RecIdField = 1,
                TermNo = absence.LocationID
            };

            try
            {
                string result = String.Empty;
                if (absence.typeID == "Photo")
                {
                    absence.UploadAbsence(Configuration, null, param.FileUpload, x => String.Format("Absence{0}{1}{2}",
                        absence.EntityID, x.EmployeeID, DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)));
                }
                DateTime actionDatetime = Core.Lib.Helper.Configuration.GetStageApp(Configuration) == "development" ? DateTime.UtcNow.AddHours(7) : DateTime.UtcNow;

                if (!absence.Temporary)
                {
                    var adapter = new AbsenceAdapter(Configuration);
                    result = adapter.DoAbsenceClockInOut(inout);
                    if (result == "Failed")
                    {
                        throw new Exception(result);
                    }
                }

                DB.Save(new ActivityLog()
                {
                    EntityID = ObjectId.Parse(absence.EntityID),
                    ActivityTypeID = ObjectId.Parse(absence.ActivityTypeID),
                    LocationID = absence.LocationID,
                    UserID = user.Id,
                    Latitude = absence.Latitude,
                    Longitude = absence.Longitude,
                    DateTime = actionDatetime, // DateTime.UtcNow
                    CreatedBy = user.Id,
                    UpdateBy = user.Id,
                    SubmittedBy = user.Id,
                    CreatedDate = actionDatetime,
                    LastUpdatedDate = actionDatetime,
                    Status = "active",
                    Temporary = absence.Temporary
                });

                if (!absence.Temporary)
                {
                    CreateAbsenceFile(absence.InOut, inout);
                }

                return ApiResult<object>.Ok($"Absence has been saved");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Absence request is failed :\n{e.Message}");
            }
        }

        [HttpGet("updatedoinoutdev")]
        public IActionResult UpdateDoInOutDev()
        {
            User user = _user.GetEmployeeUser(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var activitylogs = DB.GetCollection<ActivityLog>().Find(a => a.Temporary == true && a.UserID == user.Username).ToList();
            try
            {
                foreach (var activitylog in activitylogs)
                {
                    if ((DateTime.Now - activitylog.DateTime).TotalHours < 4.0)
                    {
                        string result = String.Empty;
                        activitylog.Temporary = false;
                        DB.Save(activitylog);
                        var activitytype = DB.GetCollection<ActivityType>().Find(a => a.Id == activitylog.ActivityTypeID).FirstOrDefault();
                        AbsenceInOut inout = new AbsenceInOut()
                        {
                            EmplIdField = user.Id,
                            EmplNameField = user.FullName,
                            InOutField = activitytype.Name == "Checkin" ? "IN" : "OUT",
                            Clock = 0,
                            PresenceDateField = activitylog.DateTime,
                            RecIdField = 1,
                            TermNo = activitylog.LocationID
                        };
                        var adapter = new AbsenceAdapter(Configuration);
                        result = adapter.DoAbsenceClockInOut(inout);
                        CreateAbsenceFile(activitytype.Name == "Checkin" ? "IN" : "OUT", inout);
                    }
                }
                return ApiResult<object>.Ok($"Absence has been updated");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Absence request is failed :\n{e.Message}");
            }
        }

        public string Upload(IConfiguration configuration, string username, string blobstr)
        {
            var uploadDirectory = Core.Lib.Helper.Configuration.UploadPath(configuration);
            // New file path and name preparation
            var newFilename = Tools.SanitizeFileName(String.Format("{0}_{1}{2}", username, DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), Path.GetExtension("img.jpg")));
            newFilename = newFilename.Replace(" ", "_");
            var newFilepath = Path.Combine(uploadDirectory, newFilename);
            // Upload file
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

            if (!System.IO.File.Exists(absenceFilePath))
            {
                using (StreamWriter sw = System.IO.File.CreateText(absenceFilePath))
                {
                    sw.WriteLine($"{inout.EmplIdField}\t{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\t7\t{io}");
                }
            }
            else
            {
                using (StreamWriter sw = System.IO.File.AppendText(absenceFilePath))
                {
                    sw.WriteLine($"{inout.EmplIdField}\t{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}\t7\t{io}");
                }
            }
        }
    }
}