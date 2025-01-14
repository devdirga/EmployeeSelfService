﻿using System;
using System.Collections.Generic;
using System.Linq;
using KANO.Core.Lib;
using KANO.Core.Model.Auth;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.IO;
using System.Net;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KANO.Api.Administration.Controllers
{
    [Route("api/[controller]")]
    public class AdministrationController : Controller
    {
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase DB;
        private readonly IConfiguration Configuration;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public AdministrationController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();     
            Configuration = conf;      
        }

        [HttpGet("getconfigpassword")]
        public IActionResult GetConfigPassword()
        {
            try { return ApiResult<ConfigPassword>.Ok(DB.GetCollection<ConfigPassword>().Find(a => a.Published == 1).FirstOrDefault());}
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }
        [HttpPost("updateconfigpassword")]
        public IActionResult UpdateConfigPassword([FromForm] FamilyForm param)
        {
            try {
                DB.Save(JsonConvert.DeserializeObject<ConfigPassword>(param.JsonData));
                return ApiResult<object>.Ok("success");
            } catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpGet("getmobileversion")]
        public IActionResult GetMobileVersion()
        {
            try { return ApiResult<List<Mobile>>.Ok(DB.GetCollection<Mobile>().Find(a => a.Version != String.Empty).ToList()); }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message);}
        }

        [HttpPost("updateandroidversion")]
        public IActionResult UpdateAndroidVersion([FromForm] TicketForm param)
        {
            
            try {
                Mobile mobile = JsonConvert.DeserializeObject<Mobile>(param.JsonData);
                mobile.Upload(Configuration, null, param.FileUpload, x => String.Format("ESSMobile{0}{1}", mobile.UpdatedDate, mobile.Version));
                DB.Save(mobile);
                return ApiResult<object>.Ok("success");
            } catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpPost("updateiosversion")]
        public IActionResult UpdateIOSVersion([FromForm] TicketForm param)
        {
            
            try {
                Mobile mobile = JsonConvert.DeserializeObject<Mobile>(param.JsonData);
                mobile.Upload(Configuration, null, param.FileUpload, x => String.Format("ESSMobile{0}{1}", mobile.UpdatedDate, mobile.Version));
                DB.Save(mobile);
                return ApiResult<object>.Ok("success");
            }
            catch (Exception e) { return ApiResult<object>.Error(HttpStatusCode.BadRequest, e.Message); }
        }

        [HttpGet("download/{employeeID}")]
        public IActionResult MDownload(string employeeID)
        {
            
            try {
                Mobile result = DB.GetCollection<Mobile>().Find(a => a.Type == employeeID).FirstOrDefault();
                var bytes = result.Download();
                return File(bytes, "application/force-download", Path.GetFileName(result.Filepath));
            }
            catch (Exception e) { throw e; }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }
    }
}
