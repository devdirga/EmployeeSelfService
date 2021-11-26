using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    [Collection("GlobalConfig")]
    public class GenericConfig<T> : IMongoPreSave<GenericConfig<T>>
    {
        [BsonId]
        public string Id { get; set; }
        public string UpdateBy { get; set; }
        public DateTime lastUpdate { get; set; }
        public string ConfigModule { get; set; }
        public T ConfigValue { get; set; }

        public void PreSave(IMongoDatabase db)
        {
            if (string.IsNullOrEmpty(Id))
            {
                Id = ConfigModule;
            }
        }
        public static T GetConfig(IMongoDatabase db, string module, string id, T defaultValue = default(T))
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            if (string.IsNullOrWhiteSpace(module)) throw new ArgumentNullException("module");
            T ret = defaultValue;
            try
            {
                var cfg = db.GetCollection<GenericConfig<T>>().Find(x => x.Id == id && x.ConfigModule == module).FirstOrDefault();
                if (cfg != null)
                {
                    ret = cfg.ConfigValue;
                    if (ret == null) ret = defaultValue;
                }
            }
            catch { }
            return ret;
        }
        public static bool SetConfig(IMongoDatabase db, string module, string id, T value, string updateBy )
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentNullException("id");
            if (string.IsNullOrWhiteSpace(module)) throw new ArgumentNullException("module");
            var val = new GenericConfig<T>()
            {
                UpdateBy = updateBy,
                Id = id,
                ConfigModule = module,
                lastUpdate = Tools.ToUTC(DateTime.Now),
                ConfigValue = value
            };
            try
            {
                db.Save(val);
                return true;
            }
            catch { }
            return false;
        }
    }
}
