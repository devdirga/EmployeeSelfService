using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace KANO.Api.Common.Controllers
{
    [Route("api/[controller]")]
    public class CommonController : Controller
    {

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;        

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public CommonController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
        }
                
        [HttpPost("encrypt")]
        public IActionResult Encrypt([FromBody]Param param)
        {
            if(string.IsNullOrWhiteSpace(param.Data)){
                return ApiResult.Error(new Exception("Parameter Data is empty"));    
            }

            var user = new User();
            user.NewPassword = param.Data;
            param.Result = user.PasswordHash;
            
            return ApiResult.Ok(param, "success");
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }

        public class Param{
            public string Data {set; get;}
            public string Result {set; get;}
        }
    }
    
}
