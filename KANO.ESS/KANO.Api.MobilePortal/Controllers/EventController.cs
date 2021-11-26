using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
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

namespace KANO.Api.MobilePortal.Controllers
{
    [Route("api/mobileportal/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        public EventController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
        }

        [HttpPost("get")]
        public IActionResult GetData([FromBody] KendoGrid param)
        {
            var result = new List<Event>();
            var groupMap = new Dictionary<string, string>();
            long total = 0;
            try
            {
                var tasks = new List<Task<TaskRequest<bool>>>();
                var filter = KendoMongoBuilder<Event>.BuildFilter(param);
                var sort = KendoMongoBuilder<Event>.BuildSort(param);

                // Fetch user data
                tasks.Add(Task.Run(() =>
                {
                    result = DB.GetCollection<Event>()
                        .Find(filter)
                        .Limit(param.Take)
                        .Skip(param.Skip)
                        .Sort(sort)
                        .ToList();

                    return TaskRequest<bool>.Create("Event", true);
                }));


                // Fetch total data
                tasks.Add(Task.Run(() => {
                    total = DB.GetCollection<Event>().CountDocuments(filter);
                    return TaskRequest<bool>.Create("Total", true);
                }));

                // Fetch role data
                //tasks.Add(Task.Run(() => {
                //    var groups = DB.GetCollection<Location>().Find(x => true).ToList();

                //    return TaskRequest<bool>.Create("Location", true);
                //}));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }


                /*
                foreach (var dt in result)
                {
                    var roleDescription = "";
                    var role = user.Roles.First();
                    if (groupMap.TryGetValue(role, out roleDescription))
                    {
                        dt.RoleDescription = roleDescription;
                    }
                    else
                    {
                        dt.RoleDescription = role;
                    }
                }
                */
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Event>>.Ok(result, total);

        }

        [HttpGet("get/{employeeID}")]
        public IActionResult GetEventByEmployee(string employeeID)
        {
            List<Event> result = new List<Event>();
            int total = 0;
            try
            {
                var builder = Builders<Event>.Filter;
                //var activeEvent = builder.Lt(u => u.EndTime, DateTime.Now) & builder.Gte(u => u.EndTime, DateTime.Now);
                var activeEvent = builder.Gte(u => u.EndTime, DateTime.Now);
                var attendanceFilter = builder.ElemMatch(u => u.Attendees, acc => acc.UserID == employeeID);
                var allFilter = builder.And(new[] { activeEvent, attendanceFilter });
                result = DB.GetCollection<Event>()
                        .Find(allFilter)
                        .ToList();
            } catch(Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Event>>.Ok(result, total);
        }

        [HttpPost("save")]
        public IActionResult Save([FromBody] paramEvent param)
        {
            try
            {
                string userID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var ev = new Event();
                if (!String.IsNullOrEmpty(param.Id))
                {
                    ev.Id = ObjectId.Parse(param.Id);
                }
                ev.Name = param.Name;
                ev.Description = param.Description;
                ev.Organizer = "";
                ev.LocationID = ObjectId.Parse(param.LocationID);
                ev.LocationName = param.LocationName;
                //ev.EntityID = param.EntityID;
                ev.Organizer = param.Organizer;
                ev.StartTime = param.StartTime;
                ev.EndTime = param.EndTime;
                if (param.Attendees.Count > 0)
                {
                    ev.Attendees = new List<Attendee>();
                    foreach (var att in param.Attendees)
                    {
                        var attendee = new Attendee();
                        //attendee.UserID = ObjectId.Parse(att.UserID);
                        attendee.UserID = (att.UserID);
                        attendee.Email = att.Email;
                        attendee.FullName = att.FullName;
                        ev.Attendees.Add(attendee);
                    }
                }


                ev.CreatedBy = userID;
                ev.CreatedDate = DateTime.UtcNow;
                ev.LastUpdatedDate = DateTime.UtcNow;
                ev.Status = "active";

                DB.Save(ev);
                return ApiResult<object>.Ok(param, "Event has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving user :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("delete/{id}")]
        public IActionResult Delete(string id)
        {
            try
            {
                ObjectId oId = new ObjectId(id);
                DB.GetCollection<Event>().FindOneAndDelete(x => x.Id == oId);
                return ApiResult<object>.Ok("Event has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving user :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("attendances")]
        public IActionResult GetAttendance([FromBody] KendoFilter param)
        {
            List<UserEvent> userEvents = new List<UserEvent>();
            int total = 0;
            try
            {
                var result = new List<User>();
                if (String.IsNullOrEmpty(param.Value))
                {
                    result = DB.GetCollection<User>().Find(f => true).Limit(10).ToList();
                }
                else
                {
                    //var filter = Builders<User>.Filter.Where(x => x.FullName.Contains(param.Value));
                    //var filter = Builders<User>.Filter.Eq(x => x.FullName, param.Value);
                    //var filter = Builders<User>.Filter.Regex(x=>x.FullName, new BsonRegularExpression("/^" + param.Value + "/^"));
                    var filter = Builders<User>.Filter.Regex(x => x.FullName, new BsonRegularExpression(param.Value, "i"));
                    result = DB.GetCollection<User>().Find(filter).Limit(10).ToList();
                }

                foreach (var fafa in result)
                {
                    userEvents.Add(new UserEvent
                    {
                        UserID = fafa.Id,
                        FullName = fafa.FullName,
                        Email = fafa.Email
                    });
                }
                return ApiResult<List<UserEvent>>.Ok(userEvents, total);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving user :\n{Format.ExceptionString(e)}");
            }
        }

        public class KendoFilter
        {
            public string Field { get; set; }
            public string Operator { get; set; }
            public string Value { get; set; }
        }
    }
}