using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json.Bson;
using System;
namespace KANO.Core.Model
{
    public class Audit<T> 
    {
        [BsonId]
        public int Id { get; set; }
        public T obj { get; set; }
        public DateTime CreateDate { get; set; }
        public string LogType { get; set; }
        public string Creator { get; set; }
        public Audit(T t, string type, string creator, IMongoDatabase db)
        {
            CreateDate = Tools.ToUTC(DateTime.Now);
            LogType = type;
            Creator = creator;
            obj = t;
            this.Id = SequenceNo.Get(db, "Log_" + obj.GetType().FullName).ClaimAsInt(db);

        }

    }
}
