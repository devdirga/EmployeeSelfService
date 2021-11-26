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
using KANO.Core.Model.Payroll;
using KANO.Core.Service;
using KANO.Core.Service.AX;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace KANO.Api.Payroll.Controllers
{
    [Route("api/payroll/[controller]")]
    [ApiController]
    public class ConfigLoanController : ControllerBase
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;
        private LoanMailTemplate mailTemplate;
        public ConfigLoanController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;
            mailTemplate = new LoanMailTemplate(Mongo,Configuration);
        }

        [HttpGet("get")]
        public async Task<IActionResult> GetConfig()
        {
            var res = new List<ConfigurationLoan>();

            res = DB.GetCollection<ConfigurationLoan>().Find(_=>true).ToList();
            return ApiResult<List<ConfigurationLoan>>.Ok(res);
        }

        [HttpPost("save")]
        public IActionResult SaveConfig([FromBody] ConfigurationLoan configLoan)
        {
            try
            {
                DB.Save(configLoan);
                
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving configuration template request :\n{e.Message}");
            }
            return ApiResult<object>.Ok("Configuration loan has been saved successfully");
        }

        [HttpPost("savetemplate")]
        public IActionResult SaveTemplate([FromBody] LoanMailTemplate param)
        {
            try
            {
                param.Id = "LoanTemplate";
                DB.Save(param);
                return ApiResult<object>.Ok("Configuration template has been saved successfully");
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error saving configuration template request :\n{e.Message}");
            }
            
        }

        [HttpGet("gettemplate")]
        public async Task<IActionResult> GetTemplate()
        {
            mailTemplate = DB.GetCollection<LoanMailTemplate>().Find(x => x.Id == "LoanTemplate").FirstOrDefault();
            return ApiResult<LoanMailTemplate>.Ok(mailTemplate);
        }
    }
}