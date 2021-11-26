using KANO.Core.Lib.Extension;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using System;

namespace KANO.Core.Service
{
    [Collection("GlobalConfig")]
    public class Config : IMongoPreSave<Config>
    {

        [BsonId]
        public string Id { get; set; }
        public DateTime lastUpdate { get; set; }
        public string ConfigModule { get; set; }
        public object ConfigValue { get; set; }
        public string UpdateBy { get; set; }
        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = ConfigModule; // ObjectId.GenerateNewId().ToString();
            }
        }

        public static object GetConfigValue(IMongoDatabase db,  string _id, object defaultValue = null)
        {
            object ret = defaultValue;
            Config cfg = db.GetCollection<Config>().Find(x => x.Id == _id).FirstOrDefault(); //DB Config.Get<Config>(_id);
            if (cfg != null)
            {
                ret = cfg.ConfigValue;
                if (ret == null) ret = defaultValue;
            }
            return ret;
        }
    }
}
