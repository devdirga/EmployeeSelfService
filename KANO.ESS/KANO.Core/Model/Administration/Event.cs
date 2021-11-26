using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace KANO.Core.Model
{
    [Collection("Event")]
    [BsonIgnoreExtraElements]
    public class Event : BaseT, IMongoPreSave<Event>, IMongoExtendedPostSave<Event>, IMongoExtendedPostDelete<Event>
    {
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        [BsonId]
        //[BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
        public string Organizer { get; set; }
        public ObjectId LocationID { get; set; }
        public string LocationName { get; set; }
        [BsonIgnore]
        [BsonIgnoreIfNull]
        public ObjectId EntityID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Nullable<DateTime> StartTime { get; set; }
        public Nullable<DateTime> EndTime { get; set; }
        public List<Attendee> Attendees { get; set; }

        public string CreatedBy { get; set; }

        public Nullable<DateTime> CreatedDate { get; set; }

        public Nullable<DateTime> LastUpdatedDate { get; set; }
        public string Status { get; set; }

        public Event() { }

        public Event(IMongoDatabase mongoDB, IConfiguration configuration)
        {
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public void PreSave(IMongoDatabase db)
        {
            this.LastUpdate = DateTime.Now;
        }

        public void PostSave(IMongoDatabase db, Event originalObject)
        {
        }

        public void PostDelete(IMongoDatabase db, Event originalObject)
        {
        }

        public List<Event> Get(int skip, int limit, string userID, DateTime startDate, DateTime endDate, string keyword)
        {
            // Find Event on Local Database
            var Event = new List<Event>();
            var filter = new BsonDocument();
            if (!string.IsNullOrEmpty(keyword))
            {
                filter = new BsonDocument {
                    { "$and", new BsonArray {
                        new BsonDocument {{ "Organizer", userID } },
                            new BsonDocument{{ "$or", new BsonArray {
                                new BsonDocument {{ "Name", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                                new BsonDocument {{ "Description", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                                new BsonDocument {{ "Attendees.Email", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                                new BsonDocument {{ "Status", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } }
                            }}}
                        }
                    }
                };
            }
            else
            {
                //filter.Add("Organizer", userID);
            }

            Event = this.MongoDB.GetCollection<Event>().Find(filter).Skip(skip).Limit(limit).ToList();
            var result = new List<Event>();

            foreach (var ev in Event)
            {
                //ev.StartTime >= startDate && ev.EndTime <= endDate
                if (true)
                {
                    result.Add(ev);
                }
            }

            if (result == null)
            {
                throw new Exception($"Event is not found");
            }

            return result;
        }

        public Event GetByID(string EventID)
        {
            // Find Event on Local Database
            var Event = this.MongoDB.GetCollection<Event>()
                .Find(x => x.Id == ObjectId.Parse(EventID))
                .FirstOrDefault();

            if (Event == null)
            {
                throw new Exception($"Event {EventID} is not found");
            }

            return Event;
        }

        public List<Event> Scan(paramScanEvent param)
        {
            // Find Event on Local Database
            var filter = new BsonDocument {
                { "$and", new BsonArray {
                    new BsonDocument {{ "_id", ObjectId.Parse(param.EventID) } },
                    new BsonDocument {{ "Attendees.Email", param.Email } },
                    }
                }
            };

            var Event = this.MongoDB.GetCollection<Event>().Find(filter).ToList();

            if (Event == null)
            {
                throw new Exception($"Event is not found");
            }

            return Event;
        }
    }

    public class Attendee
    {
        public string UserID { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
    }

    public class paramEvent
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "entityID")]
        public string EntityID { get; set; }
        [JsonProperty(PropertyName = "locationID")]
        public string LocationID { get; set; }
        [JsonProperty(PropertyName = "locationName")]
        public string LocationName { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "startTime")]
        public Nullable<DateTime> StartTime { get; set; }
        [JsonProperty(PropertyName = "endTime")]
        public Nullable<DateTime> EndTime { get; set; }
        [JsonProperty(PropertyName = "attendees")]
        public List<paramAttendee> Attendees { get; set; }
        public string Organizer { get; set; }
    }
    public class paramAttendee
    {
        [JsonProperty(PropertyName = "userID")]
        public string UserID { get; set; }

        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }

        [JsonProperty(PropertyName = "fullName")]
        public string FullName { get; set; }
    }

    public class paramScanEvent
    {
        [JsonProperty(PropertyName = "eventID")]
        public string EventID { get; set; }
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
    }

}
