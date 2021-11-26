using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KANO.Core.Model
{
    [Collection("Entity")]
    [BsonIgnoreExtraElements]
    public class Entity : BaseT, IMongoPreSave<Entity> , IMongoExtendedPostSave<Entity>, IMongoExtendedPostDelete<Entity>
    {
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        [BsonId]
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Logo { get; set; }

        public string CreatedBy { get; set; }

        public Nullable<DateTime> CreatedDate { get; set; }

        public Nullable<DateTime> LastUpdatedDate { get; set; }
        public string Status { get; set; }

        public List<string> Groups { get; set; }
        public User Owner { get; set; }
        public SubscriptionMap Subscription { get; set; }

        public Entity() { }

        public Entity(IMongoDatabase mongoDB, IConfiguration configuration)  {
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public void PreSave(IMongoDatabase db)
        {
        }

        public void PostSave(IMongoDatabase db, Entity originalObject)
        {
        }

        public void PostDelete(IMongoDatabase db, Entity originalObject)
        {
        }

        public List<EntityMap> Get(int skip, int limit, string keyword)
        {
            // Find Entity on Local Database
            var Entity = new List<Entity>();
            var filter = new BsonDocument();
            if (!string.IsNullOrEmpty(keyword))
            {
                filter = new BsonDocument {
                    { "$or", new BsonArray {
                       new BsonDocument {{ "Name", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                       new BsonDocument {{ "Description", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                       new BsonDocument {{ "Status", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } }
                    }}
                };
            }

            Entity = this.MongoDB.GetCollection<Entity>()
                .Find(filter).Skip(skip).Limit(limit).ToList();

            if (Entity == null)
            {
                throw new Exception($"Entity is not found");
            }

            var entityStr = JsonConvert.SerializeObject(Entity);
            var entityMaps = JsonConvert.DeserializeObject<List<EntityMap>>(entityStr);

            foreach (var entityMap in entityMaps)
            {
                entityMap.Groups = new List<string>();
                entityMap.Owner = new UserMobileResult();
                var user = this.MongoDB.GetCollection<User>().Find(x => x.Id == entityMap.CreatedBy).FirstOrDefault();
                if (user != null)
                {
                    entityMap.Owner.Id = user.Id;
                    entityMap.Owner.Username = user.Username;
                    entityMap.Owner.Email = user.Email;
                    entityMap.Owner.PhoneNumber = user.AdditionalInfo == null ? "" : user.AdditionalInfo.PhoneNumber;
                    entityMap.Owner.FirstName = user.AdditionalInfo == null ? "" : user.AdditionalInfo.FirstName;
                    entityMap.Owner.LastName = user.AdditionalInfo == null ? "" : user.AdditionalInfo.LastName;
                    entityMap.Owner.Address = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Address;
                    entityMap.Owner.Position = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Position;
                    entityMap.Owner.Picture = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Picture;
                    entityMap.Owner.IsSelfieAuth = user.IsSelfieAuth;
                }
                entityMap.Subscription = new SubscriptionMap();
                var subscription = this.MongoDB.GetCollection<Subscription>().Find(x => x.CreatedBy == entityMap.CreatedBy).FirstOrDefault();
                if (subscription != null)
                {
                    entityMap.Subscription.ExpiredDate = subscription.ExpiredDate;
                    entityMap.Subscription.MaxEntity = 0;
                    entityMap.Subscription.CreatedEntity = 0;
                }
            }

            return entityMaps;
        }

        public List<EntityMemberMap> GetMember(ObjectId entityID, int skip, int limit, string keyword)
        {
            // Find Entity on Local Database
            var Entity = new List<Entity>();
            var filter = new BsonDocument();
            if (!string.IsNullOrEmpty(keyword))
            {
                filter = new BsonDocument {
                    { "$and", new BsonArray {
                        new BsonDocument {{ "_id", entityID } },
                            new BsonDocument{{ "$or", new BsonArray {
                                new BsonDocument {{ "Name", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                                new BsonDocument {{ "Description", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                                new BsonDocument {{ "Status", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } }
                            }}}
                        }
                    }
                };
            }
            else
            {
                filter.Add("_id", entityID);
            }

            Entity = this.MongoDB.GetCollection<Entity>()
                .Find(filter).ToList();

            if (Entity == null)
            {
                throw new Exception($"Entity is not found");
            }

            var entityMembers = new List<EntityMemberMap>();
            foreach (var en in Entity)
            {
                var user = this.MongoDB.GetCollection<User>().Find(x => x.Id == en.CreatedBy).FirstOrDefault();
                if (user == null)
                {
                    continue;
                }
                var entityMember = new EntityMemberMap();
                entityMember.Email = user.Email;
                entityMember.Id = en.Id.ToString();
                entityMember.Name = en.Name;
                //entityMember.PhoneNumber = user.AdditionalInfo.PhoneNumber;
                //entityMember.Picture = user.AdditionalInfo.Picture;
                //entityMember.Position = user.AdditionalInfo.Position;
                entityMember.UserID = user.Id;
                entityMember.Username = user.Username;

                entityMembers.Add(entityMember);
            }

            return entityMembers;
        }

        public EntityMap GetByID(string EntityID) {
            // Find Entity on Local Database
            var Entity = this.MongoDB.GetCollection<Entity>()
                .Find(x => x.Id == ObjectId.Parse(EntityID))
                .FirstOrDefault();

            if (Entity == null)
            {
                throw new Exception($"Entity {EntityID} is not found");
            }

            var entityStr = JsonConvert.SerializeObject(Entity);
            var entityMap = JsonConvert.DeserializeObject<EntityMap>(entityStr);

            entityMap.Groups = new List<string>();
            entityMap.Owner = new UserMobileResult();
            var user = this.MongoDB.GetCollection<User>().Find(x => x.Id == Entity.CreatedBy).FirstOrDefault();
            if (user != null)
            {
                entityMap.Owner.Id = user.Id;
                entityMap.Owner.Username = user.Username;
                entityMap.Owner.Email = user.Email;
                entityMap.Owner.PhoneNumber = user.AdditionalInfo == null ? "" : user.AdditionalInfo.PhoneNumber;
                entityMap.Owner.FirstName = user.AdditionalInfo == null ? "" : user.AdditionalInfo.FirstName;
                entityMap.Owner.LastName = user.AdditionalInfo == null ? "" : user.AdditionalInfo.LastName;
                entityMap.Owner.Address = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Address;
                entityMap.Owner.Position = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Position;
                entityMap.Owner.Picture = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Picture;
                entityMap.Owner.IsSelfieAuth = user.IsSelfieAuth;
            }
            entityMap.Subscription = new SubscriptionMap();
            var subscription = this.MongoDB.GetCollection<Subscription>().Find(x => x.CreatedBy == Entity.CreatedBy).FirstOrDefault();
            if (subscription != null)
            {
                entityMap.Subscription.ExpiredDate = subscription.ExpiredDate;
                entityMap.Subscription.MaxEntity = 0;
                entityMap.Subscription.CreatedEntity = 0;
            }

            return entityMap;
        }
    }

    public class SubscriptionMap
    {
        [JsonProperty(PropertyName = "expiredDate")]
        public Nullable<DateTime> ExpiredDate { get; set; }
        [JsonProperty(PropertyName = "maxEntity")]
        public int MaxEntity { get; set; }
        [JsonProperty(PropertyName = "createdEntity")]
        public int CreatedEntity { get; set; }
    }

    public class EntityMap
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "logo")]
        public string Logo { get; set; }
        [JsonProperty(PropertyName = "createdBy")]
        public string CreatedBy { get; set; }
        [JsonProperty(PropertyName = "createdDate")]
        public Nullable<DateTime> CreatedDate { get; set; }
        [JsonProperty(PropertyName = "lastUpdatedDate")]
        public Nullable<DateTime> LastUpdatedDate { get; set; }
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "administrators")]
        public string Administrators { get; set; }
        [JsonProperty(PropertyName = "groups")]
        public List<string> Groups { get; set; }
        [JsonProperty(PropertyName = "owner")]
        public UserMobileResult Owner { get; set; }
        [JsonProperty(PropertyName = "subscription")]
        public SubscriptionMap Subscription { get; set; }
    }

    public class EntityMemberMap
    {
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "phoneNumber")]
        public string PhoneNumber { get; set; }
        [JsonProperty(PropertyName = "picture")]
        public string Picture { get; set; }
        [JsonProperty(PropertyName = "position")]
        public string Position { get; set; }
        [JsonProperty(PropertyName = "userID")]
        public string UserID { get; set; }
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }
    }
}
