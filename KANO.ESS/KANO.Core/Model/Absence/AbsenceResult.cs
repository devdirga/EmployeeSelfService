using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

using MongoDB.Bson;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;

using KANO.Core.Lib.Extension;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using KANO.Core.Service;
using KANO.Core.Service.AX;



namespace KANO.Core.Model
{
    public class BasicResult
    {
        public string Data { get; set; }
        public string Message { get; set; }
        public string Success { get; set; }
    }

    public class ActivityLogResult
    {
        public string Id { get; set; }
        public string EntityID { get; set; }
        public string ActivityTypeID { get; set; }
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
    }

    public class ActivityTypeResult
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "entityID")]
        public string EntityID { get; set; }
        [JsonProperty(PropertyName = "uniqueKey")]
        public string UniqueKey { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "category")]
        public string Category { get; set; }
        [JsonProperty(PropertyName = "preActivities")]
        public List<ObjectId> PreActivities { get; set; }
        [JsonProperty(PropertyName = "postActivities")]
        public List<ObjectId> PostActivities { get; set; }
        [JsonProperty(PropertyName = "icon")]
        public string Icon { get; set; }
        [JsonProperty(PropertyName = "createdBy")]
        public string CreatedBy { get; set; }
        [JsonProperty(PropertyName = "createdDate")]
        public Nullable<DateTime> CreatedDate { get; set; }
        [JsonProperty(PropertyName = "lastUpdatedDate")]
        public Nullable<DateTime> LastUpdatedDate { get; set; }
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        public string IsSelfieAuth { get; set; }
    }

    public class EntityResult
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
    }

    public class EventResult
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "organizer")]
        public string Organizer { get; set; }
        [JsonProperty(PropertyName = "locationID")]
        public string LocationID { get; set; }
        [JsonProperty(PropertyName = "entityID")]
        public string EntityID { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "startTime")]
        public Nullable<DateTime> StartTime { get; set; }
        [JsonProperty(PropertyName = "endTime")]
        public Nullable<DateTime> EndTime { get; set; }
        [JsonProperty(PropertyName = "attendees")]
        public  List<AttendeeResult> Attendees { get; set; }
        [JsonProperty(PropertyName = "createdBy")]

        public string CreatedBy { get; set; }
        [JsonProperty(PropertyName = "createdDate")]

        public Nullable<DateTime> CreatedDate { get; set; }
        [JsonProperty(PropertyName = "lastUpdatedDate")]

        public Nullable<DateTime> LastUpdatedDate { get; set; }
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "location")]
        public LocationMap Location { get; set; }
        [JsonProperty(PropertyName = "organizerDetail")]
        public OrganizerDetailMap OrganizerDetail { get; set; }
    }

    public class AttendeeResult
    {
        [JsonProperty(PropertyName = "userID")]
        public string UserID { get; set; }
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
    }

    public class LocationResult
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "entityID")]
        public string EntityID { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }
        [JsonProperty(PropertyName = "longitude")]
        public double Longitude { get; set; }
        [JsonProperty(PropertyName = "latitude")]
        public double Latitude { get; set; }
        [JsonProperty(PropertyName = "radius")]
        public double Radius { get; set; }
        [JsonProperty(PropertyName = "tags")]
        public List<string> Tags { get; set; }
        [JsonProperty(PropertyName = "isVirtual")]
        public bool IsVirtual { get; set; }
        [JsonProperty(PropertyName = "entity")]
        public EntityResult Entity { get; set; }
        [JsonProperty(PropertyName = "createdBy")]
        public string CreatedBy { get; set; }
        [JsonProperty(PropertyName = "createdDate")]
        public Nullable<DateTime> CreatedDate { get; set; }
        [JsonProperty(PropertyName = "lastUpdatedDate")]
        public Nullable<DateTime> LastUpdatedDate { get; set; }
        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }
        [JsonProperty(PropertyName = "groups")]
        public List<string> Groups { get; set; }
        [JsonProperty(PropertyName = "owner")]
        public UserMobileResult Owner { get; set; }
        [JsonProperty(PropertyName = "subscription")]
        public SubscriptionMap Subscription { get; set; }
    }

    public class UserResult
    {
        public string Id { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string OldPassword { get; set; }
        public string ProfilePict { get; set; }
        public DateTime LastLogin { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        private DateTime lastPasswordChangedDate { get; set; }
        public DateTime LastPasswordChangedDate { get; set; }

        public bool Enable { get; set; }
        public string NewPassword { get; set; }

        public List<string> Roles { get; set; }
        public string RoleDescription { get; set; }
    }

    public class ActivityLogMap
    {
        [JsonProperty(PropertyName = "createdBy")]
        public string CreatedBy { get; set; }
        [JsonProperty(PropertyName = "createdDate")]
        public Nullable<DateTime> CreatedDate { get; set; }
        [JsonProperty(PropertyName = "dateTime")]
        public Nullable<DateTime> DateTime { get; set; }
        [JsonProperty(PropertyName = "entityID")]
        public string EntityID { get; set; }
        [JsonProperty(PropertyName = "entityName")]
        public string EntityName { get; set; } = "TPS";
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "lastUpdatedDate")]
        public Nullable<DateTime> LastUpdatedDate { get; set; }
        [JsonProperty(PropertyName = "latitude")]
        public double Latitude { get; set; }
        [JsonProperty(PropertyName = "longitude")]
        public double Longitude { get; set; }
        [JsonProperty(PropertyName = "submittedBy")]
        public string SubmittedBy { get; set; }
        [JsonProperty(PropertyName = "activityType")]
        public ActivityTypeMap ActivityType { get; set; }
        [JsonProperty(PropertyName = "location")]
        public LocationMap Location { get; set; }
        [JsonProperty(PropertyName = "user")]
        public UserMap User { get; set; }
    }
    public class ActivityTypeMap
    {
        [JsonProperty(PropertyName = "activityTypeID")]
        public string ActivityTypeId { get; set; }
        [JsonProperty(PropertyName = "activityTypeName")]
        public string ActivityTypeName { get; set; }
    }

    public class LocationMap
    {
        [JsonProperty(PropertyName = "locationID")]
        public string LocationID { get; set; }
        [JsonProperty(PropertyName = "locationName")]
        public string LocationName { get; set; }
        [JsonProperty(PropertyName = "locationAddress")]
        public string LocationAddress { get; set; } = "";
    }
    public class OrganizerDetailMap
    {
        [JsonProperty(PropertyName = "firstName")]
        public string FirstName { get; set; } = "";
        [JsonProperty(PropertyName = "lastName")]
        public string LastName { get; set; } = "";
        [JsonProperty(PropertyName = "phoneNumber")]
        public string PhoneNumber { get; set; } = "";


    }
    public class UserMap
    {
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
        [JsonProperty(PropertyName = "firstName")]
        public string FirstName { get; set; }
        [JsonProperty(PropertyName = "lastName")]
        public string LastName { get; set; }
        [JsonProperty(PropertyName = "userID")]
        public string UserId { get; set; }
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }
    }

    public class DashboardActivity
    {
        public DateTime TimeAbsence { get; set; }
        public long Total { get; set; }
    }
    public class DashboardActivityDetail
    {
        public string EmployeeID { get; set; }
        public DateTime TimeAbsence { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }

        public string LocationID { get; set; }
        public string LocationName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }

    public class SurveyResult
    {
        [JsonProperty(PropertyName = "id")]
        public string  Id { get; set; }
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        [JsonProperty(PropertyName = "surveyUrl")]
        public Uri SurveyUrl { get; set; }

    }

    public class AbsenceForm
    {
        public string typeID { get; set; }
        public string EntityID { get; set; }
        public string ActivityTypeID { get; set; }
        public string LocationID { get; set; }
        public Double Longitude { get; set; }
        public Double Latitude { get; set; }
        public string InOut { get; set; }
        public string Photo { get; set; }
        public string LogoContent { get; set; }
        public string LogoMIME { get; set; }
    }

    [Collection("Absences")]
    [BsonIgnoreExtraElements]
    public class Absences : BaseDocumentVerification, IMongoPreSave<Absences>
    {
        public string typeID { get; set; }
        public string EntityID { get; set; }
        public string ActivityTypeID { get; set; }
        public string LocationID { get; set; }
        public Double Longitude { get; set; }
        public Double Latitude { get; set; }
        public string InOut { get; set; }
        public string Photo { get; set; }
        public bool Temporary { get; set; }
    }
}
