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
    [Collection("ActivityType")]
    [BsonIgnoreExtraElements]
    public class ActivityType : BaseT, IMongoPreSave<ActivityType> , IMongoExtendedPostSave<ActivityType>, IMongoExtendedPostDelete<ActivityType>
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
        public string UniqueKey { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public List<ObjectId> PreActivities { get; set; }
        public List<ObjectId> PostActivities { get; set; }
        public string Icon { get; set; }

        public string CreatedBy { get; set; }

        public Nullable<DateTime> CreatedDate { get; set; }

        public Nullable<DateTime> LastUpdatedDate { get; set; }
        public string Status { get; set; }

        public ActivityType() { }

        public ActivityType(IMongoDatabase mongoDB, IConfiguration configuration)  {
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public void PreSave(IMongoDatabase db)
        {
        }

        public void PostSave(IMongoDatabase db, ActivityType originalObject)
        {
        }

        public void PostDelete(IMongoDatabase db, ActivityType originalObject)
        {
        }

        public List<ActivityType> Get(string entityID, int skip, int limit, string keyword)
        {
            // Find ActivityType on Local Database
            var ActivityType = new List<ActivityType>();
            var filter = new BsonDocument();
            if (!string.IsNullOrEmpty(keyword))
            {
                filter = new BsonDocument {
                    { "$and", new BsonArray {
                        new BsonDocument {{ "EntityID", ObjectId.Parse(entityID) } },
                        new BsonDocument{{ "$or", new BsonArray {
                            new BsonDocument {{ "UniqueKey", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Name", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Description", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Category", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Status", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } }
                        }}}
                        }   
                    }
                };
            }
            else
            {
                filter.Add("EntityID", ObjectId.Parse(entityID));
            }

            ActivityType = this.MongoDB.GetCollection<ActivityType>().Find(filter).Skip(skip).Limit(limit).SortBy(x => x.Name).ToList();

            if (ActivityType == null)
            {
                throw new Exception($"ActivityType is not found");
            }

            return ActivityType;
        }

        public ActivityType GetByID(string ActivityTypeID) {
            // Find ActivityType on Local Database
            var ActivityType = this.MongoDB.GetCollection<ActivityType>()
                .Find(x => x.Id == ObjectId.Parse(ActivityTypeID))
                .FirstOrDefault();

            if (ActivityType == null)
            {
                throw new Exception($"ActivityType {ActivityTypeID} is not found");
            }

            return ActivityType;
        }
    }
}
