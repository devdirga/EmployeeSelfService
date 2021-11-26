using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;

namespace KANO.Api.Agenda.Controllers
{
    [Route("api/timeManagement/[controller]")]
    [ApiController]
    public class AgendaController : ControllerBase
    {

        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private Core.Model.Agenda _agenda;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public AgendaController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            _agenda = new Core.Model.Agenda(DB, Configuration);
        }
        
        [HttpPost("get/range")]
        public async Task<IActionResult> GetRange([FromBody]GridDateRange param)
        {
            try
            {
                var result = _agenda.GetS(param.Username, param.Range);
                return ApiResult<List<Core.Model.Agenda>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to get agenda '' :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpGet("download/{token}")]
        public IActionResult Download(string token)
        {
            // Download the data
            Console.WriteLine($"{DateTime.Now} >>> {token}");
            try
            {
                var file = new FieldAttachment();
                var decodedToken = WebUtility.UrlDecode(token);
                file.Filepath = Hasher.Decrypt(decodedToken);
                Console.WriteLine($"{DateTime.Now} >>> {file.Filepath}");
                var bytes = file.Download();
                return File(bytes, "application/force-download", file.Filename);
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now} >>> {Format.ExceptionString(e, true)}");
                throw e;
            }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }

    }
}