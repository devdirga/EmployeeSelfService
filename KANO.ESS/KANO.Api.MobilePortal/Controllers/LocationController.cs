using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KANO.Api.MobilePortal.Controllers
{
    [Route("api/mobileportal/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;

        public LocationController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
        }

        [HttpPost("get")]
        public IActionResult GetData([FromBody] KendoGrid param)
        {
            var result = new List<Location>();
            var groupMap = new Dictionary<string, string>();
            long total = 0;
            try
            {
                var tasks = new List<Task<TaskRequest<bool>>>();
                var filter = KendoMongoBuilder<Location>.BuildFilter(param);
                var sort = KendoMongoBuilder<Location>.BuildSort(param);

                // Fetch user data
                tasks.Add(Task.Run(() =>
                {
                    result = DB.GetCollection<Location>()
                        .Find(filter)
                        .ToList();

                    return TaskRequest<bool>.Create("Location", true);
                }));

                
                // Fetch total data
                tasks.Add(Task.Run(() => {
                    total = DB.GetCollection<Location>().CountDocuments(filter);
                    return TaskRequest<bool>.Create("Total", true);
                }));
                
                /*
                // Fetch role data
                tasks.Add(Task.Run(() => {
                    var groups = DB.GetCollection<Group>().Find(x => true).ToList();
                    foreach (var group in groups)
                    {
                        groupMap[group.Id] = group.Name;
                    }

                    return TaskRequest<bool>.Create("Role", true);
                }));
                */

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                foreach(var xx in result)
                {
                    //xx.Id = ObjectId.Parse(xx.Id);
                }
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Location>>.Ok(result, total);

        }

        [HttpPost("save")]
        public IActionResult Save([FromBody] paramLocation param)
        {
            try
            {
                var loc = new Location();
                if (!String.IsNullOrEmpty(param.Id))
                {
                    loc.Id = ObjectId.Parse(param.Id);
                }
                
                loc.Name = param.Name;
                loc.Address = param.Address;
                loc.Latitude = param.Latitude;
                loc.Longitude = param.Longitude;
                loc.Radius = param.Radius;
                loc.IsVirtual = param.IsVirtual;
                loc.Tags = param.Tags;
                loc.Status = "Active";
                loc.Entity = null;
                loc.Code = param.Code;
                //string userID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                loc.CreatedBy = param.CreatedBy;
                loc.CreatedDate = DateTime.UtcNow;
                loc.LastUpdatedDate = DateTime.UtcNow;
                DB.Save(loc);
                return ApiResult<object>.Ok(param, "Event has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving user :\n{Format.ExceptionString(e)}");
            }
        }
    }
}