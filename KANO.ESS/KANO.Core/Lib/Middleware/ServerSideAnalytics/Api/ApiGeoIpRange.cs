using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace KANO.Core.Lib.Middleware.ServerSideAnalytics.Api
{
    internal class ApiGeoIpRange
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public CountryCode CountryCode { get; set; }
    }
}
