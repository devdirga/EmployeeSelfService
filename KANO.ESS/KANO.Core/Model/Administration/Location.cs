using KANO.Core.Lib.Extension;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace KANO.Core.Model
{
    [Collection("Location")]
    [BsonIgnoreExtraElements]
    public class Location : BaseT, IMongoPreSave<Location> , IMongoExtendedPostSave<Location>, IMongoExtendedPostDelete<Location>
    {
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        [BsonId]
        public ObjectId Id { get; set; }
        [BsonIgnore]
        [JsonIgnore]
        public ObjectId EntityID { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Address { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Radius { get; set; }
        public List<string> Tags { get; set; }
        public bool IsVirtual { get; set; }
        public Entity Entity { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<DateTime> CreatedDate { get; set; }
        public Nullable<DateTime> LastUpdatedDate { get; set; }
        public string Status { get; set; }
        public string Unit { get; set; }
        public Location() { }

        public Location(IMongoDatabase mongoDB, IConfiguration configuration)  {
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public void PreSave(IMongoDatabase db)
        {
        }

        public void PostSave(IMongoDatabase db, Location originalObject)
        {
        }

        public void PostDelete(IMongoDatabase db, Location originalObject)
        {
        }

        public List<Location> Get(ObjectId entityID, int skip, int limit, string keyword)
        {
            BsonDocument filter = new BsonDocument();
            if (!string.IsNullOrEmpty(keyword))
            {
                filter = new BsonDocument {
                    { "$and", new BsonArray {
                        new BsonDocument {{ "EntityID", entityID } },
                        new BsonDocument{{ "$or", new BsonArray {
                            new BsonDocument {{ "Name", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Address", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Tags", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Entity.Id", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Entity.Name", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Entity.Description", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                            new BsonDocument {{ "Status", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } }
                        }}}
                        }
                    }
                };
            }
            else
            {
                filter.Add("EntityID", entityID);
            }

            List<Location> location = this.MongoDB.GetCollection<Location>().Find(filter).Skip(skip).Limit(limit).ToList();

            if (location == null)
            {
                throw new Exception($"Location is not found with {keyword}");
            }

            return location;
        }

        public Location GetByID(string locationID) 
        {
            Location Location = this.MongoDB.GetCollection<Location>().Find(x => x.Id == ObjectId.Parse(locationID)).FirstOrDefault();
            
            if (Location == null)
            {
                throw new Exception($"Location {locationID} is not found");
            }
            
            return Location;
        }

        public Location GetByCode(string code)
        {
            Location Location = this.MongoDB.GetCollection<Location>().Find(x => x.Code == code).FirstOrDefault();
            if(Location == null)
            {
                throw new Exception($"Location with code {code} is not found");
            }

            return Location;
        }

        public double CalculateDistance(GeoCoordinate sCoordinate, GeoCoordinate dCoordinate)
        {
            var d1 = sCoordinate.Latitude * (Math.PI / 180.0);
            var num1 = sCoordinate.Longitude * (Math.PI / 180.0);
            var d2 = dCoordinate.Latitude * (Math.PI / 180.0);
            var num2 = dCoordinate.Longitude * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                     Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);
            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
        }

    }

    public class GeoCoordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class paramLocation
    {
        public string Id { get; set; }
        public string EntityID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Radius { get; set; }
        public List<string> Tags { get; set; }
        public bool IsVirtual { get; set; }

        public Entity Entity { get; set; }

        public string CreatedBy { get; set; }

        public string Code { get; set; }

    }
}
