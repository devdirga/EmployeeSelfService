using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Lib.Middleware.ServerSideAnalytics.Mongo;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KANO.Api.Administration.Controllers
{
    [Route("api/administration/[controller]")]
    public class UserController : Controller
    {

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private UpdateRequest _updateRequest;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public UserController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }


        [HttpGet("get/detail/{Username}")]
        public IActionResult GetDetail(string Username)
        {
            var UserModel = new List<User>();
            try
            {
                UserModel = DB.GetCollection<User>().Find(x => x.Username == Username).ToList();
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<User>>.Ok(UserModel);
        }

        [HttpGet("get/detailuser/{Username}")]
        public IActionResult GetDetailUser(string Username)
        {
            var UserModel = new User();
            try
            {
                UserModel = DB.GetCollection<User>().Find(x => x.Username == Username).FirstOrDefault();
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading User :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<User>.Ok(UserModel);
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
                    var groups = DB.GetCollection<Group>().Find(x=>true).ToList();
                    foreach (var group in groups) {
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

                foreach (var user in result) {
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

        [HttpPost("save")]
        public IActionResult Save([FromBody] User param)
        {
            // var data = JsonConvert.DeserializeObject<UserModel>(param);

            // var oldData = DB.GetCollection<UserModel>()
            //     .Find(x => x.Id == param.Id)
            //     .FirstOrDefault();

            try
            {
                if (!string.IsNullOrWhiteSpace(param.Password)) {
                    param.NewPassword= param.Password;
                }
                DB.Save(param);
                return ApiResult<object>.Ok(param, "User has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving user :\n{Format.ExceptionString(e)}");
            }

        }
    }
}
