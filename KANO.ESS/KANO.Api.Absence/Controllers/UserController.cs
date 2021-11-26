using System;
using System.Linq;
using KANO.Core.Model;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authorization;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System.IO;
using System.Globalization;

namespace KANO.Api.Absence.Controllers
{
    [Route("api/absence/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase DB;
        private readonly IConfiguration Configuration;
        private readonly User _user;
        private readonly ActivityType _activityType;
        private readonly ActivityLog _activityLog;
        private readonly Location _location;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public UserController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _user = new User(DB, Configuration);
            _activityType = new ActivityType(DB, Configuration);
            _activityLog = new ActivityLog(DB, Configuration);
            _location = new Location(DB, Configuration);
        }

        [HttpPost("login")]
        public IActionResult CreateToken()
        {
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken))
            {
                string authHeader = authToken.First();
                string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));
                int seperatorIndex = usernamePassword.IndexOf(':');
                var username = usernamePassword.Substring(0, seperatorIndex);
                var password = usernamePassword.Substring(seperatorIndex + 1);
                try
                {
                    User user = _user.GetEmployeeUser(username);
                    if (user != null)
                    {
                        if (user.VerifyPassword(password) != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                        {
                            //Add Claims
                            var claims = new[]
                            {
                            new Claim(JwtRegisteredClaimNames.UniqueName, user.Id),
                            new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                            new Claim(JwtRegisteredClaimNames.Jti, user.FullName),
                        };

                            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"]));
                            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                            var expire_minutes = Convert.ToDouble(Configuration["Tokens:ExpireMinutes"]);

                            var token = new JwtSecurityToken(Configuration["Tokens:Issuer"], Configuration["Tokens:Audience"],
                                claims,
                                expires: DateTime.Now.AddMinutes(expire_minutes),
                                signingCredentials: creds);

                            Response.Cookies.Append("Access-Token", Convert.ToString(token), new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });

                            return Ok(new
                            {
                                data = new JwtSecurityTokenHandler().WriteToken(token),
                                message = "",
                                success = true
                            });
                        }
                    }

                }
                catch(Exception e)
                {
                    return Ok(new
                    {
                        data = "",
                        message = e.Message,
                        success = false
                    });
                }
                
            }

            string res = String.Empty; //Use String Class
            return Ok(new
            {
                data = res,
                message = "Invalid user password",
                success = false
            });
        }

        [HttpPost("me/logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("Access-Token");
            string token = ""; 
            return Ok(new
            {
                data = token,
                message = "",
                success = true
            });
        }

        [Authorize]
        [HttpGet("me")]
        public IActionResult GetProfile()
        {
            string employeeID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                var user = _user.GetEmployeeUser(employeeID);
                var result = _user.MappingUserToUserMobile(user);
                return Ok(new
                {
                    data = result,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpPut("me")]
        public IActionResult UpdateProfile([FromBody] User param)
        {
            string employeeID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                var result = _user.GetEmployeeUser(employeeID);
                result.Username = param.Username;
                result.FullName = param.FullName;
                result.Email = param.Email;
                result.LastUpdate = DateTime.Now;
                DB.Save(result);
                var userMobile = _user.MappingUserToUserMobile(result);
                return Ok(new
                {
                    data = userMobile,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpPost("register")]
        public IActionResult Save([FromBody] User param)
        {
            try
            {
                DB.Save(param);
                return Ok(new
                {
                    data = param,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [HttpPost("upload")]
        public IActionResult Upload([FromForm] UploadForm param)
        {
            try
            {
                var user = _user.GetEmployeeUser(param.Username);
                param.PicturePath = Upload(Configuration, user.Username, user.AdditionalInfo.Picture, param.FileUpload.FirstOrDefault());
                user.AdditionalInfo.Picture = param.PicturePath;
                DB.Save(user);
                return Ok(new
                {
                    data = param,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpPut("me/change-password")]
        public IActionResult ChangePassword([FromBody] paramChangePassword param)
        {
            string employeeID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                var user = _user.GetEmployeeUser(employeeID);

                if (user != null)
                {
                    if (user.VerifyPassword(param.OldPassword) != Microsoft.AspNetCore.Identity.PasswordVerificationResult.Failed)
                    {
                        var hasher = new PasswordHasher<User>();
                        user.PasswordHash = hasher.HashPassword(user, param.NewPassword);

                        var res = hasher.VerifyHashedPassword(user, user.PasswordHash, param.NewPassword);
                        //DB.Save(user);
                    }
                }

                var result = _user.GetEmployeeUser(employeeID);
                return Ok(new
                {
                    data = result,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpGet("list")]
        public IActionResult GetList()
        {
            try
            {
                int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
                int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
                string keyword = Request.Query["search"].ToString();

                var result = _user.Get(skip, limit, keyword);
                var userMobiles = new List<UserMobileResult>();
                foreach (var res in result)
                {
                    userMobiles.Add(_user.MappingUserToUserMobile(res));
                }

                return Ok(new
                {
                    data = userMobiles,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpGet("locationlist")]
        public IActionResult GetLocationsByEntityID()
        {
            try
            {
                ObjectId entityID = String.IsNullOrEmpty(Request.Query["entityID"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["entityID"].ToString());

                var result = _user.GetLocationsByEntityID(entityID);
                return Ok(new
                {
                    data = result,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpGet("locations")]
        public IActionResult GetLocationsByUserID()
        {
            try
            {
                string userID = Request.Query["userID"];

                var result = _user.GetLocationsByUserID(userID);
                return Ok(new
                {
                    data = result,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        [Authorize]
        [HttpGet("locations-distance")]
        public IActionResult GetLocationsDistanceByUserID()
        {
            try
            {
                string userID = Request.Query["userID"];
                double slatitude = String.IsNullOrEmpty(Request.Query["latitude"]) ? 0.0 : double.Parse(Request.Query["latitude"], CultureInfo.InvariantCulture);
                double slongitude = String.IsNullOrEmpty(Request.Query["longitude"]) ? 0.0 : double.Parse(Request.Query["longitude"], CultureInfo.InvariantCulture);

                var sCoord = new GeoCoordinate();
                sCoord.Latitude = slatitude;
                sCoord.Longitude = slongitude;

                var locations = _user.GetLocationsByUserID(userID);
                var locationsStr = JsonConvert.SerializeObject(locations);
                List<dynamic> result = JsonConvert.DeserializeObject<List<dynamic>>(locationsStr);

                foreach (var res in result)
                {
                    var dCoord = new GeoCoordinate();
                    dCoord.Latitude = res.latitude;
                    dCoord.Longitude = res.longitude;
                    res.distance = _location.CalculateDistance(sCoord, dCoord);
                }

                return Ok(new
                {
                    data = result,
                    message = "",
                    success = true
                });
            }
            catch (Exception e)
            {
                return Ok(new
                {
                    data = (object)null,
                    message = Format.ExceptionString(e),
                    success = false
                });
            }
        }

        public class paramChangePassword
        {
            public string OldPassword { get; set; }
            public string NewPassword { get; set; }
        }

        public string Upload(IConfiguration configuration, string username, string oldpicturePath, IFormFile FileUpload)
        {
            var uploadDirectory = Core.Lib.Helper.Configuration.UploadPath(configuration);
            var maxFilesize = Core.Lib.Helper.Configuration.UploadMaxFileSize(configuration);
            var allowedExtension = Core.Lib.Helper.Configuration.UploadAllowedExtensions(configuration);

            // Currently we limit only one file upload
            var file = FileUpload;

            // File validation
            if (file.Length > maxFilesize)
            {
                throw new Exception($"File upload size could not be more than {Format.FormatFileSize(maxFilesize)}");
            }

            if (!allowedExtension.Contains(Path.GetExtension(file.FileName)))
            {
                throw new Exception($"File upload extension should be {string.Join(", ", allowedExtension)}");
            }

            // New file path and name preparation
            var newFilename = Tools.SanitizeFileName(String.Format("{0}_{1}{2}", username, DateTime.Now.ToLocalTime().ToString("ddMMyyyyHHmmssff"), Path.GetExtension(file.FileName)));
            newFilename = newFilename.Replace(" ", "_");
            var newFilepath = Path.Combine(uploadDirectory, newFilename);

            // Upload file
            using (var fileStream = new FileStream(newFilepath, FileMode.Create))
            {
                file.CopyTo(fileStream);
            }

            // Delete Old picture
            if (!string.IsNullOrEmpty(oldpicturePath))
            {
                System.IO.File.Delete(oldpicturePath);
            }

            return newFilepath;
        }

    }
}