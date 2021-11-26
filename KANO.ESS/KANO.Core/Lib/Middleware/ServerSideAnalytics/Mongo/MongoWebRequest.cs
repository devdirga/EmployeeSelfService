using System;
using KANO.Core.Lib.Extension;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KANO.Core.Lib.Middleware.ServerSideAnalytics.Mongo
{
    [Collection("Logs")]
    [BsonIgnoreExtraElements]
    public class MongoWebRequest
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string Identity { get; set; }

        public string RemoteIpAddress { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string UserAgent { get; set; }

        public string Referer { get; set; }

        public bool IsWebSocket { get; set; }
        public int StatusCode { get; set; }

        public string Response { get; set; }

        public string Request { get; set; }

        public CountryCode CountryCode { get; set; }
    }
}