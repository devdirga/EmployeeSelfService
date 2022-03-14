using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using static KANO.Core.Model.User;

namespace KANO.Core.Model
{
    [Collection("Users")]
    [BsonIgnoreExtraElements]
    public class User : BaseT, IMongoPreSave<User>, IMongoExtendedPostSave<User>, IMongoExtendedPostDelete<User>
    {
        [BsonIgnore]
        [JsonIgnore]
        protected IMongoDatabase MongoDB;
        [BsonIgnore]
        [JsonIgnore]
        protected IConfiguration Configuration;

        //[BsonIgnore]
        //[JsonIgnore]
        public string OdooID { get; set; }

        [BsonId]
        public string Id { get; set; }
        [BsonIgnore]
        public string Password { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        [JsonIgnore]
        public string PasswordHash { get; set; }
        [BsonIgnore]
        public string OldPassword { get; set; }
        public string ProfilePict { get; set; }
        //public List<LocationResult> Location { get; set; }
        public List<LocationMember> Location { get; set; }
        public DateTime LastLogin { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        private DateTime lastPasswordChangedDate { get; set; }
        public string FirebaseToken { get; set; }
        public DateTime LastPasswordChangedDate
        {
            get
            {
                if (this.lastPasswordChangedDate.Year == 1)
                {
                    return this.LastUpdate;
                }
                return this.lastPasswordChangedDate;
            }
            set
            {
                this.lastPasswordChangedDate = value;
            }
        }

        public string IsSelfieAuth { get; set; } = "no";

        public AdditionalUserInfo AdditionalInfo { get; set; }
        public bool Enable { get; set; } = false;
        [BsonIgnore]
        public string NewPassword
        {
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var hasher = new PasswordHasher<User>();
                    PasswordHash = hasher.HashPassword(this, value);
                    this.LastPasswordChangedDate = DateTime.Now;
                }
            }
        }
        public List<string> Roles { get; set; } = new List<string>();
        // public List<Building> Building { get; set; } = new List<Building>();

        [BsonIgnore]
        public string RoleDescription { get; set; }
        public IDictionary<string, string> UserData { get; set; } = new Dictionary<string, string>();


        //public string TenantReference { get; set; }
        //public string MemberReference { get; set; }

        public User() { }

        public User(IMongoDatabase mongoDB, IConfiguration configuration)
        {
            this.MongoDB = mongoDB;
            this.Configuration = configuration;
        }

        public PasswordVerificationResult VerifyPassword(string password)
        {
            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(this, PasswordHash, password);

            return result;
        }
        public void PreSave(IMongoDatabase db)
        {

            if (string.IsNullOrWhiteSpace(this.Id))
            {
                this.Id = ObjectId.GenerateNewId().ToString();
            }

            this.LastUpdate = DateTime.Now;

            if (!string.IsNullOrWhiteSpace(this.Password))
            {
                this.NewPassword = this.Password;
            }
            else if (string.IsNullOrWhiteSpace(this.PasswordHash))
            {
                var exUser = db.GetCollection<User>().Find(x => x.Id == this.Id).FirstOrDefault();
                if (exUser != null)
                {
                    this.PasswordHash = exUser.PasswordHash;
                }
                else
                {
                    throw new Exception("Password could not be empty");
                }
            }
        }

        public void PostSave(IMongoDatabase db, User originalObject)
        {
        }

        public void PostDelete(IMongoDatabase db, User originalObject)
        {
        }

        public bool IsOldPassword(string password)
        {
            return new PasswordHasher<User>()
                .VerifyHashedPassword(this, PasswordHash, password).ToString() == "Success";
        }

        public User GetEmployeeUser(string employeeID)
        {
            // Find User on Local Database
            var user = this.MongoDB.GetCollection<User>()
                .Find(x => x.Username == employeeID)
                .FirstOrDefault();

            if (user == null)
            {
                throw new Exception($"user {employeeID} is not found");
            }

            return user;
        }

