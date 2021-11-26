using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
namespace KANO.Core.Service
{
    public class DataHelper
    {

        public DataHelper(IMongoManager mongo, IConfiguration _Configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = _Configuration;
        }

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        public IConfiguration Configuration;

        public List<T> Populate<T>(string memoryId,
          IMongoQuery q = null, int take = 0, int skip = 0,
          IEnumerable<string> fields = null,
          SortByBuilder sort = null,
          string collectionName = "", bool memoryObject = false, bool forceReadDb = false)
        {
            List<BsonDocument> docs = Populate(memoryId, q, take, skip, fields, sort, collectionName, memoryObject, forceReadDb);
            List<T> ret = new List<T>();
            foreach (var doc in docs)
            {
                try
                {
                    ret.Add(BsonSerializer.Deserialize<T>(doc));
                }
                catch (Exception)
                {
                    throw new Exception("Unable to deserialize BsonDocument => " + JsonConvert.SerializeObject(doc.ToDictionary()));
                }
            }
            return ret;
        }
        internal MongoDatabase _db;
        public MongoDatabase GetDb()
        {
            if (_db == null)
            {
                var url = Configuration["MongoDBCache:ConnectionString"];
                var dbname = MongoUrl.Create(url).DatabaseName;
                var conn = new MongoClient(url);
                _db = conn.GetDatabase(dbname) as MongoDatabase;

            }
            return _db;
        }
        public List<BsonDocument> Populate(string memoryId,
           IMongoQuery q = null, int take = 0, int skip = 0,
           IEnumerable<string> fields = null,
           SortByBuilder sort = null,
           string collectionName = "", bool memoryObject = false, bool forceReadDb = false)
        {
            var ret = new List<BsonDocument>();
            if (collectionName.Equals(""))
                collectionName = memoryId;

            //if (memoryObject == true && forceReadDb == false)
            //    ret = MemoryHelper.Populate<BsonDocument>(memoryId);
            //bool saveToMemory = false;
            if (ret.Count == 0)
            {
                var cursor = q == null ?
                    GetDb().GetCollection<BsonDocument>(collectionName).Find(null) :
                    GetDb().GetCollection<BsonDocument>(collectionName).Find(q);
                if (fields != null && fields.Count() > 0) cursor.SetFields(fields.ToArray());
                if (sort != null) cursor.SetSortOrder(sort);
                if (take == 0)
                {
                    ret = cursor.AsQueryable().ToList();
                }
                else
                {
                    cursor.SetSkip(skip);
                    cursor.SetLimit(take);
                    return cursor.ToList();
                }
                //if (memoryObject == true)
                //    saveToMemory = true;
            }

            //if (saveToMemory)
            //    MemoryHelper.Save(memoryId, ret.Select(d=>(object)d).ToList());

            return ret;
        }

    }
}
