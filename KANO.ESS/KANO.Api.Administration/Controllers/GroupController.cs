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

namespace KANO.Api.Administration.Controllers
{
    [Route("api/administration/[controller]")]
    [ApiController]
    public class GroupController : ControllerBase
    {

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public GroupController(IMongoManager mongo, IConfiguration conf)
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


        [HttpGet("getdetail/{Id}")]
        public IActionResult GetDetail(string Id)
        {
            var GroupsManagement = new Group();
            try
            {
                GroupsManagement = DB.GetCollection<Group>().Find(x => x.Id == Id).FirstOrDefault();
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading Group :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<Group>.Ok(GroupsManagement);
        }

        [HttpGet("get/data")]
        public IActionResult GetData()
        {

            var GroupsManagement = new List<Group>();
            try
            {
                GroupsManagement = DB.GetCollection<Group>().Find(x => x.Id != "").ToList().OrderByDescending(x => x.Name).ToList();
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading Group :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Group>>.Ok(GroupsManagement);

        }

        [HttpGet("get/user/{username}")]
        public IActionResult GetDataByUserID(string username)
        {

            var groups = new List<Group>();
            try
            {
                var useDefaultSetting = false;
                var user = DB.GetCollection<User>().Find(x=>x.Username==username).FirstOrDefault();
                if(user != null){                    
                    groups = DB.GetCollection<Group>().Find(x => x.Id != "" && user.Roles.Contains(x.Id)).ToList().OrderByDescending(x => x.Name).ToList();
                    useDefaultSetting = (groups ==null || groups.Count() == 0);
                }else{
                    useDefaultSetting = true;
                }                

                if(useDefaultSetting){
                    var group = new Group
                    {
                        Id = "default",
                        CreateBy = "system",
                        CreateDate = DateTime.Now,
                        Enable = true,
                        LastUpdate = DateTime.Now,
                        Name = "default",
                        Grant = new List<AccessGrant>(),
                        Types = new List<int>(),
                    };

                    var pages = DB.GetCollection<Page>().Find(x=> true).ToList();
                    foreach (var page in pages)
                    {
                        group.Grant.Add(new AccessGrant
                        {
                            Actions=ActionGrant.None,
                            PageID=page.Id,
                            PageCode=page.PageCode,
                            PageTitle=page.Title,
                        });
                    }

                    // foreach (int type in Enum.GetValues(typeof(SpecialGroupType))) {
                    //     group.Types.Add(type);
                    // }

                    groups.Add(group);
                }
                
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading Group :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Group>>.Ok(groups);

        }

        [HttpPost("save")]
        public IActionResult Save([FromBody] Group param)
        {

            if (Group.IsNameAlreadyUsed(DB, param))
            {
                return ApiResult<Group>.Ok(param, message: "This Group Name is already been used, please specify another Group Name!");
            }
            else
            {
                try
                {
                    DB.Save<Group>(param);
                    return ApiResult<Group>.Ok(param, "Group has been saved successfully");
                }
                catch (Exception e)
                {
                    return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving Group :\n{Format.ExceptionString(e)}");
                }
            }

        }
    }
}
