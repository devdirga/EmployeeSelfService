using System;
using System.Linq;
using KANO.Core.Model;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using System.Collections.Generic;
using MongoDB.Bson.Serialization;

namespace KANO.Api.Absence.Controllers
{
    [Route("api/absence/[controller]")]
    [ApiController]
    public class EventController : ControllerBase
    {
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase DB;
        private readonly IConfiguration Configuration;
        private readonly Event _event;
        private readonly User _user;
        private readonly Location _location;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public EventController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _event = new Event(DB, Configuration);
            _user = new User(DB, Configuration);
            _location = new Location(DB, Configuration);
        }

        [Authorize]
        [HttpGet("list")]
        public IActionResult Get()
        {
            try
            {
                int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
                int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
                string userID = Request.Query["userID"];
                DateTime startDate = String.IsNullOrEmpty(Request.Query["startDate"]) ? DateTime.Now : Convert.ToDateTime(Request.Query["startDate"].ToString());
                DateTime endDate = String.IsNullOrEmpty(Request.Query["endDate"]) ? DateTime.Now : Convert.ToDateTime(Request.Query["endDate"].ToString());
                string keyword = Request.Query["search"].ToString();

                var result = _event.Get(skip, limit, userID, startDate, endDate, keyword);

                List<EventResult> eventResult = new List<EventResult>();

                foreach(Event evnt in result)
                {
                    eventResult.Add(MapFromEvent(evnt));
                }

                return Ok(new
                {
                    data = eventResult,
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

        [Authorize]
        [HttpGet("{EventID}")]
        public IActionResult GetByID(string EventID)
        {
            try
            {
                var result = _event.GetByID(EventID);
                EventResult eventResult = MapFromEvent(result);
                return Ok(new
                {
                    data = eventResult,
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

        [Authorize]
        [HttpPost()]
        public IActionResult Save([FromBody] paramEvent param)
        {
            try
            {
                string userID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var evennt = new Event();
                //evennt.Organizer = ObjectId.Parse(param.EntityID);
                evennt.Organizer = userID;
                evennt.LocationID = ObjectId.Parse(param.LocationID);
                evennt.EntityID = ObjectId.Parse(param.EntityID);
                evennt.Name = param.Name;
                evennt.Description = param.Description;
                evennt.StartTime = param.StartTime;
                evennt.EndTime = param.EndTime;
                if (param.Attendees.Count > 0)
                {
                    evennt.Attendees = new List<Attendee>();
                    foreach (var att in param.Attendees)
                    {
                        var attendee = new Attendee();
                        //attendee.UserID = ObjectId.Parse(att.UserID);
                        attendee.UserID = (att.UserID);
                        attendee.Email = att.Email;
                        evennt.Attendees.Add(attendee);
                    }
                }

                //evennt.CreatedBy = ObjectId.Parse(param.EntityID);
                evennt.CreatedBy = userID;
                evennt.CreatedDate = DateTime.UtcNow;
                evennt.LastUpdatedDate = DateTime.UtcNow;
                evennt.Status = "active";
                DB.Save(evennt);

                return Ok(new
                {
                    data = param,
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

        [Authorize]
        [HttpPost("scan")]
        public IActionResult Scan([FromBody] paramScanEvent param)
        {
            try
            {
                var events = _event.Scan(param);
                var eventResult = new List<EventResult>();

                foreach (var evennt in events)
                {
                    var ev = MapFromEvent(evennt);
                    eventResult.Add(ev);
                }
                    
                return Ok(new
                {
                    data = eventResult,
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

        [Authorize]
        [HttpPut("{eventID}")]
        public IActionResult Update([FromBody] paramEvent param)
        {
            try
            {
                string userID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var evennt = new Event();
                evennt.Id = ObjectId.Parse(param.Id);
                //evennt.Organizer = ObjectId.Parse(param.EntityID);
                evennt.Organizer = userID;
                evennt.LocationID = ObjectId.Parse(param.LocationID);
                evennt.EntityID = ObjectId.Parse(param.EntityID);
                evennt.Name = param.Name;
                evennt.Description = param.Description;
                evennt.StartTime = param.StartTime;
                evennt.EndTime = param.EndTime;
                if (param.Attendees.Count > 0)
                {
                    evennt.Attendees = new List<Attendee>();
                    foreach (var att in param.Attendees)
                    {
                        var attendee = new Attendee();
                        //attendee.UserID = ObjectId.Parse(att.UserID);
                        attendee.UserID = (att.UserID);
                        attendee.Email = att.Email;
                        evennt.Attendees.Add(attendee);
                    }
                }

                //evennt.CreatedBy = ObjectId.Parse(param.EntityID);
                evennt.CreatedBy = userID;
                evennt.CreatedDate = DateTime.UtcNow;
                evennt.LastUpdatedDate = DateTime.UtcNow;
                evennt.Status = "active";
                DB.Save(evennt);

                return Ok(new
                {
                    data = param,
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

        public EventResult MapFromEvent(Event ev)
        {
            List<AttendeeResult> ar = new List<AttendeeResult>();
            
            foreach(Attendee at in ev.Attendees){
                ar.Add(new AttendeeResult { 
                    Email = at.Email,
                    UserID = at.UserID                
                });
            }

            Location location = _location.GetByID(ev.LocationID.ToString());
            

            return new EventResult
            {
                Id = ev.Id.ToString(),
                Organizer = ev.Organizer.ToString(),
                LocationID = ev.LocationID.ToString(),
                EntityID = ev.EntityID.ToString(),
                Name = ev.Name,
                Description = ev.Description,
                StartTime = ev.StartTime,
                EndTime = ev.EndTime,
                CreatedBy = ev.CreatedBy,
                CreatedDate = ev.CreatedDate,
                LastUpdatedDate = ev.LastUpdatedDate,
                Status = ev.Status,
                Attendees = ar,
                Location = new LocationMap
                {
                    LocationID = location.Id.ToString(),
                    LocationName = location.Name
                },
                OrganizerDetail = new OrganizerDetailMap { }
            };
        }

    }
}