        public List<User> Get(int skip, int limit, string keyword)
        {
            // Find User on Local Database
            var User = new List<User>();
            var filter = new BsonDocument();
            if (!string.IsNullOrEmpty(keyword))
            {
                filter = new BsonDocument {
                    { "$or", new BsonArray {
                        new BsonDocument {{ "User", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                        new BsonDocument {{ "FullName", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } },
                        new BsonDocument {{ "Email", new BsonDocument { { "$regex", keyword }, { "$options", "i" } } } }
                    }}
                };
            }

            User = this.MongoDB.GetCollection<User>().Find(filter).Skip(skip).Limit(limit).ToList();

            if (User == null)
            {
                throw new Exception($"User is not found");
            }

            return User;
        }

        public List<LocationResult> GetLocationsByEntityID(ObjectId entityID)
        {
            // Find Location on Local Database
            var Locations = new List<Location>();
            var filter = new BsonDocument();
            filter.Add("EntityID", entityID);

            Locations = this.MongoDB.GetCollection<Location>().Find(filter).ToList();

            if (Locations == null)
            {
                throw new Exception($"Location is not found");
            }

            var locationsStr = JsonConvert.SerializeObject(Locations);
            var locationResults = JsonConvert.DeserializeObject<List<LocationResult>>(locationsStr);

            foreach (var locationResult in locationResults)
            {
                locationResult.Groups = new List<string>();
                locationResult.Owner = new UserMobileResult();
                locationResult.Subscription = new SubscriptionMap();
            }

            return locationResults;
        }

        public List<LocationResult> GetLocationsByUserID(string userID)
        {
            // Find User on Local Database
            User User = this.MongoDB.GetCollection<User>().Find(x => x.Id == userID).FirstOrDefault();

            if (User == null)
            {
                throw new Exception($"User is not found");
            }

            var locationsStr = JsonConvert.SerializeObject(User.Location);
            var locationResults = JsonConvert.DeserializeObject<List<LocationResult>>(locationsStr);

            foreach (var locationResult in locationResults)
            {
                locationResult.Groups = new List<string>();
                locationResult.Owner = new UserMobileResult();
                locationResult.Subscription = new SubscriptionMap();
            }

            return locationResults;
        }

        public UserMobileResult GetByIDForMobile(string userID)
        {
            var filter = new BsonDocument();
            filter.Add("Id", userID);

            var user = this.MongoDB.GetCollection<User>().Find(filter).FirstOrDefault();

            if (user == null)
            {
                throw new Exception($"user is not found");
            }

            var result = new UserMobileResult();
            if (user != null)
            {
                result.Id = user.Id;
                result.Username = user.Username;
                result.Email = user.Email;
                result.PhoneNumber = user.AdditionalInfo == null ? "" : user.AdditionalInfo.PhoneNumber;
                result.FirstName = user.AdditionalInfo == null ? "" : user.AdditionalInfo.FirstName;
                result.LastName = user.AdditionalInfo == null ? "" : user.AdditionalInfo.LastName;
                result.Address = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Address;
                result.Position = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Position;
                result.Picture = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Picture;
                result.IsSelfieAuth = user.IsSelfieAuth;
            }

            return result;
        }

        public UserMobileResult MappingUserToUserMobile(User user)
        {
            var result = new UserMobileResult();
            if (user != null)
            {
                result.Id = user.Id;
                result.Username = user.Username;
                result.Email = user.Email;

                result.FirstName = user.FullName;
                result.LastName = user.FullName;
                result.IsSelfieAuth = user.IsSelfieAuth;

                /*
                result.PhoneNumber = user.AdditionalInfo == null ? "" : user.AdditionalInfo.PhoneNumber;
                result.FirstName = user.AdditionalInfo == null ? "" : user.AdditionalInfo.FirstName;
                result.LastName = user.AdditionalInfo == null ? "" : user.AdditionalInfo.LastName;
                result.Address = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Address;
                result.Position = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Position;
                result.Picture = user.AdditionalInfo == null ? "" : user.AdditionalInfo.Picture;
                */

                string curFile = System.IO.Path.Combine(Core.Lib.Helper.Configuration.GetEmployeeImagePath(Configuration), user.Id + ".jpg");
                result.Picture = System.IO.File.Exists(curFile) ? curFile : null;
            }

            return result;
        }

        public static void Init(IMongoDatabase db)
        {
            var administrator = "7312020022";
            var userCount = db.GetCollection<User>().CountDocuments(x => x.Id == administrator);
            if (userCount > 0) return;

            var user = new User
            {
                Id = administrator,
                CreateBy = "system",
                CreateDate = DateTime.Now,
                Enable = true,
                LastUpdate = DateTime.Now,
                Username = administrator,
                FullName = "ABDULLOH",
                Email = "abdulloh_dev_test@mailinator.com",
                NewPassword = "Password.1",
                Roles = new List<string>(new string[] { "Administrator" }),
                UserData = new Dictionary<string, string>(),
            };
            user.UserData.Add("gender", "Male");

            db.Save(user);
        }

        public User GetUserByID(String username)
        {
            return this.MongoDB.GetCollection<User>().Find(a => a.Username == username).FirstOrDefault();
        }

        public class AdditionalUserInfo
        {
            public string PhoneNumber { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Address { get; set; }
            public string Position { get; set; }
            public string Picture { get; set; }
        }
    }

    public class UserMobileResult
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
        [JsonProperty(PropertyName = "phoneNumber")]
        public string PhoneNumber { get; set; }
        [JsonProperty(PropertyName = "firstName")]
        public string FirstName { get; set; }
        [JsonProperty(PropertyName = "lastName")]
        public string LastName { get; set; }
        [JsonProperty(PropertyName = "address")]
        public string Address { get; set; }
        [JsonProperty(PropertyName = "position")]
        public string Position { get; set; }
        [JsonProperty(PropertyName = "picture")]
        public string Picture { get; set; }
        [JsonProperty(PropertyName = "isVisitor")]
        public bool IsVisitor { get; set; } = false;
        [JsonProperty(PropertyName = "IsInvitedButHaventAccepted")]
        public bool IsInvitedButHaventAccepted { get; set; } = false;
        [JsonProperty(PropertyName = "hashPassword")]
        public string HashPassword { get; set; } = "";
        [JsonProperty(PropertyName = "newPassword")]
        public string NewPassword { get; set; } = "";
        [JsonProperty(PropertyName = "oldPassword")]
        public string OldPassword { get; set; } = "";
        [JsonProperty(PropertyName = "createdDate")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        [JsonProperty(PropertyName = "lastUpdatedDate")]
        public DateTime LastUpdatedDate { get; set; } = DateTime.Now;
        [JsonProperty(PropertyName = "isSelfieAuth")]
        public string IsSelfieAuth { get; set; }
    }

    public class UploadForm
    {
        public string Username { get; set; }
        public IEnumerable<IFormFile> FileUpload { get; set; }
        public string PicturePath { get; set; }
    }

    public class ParamMemberLocation
    {
        public string Id { get; set; }
        public string[] LocationId { get; set; }
        public string UpdateBy { get; set; }
    }

    public class LocationMember
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
        public string Address { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double Radius { get; set; }
        public string EntityID { get; set; }
        public List<string> Tags { get; set; }
        public bool IsVirtual { get; set; }
        public Entity Entity { get; set; }
        public string CreatedBy { get; set; }
        public Nullable<DateTime> CreatedDate { get; set; }
        public Nullable<DateTime> LastUpdatedDate { get; set; }
        public string Status { get; set; }
    }

    public class UserLocation
    {
        public string Id { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string OldPassword { get; set; }
        public string ProfilePict { get; set; }
        public List<LocationResult> Location { get; set; }
        public DateTime LastLogin { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastPasswordChangedDate { get; set; }
        public AdditionalUserInfo AdditionalInfo { get; set; }
        public bool Enable { get; set; }
        public string NewPassword { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public string RoleDescription { get; set; }
        public IDictionary<string, string> UserData { get; set; } = new Dictionary<string, string>();
    }

    public class UserEvent
    {
        public string UserID { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
