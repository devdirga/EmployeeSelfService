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
    [Collection("ActivityLog")]
    [BsonIgnoreExtraElements]
    public class ActivityLog : BaseT, IMongoPreSave<ActivityLog> , IMongoExtendedPostSave<ActivityLog>, IMongoExtendedPostDelete<ActivityLog>
    {
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        [BsonId]
        public ObjectId Id { get; set; }
        public ObjectId EntityID { get; set; }
        public ObjectId ActivityTypeID { get; set; }
        public string LocationID { get; set; }
        public string UserID { get; set; }
        public string SubmittedBy { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime DateTime { get; set; }
        public double Hours { get; set; }

        public string CreatedBy { get; set; }

        public Nullable<DateTime> CreatedDate { get; set; }

        public Nullable<DateTime> LastUpdatedDate { get; set; }
        public string Status { get; set; }
        public bool Temporary { get; set; } = false;
        public bool IsOldApps { get; set; } = true;
        public ActivityLog() { }

        public ActivityLog(IMongoDatabase mongoDB, IConfiguration configuration)  {
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public void PreSave(IMongoDatabase db)
        {
        }

        public void PostSave(IMongoDatabase db, ActivityLog originalObject)
        {
        }

        public void PostDelete(IMongoDatabase db, ActivityLog originalObject)
        {
        }

        // For old mobile apps
        public List<ActivityLog> Get(ObjectId entityID, int skip, int limit, string userID, ObjectId activityTypeID, ObjectId locationID, DateTime startDate, DateTime endDate, string keyword)
        {
            String code = String.Empty;
            if (locationID != ObjectId.Empty) {
                Location Location = this.MongoDB.GetCollection<Location>().Find(x => x.Id == locationID).FirstOrDefault();
                if (Location != null)
                    code = Location.Code;
            }
            var ActivityLog = new List<ActivityLog>();
            var filter = new BsonDocument();
            if (!string.IsNullOrEmpty(keyword))
                filter = new BsonDocument {
                    { "$and", new BsonArray {
                        new BsonDocument {{ "EntityID", entityID } },
                        new BsonDocument {{ "UserID", userID } },
                        new BsonDocument {{ "ActivityTypeID", activityTypeID } },
                        new BsonDocument {{ "LocationID", code } },
                        new BsonDocument{{ "$or", new BsonArray {
                            new BsonDocument {{ "Status", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } }
                        }}}
                        }
                    }
                };
            else
            {
                if(entityID != ObjectId.Empty)
                    filter.Add("EntityID", entityID);
                if (!string.IsNullOrEmpty(userID))
                    filter.Add("UserID", userID);                
                if (activityTypeID != ObjectId.Empty)
                    filter.Add("ActivityTypeID", activityTypeID);                
                if (locationID != ObjectId.Empty)
                    filter.Add("LocationID", code);                
            }
            ActivityLog = this.MongoDB.GetCollection<ActivityLog>().Find(filter).Skip(skip).Limit(limit).SortByDescending(x => x.DateTime).ToList();
            var result = new List<ActivityLog>();
            foreach (var al in ActivityLog) {
                if (!al.IsOldApps)
                    al.DateTime = al.DateTime.AddHours(-7);
                result.Add(al);
            }
            if (result == null)
                throw new Exception($"ActivityLog for {userID} is not found");
            return result;
        }

        // For new mobile apps
        public List<ActivityLog> MGet(ObjectId entityID, int skip, int limit, string userID, ObjectId activityTypeID, ObjectId locationID, DateTime startDate, DateTime endDate, string keyword)
        {
            String code = String.Empty;
            if (locationID != ObjectId.Empty) {
                Location Location = this.MongoDB.GetCollection<Location>().Find(x => x.Id == locationID).FirstOrDefault();
                if (Location != null)
                    code = Location.Code;
            }
            var ActivityLog = new List<ActivityLog>();
            var filter = new BsonDocument();
            if (!string.IsNullOrEmpty(keyword))
                filter = new BsonDocument {
                    { "$and", new BsonArray {
                        new BsonDocument {{ "EntityID", entityID } },
                        new BsonDocument {{ "UserID", userID } },
                        new BsonDocument {{ "ActivityTypeID", activityTypeID } },
                        new BsonDocument {{ "LocationID", code } },
                        new BsonDocument{{ "$or", new BsonArray {
                            new BsonDocument {{ "Status", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } }
                        }}}
                        }
                    }
                };
            else
            {
                if (entityID != ObjectId.Empty)
                    filter.Add("EntityID", entityID);
                if (!string.IsNullOrEmpty(userID))
                    filter.Add("UserID", userID);
                if (activityTypeID != ObjectId.Empty)
                    filter.Add("ActivityTypeID", activityTypeID);
                if (locationID != ObjectId.Empty)
                    filter.Add("LocationID", code);                
            }
            ActivityLog = this.MongoDB.GetCollection<ActivityLog>().Find(filter).Skip(skip).Limit(limit).SortByDescending(x => x.DateTime).ToList();
            var result = new List<ActivityLog>();
            foreach (var al in ActivityLog) {
                if (al.IsOldApps)
                    al.DateTime = al.DateTime.AddHours(7);
                result.Add(al);
            }
            if (result == null)
                throw new Exception($"ActivityLog for {userID} is not found");
            return result;
        }

        public ActivityLog GetByID(string id)
        {
            var ActivityLog = this.MongoDB.GetCollection<ActivityLog>().Find(x => x.Id == ObjectId.Parse(id)).FirstOrDefault();
            if (ActivityLog == null)
                throw new Exception($"ActivityLog {id} is not found");
            return ActivityLog;
        }
    }

    public class ParamMbuh
    {
        public DateTime temp { get; set; }
        public int Take { set; get; }
        public int Skip { set; get; }
        public int Page { set; get; }
        public int PageSize { set; get; }
    }
}
