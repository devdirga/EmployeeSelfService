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
using System.Web;

namespace KANO.Api.Absence.Controllers
{
    [Route("api/absence/[controller]")]
    [ApiController]
    public class EntityController : ControllerBase
    {
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase DB;
        private readonly IConfiguration Configuration;
        private readonly Entity _entity;
        private readonly User _user;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public EntityController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _entity = new Entity(DB, Configuration);
            _user = new User(DB, Configuration);
        }

        //[Authorize]
        //[HttpGet("list")]
        //public IActionResult GetList()
        //{
        //    try
        //    {
        //        int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
        //        int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
        //        string keyword = Request.Query["search"].ToString();

        //        var result = _entity.Get(skip, limit, keyword);

        //        return Ok(new
        //        {
        //            data = result,
        //            message = "",
        //            success = true
        //        });
        //    }
        //    catch (Exception e)
        //    {
        //        return Ok(new
        //        {
        //            data = (object)null,
        //            message = Format.ExceptionString(e),
        //            success = false
        //        });
        //    }
        //}

        [Authorize]
        [HttpGet("list")]
        public IActionResult GetList()
        {
            try
            {
                string employeeID = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
                int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
                string keyword = Request.Query["search"].ToString();

                var result = _entity.MGet(employeeID, skip, limit, keyword);
                return ApiResult<List<EntityMap>>.Ok(result, result.Count);
            }
            catch (Exception e)
            {
                return ApiResult<List<EntityMap>>.Error(
                    HttpStatusCode.BadRequest, $"Fetching entitylist data error :\n{Format.ExceptionString(e)}");
            }
        }

        [Authorize]
        [HttpGet("member/list")]
        public IActionResult GetMemberList()
        {
            try
            {
                ObjectId entityID = String.IsNullOrEmpty(Request.Query["id"]) ? ObjectId.Empty : ObjectId.Parse(Request.Query["id"]);
                int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
                int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
                string keyword = Request.Query["search"].ToString();

                var result = _entity.GetMember(entityID, skip, limit, keyword);
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
        [HttpGet("{EntityID}")]
        public IActionResult GetByID(string EntityID)
        {
            try
            {
                var result = _entity.GetByID(EntityID);
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

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [Authorize]
        [HttpGet("mlist")]
        public IActionResult MList()
        {
            try {
                int skip = String.IsNullOrEmpty(Request.Query["skip"]) ? 0 : Int32.Parse(Request.Query["skip"]);
                int limit = String.IsNullOrEmpty(Request.Query["limit"]) ? 0 : Int32.Parse(Request.Query["limit"]);
                string keyword = Request.Query["search"].ToString();
                var result = _entity.MGet(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, skip, limit, keyword);
                return ApiResult<List<EntityMap>>.Ok(result, result.Count);
            }
            catch (Exception e) { return ApiResult<List<EntityMap>>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

    }
}