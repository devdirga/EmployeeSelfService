using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace KANO.Api.MobilePortal.Controllers
{
    [Route("api/mobileportal/[controller]")]
    [ApiController]
    public class MemberController : ControllerBase
    {

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public MemberController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
        }

        [HttpPost("get/data")]
        public IActionResult GetData([FromBody] KendoGrid param)
        {
            var result = new List<User>();
            var groupMap = new Dictionary<string, string>();
            long total = 0;
            try
            {
                var tasks = new List<Task<TaskRequest<bool>>>();
                var filter = KendoMongoBuilder<User>.BuildFilter(param);
                var sort = KendoMongoBuilder<User>.BuildSort(param);

                // Fetch user data
                tasks.Add(Task.Run(() =>
                {
                    result = DB.GetCollection<User>()
                        .Find(filter)
                        .Limit(param.Take)
                        .Skip(param.Skip)
                        .Sort(sort)
                        .ToList();

                    return TaskRequest<bool>.Create("User", true);
                }));

                // Fetch total data
                tasks.Add(Task.Run(() => {
                    total = DB.GetCollection<User>().CountDocuments(filter);
                    return TaskRequest<bool>.Create("Total", true);
                }));

                // Fetch role data
                tasks.Add(Task.Run(() => {
                    var groups = DB.GetCollection<Group>().Find(x => true).ToList();
                    foreach (var group in groups)
                    {
                        groupMap[group.Id] = group.Name;
                    }

                    return TaskRequest<bool>.Create("Role", true);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                foreach (var user in result)
                {
                    var roleDescription = "";
                    var role = user.Roles.First();
                    if (groupMap.TryGetValue(role, out roleDescription))
                    {
                        user.RoleDescription = roleDescription;
                    }
                    else
                    {
                        user.RoleDescription = role;
                    }
                }
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<User>>.Ok(result, total);

        }

        [HttpPost("save/location")]
        public IActionResult Save([FromBody] User param)
        {
            try
            {
                //var loc = DB.GetCollection<Location>().Find(_ => true).ToList();
                //var dd = loc.Where(f => param.LocationId.Any(g => f.Id.ToString().Contains(g)) || param.LocationId.Any(g => f.Id.ToString().Contains(g))).ToList();
                var user = DB.GetCollection<User>().Find(z => z.Id == param.Id).FirstOrDefault();
                user.Location = param.Location;
                user.IsSelfieAuth = param.IsSelfieAuth;
                user.UpdateBy = param.UpdateBy;
                user.LastUpdate = DateTime.Now;

                DB.Save(user);
                return ApiResult<object>.Ok(param, "Event has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving user :\n{Format.ExceptionString(e)}");
            }
        }
    }
}
