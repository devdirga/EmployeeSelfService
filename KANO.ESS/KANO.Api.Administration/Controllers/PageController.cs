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

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KANO.Api.Administration.Controllers
{
    [Route("api/administration/[controller]")]
    public class PageController : Controller
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private UpdateRequest _updateRequest;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public PageController(IMongoManager mongo, IConfiguration conf)
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

        [HttpGet("detail/{Id}")]
        public IActionResult GetDetail(string Id)
        {
            var pageModel = new List<Page>();
            try
            {
                pageModel = DB.GetCollection<Page>().Find(x => x.Id == Id).ToList();
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading Page Management :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Page>>.Ok(pageModel);
        }

        [HttpGet("user/{username}/{flat=false}")]
        public IActionResult FindByUserLogin(string username, bool flat = false)
        {
            
            var dataUser = DB.GetCollection<User>().Find(x => x.Username == username).FirstOrDefault();

            var menus = MenuPage.GenerateFromGroupIds(DB, dataUser.Roles.ToArray(), true, flat);
            return ApiResult.Ok(menus);
        }

        [HttpGet("get")]
        public IActionResult GetData()
        {

            var PageManagement = new List<Page>();
            try
            {
                PageManagement = DB.GetCollection<Page>().Find(x => x.Id != "").ToList().OrderByDescending(x => x.PageCode).ToList();
            }
            catch (Exception e)
            {
                ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error loading Page Management :\n{Format.ExceptionString(e)}");
            }

            return ApiResult<List<Page>>.Ok(PageManagement);

        }

        [HttpPost("save")]
        public IActionResult Save([FromBody]  Page param)
        {

            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            if (string.IsNullOrEmpty(param.PageCode) && !string.IsNullOrEmpty(param.Title))
            {
                param.Id = textInfo.ToTitleCase(param.Title).Replace(" ", string.Empty);
                param.PageCode = textInfo.ToTitleCase(param.Title).Replace(" ", string.Empty);
                param.CreateBy = param.UpdateBy;
                param.CreateDate = DateTime.Now;
            }

            try
            {
                DB.Save(param);
                return ApiResult<Page>.Ok(param, "Page Management has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<Page>.Error(HttpStatusCode.BadRequest, $"Error saving Page Management :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpGet("delete")]
        public IActionResult Delete([FromForm] Page param)
        {
            DB.Delete<Page>(param);

            return ApiResult<String>.Ok(param.Id);
        }

        [HttpGet("update")]
        public IActionResult Update([FromForm] paramModel param)
        {
            if (param.ID.Length == 0)
            {
                throw new Exception("Empty Old ID");
            }
            Page us = param.Value;

            DB.Save<Page>(us);

            return ApiResult<Page>.Ok(param.Value);
        }


        public class paramModel
        {
            public Page Value { get; set; }
            public string ID { get; set; }
        }


        

        [HttpGet("get/tiered")]
        public IActionResult FindTiered()
        {
            var data = DB.GetCollection<Page>().Find(a => string.IsNullOrEmpty(a.ParentId)).ToList().OrderBy(x => x.Index);
            var tdata = new List<TieredPageManagement>();
            foreach (var d in data)
            {
                var t = TieredPageManagement.FromPageManagement(d);
                t.Level = 0;
                tdata.Add(t);
                FindChildren(tdata, t.Id, 1);
            }

            return ApiResult<IEnumerable<TieredPageManagement>>.Ok(tdata);
        }

        protected void FindChildren(List<TieredPageManagement> collection, string parentId, int level)
        {
            var data = DB.GetCollection<Page>().Find(a => a.ParentId == parentId).ToList().OrderBy(x => x.Index);

            foreach (var d in data)
            {
                var t = TieredPageManagement.FromPageManagement(d);
                t.Level = level;
                collection.Add(t);
                FindChildren(collection, t.Id, level + 1);
            }
        }
    }
}
