using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using KANO.Core.Lib;
using KANO.Core.Lib.Helper;
using KANO.Core.Lib.Middleware.ServerSideAnalytics.Api;
using KANO.Core.Lib.Middleware.ServerSideAnalytics.Mongo;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace KANO.Api.Common.Controllers
{
    [Route("api/common/[controller]")]
    [ApiController]
    public class LoggerController : ControllerBase
    {

        private IMapper Mapper;
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private readonly IMongoCollection<MongoWebRequest> RequestCollection;
        private IConfiguration Configuration;

        // Required, this make sure we use Dependency Injection provided by ASP.Core
        public LoggerController(IMongoManager mongo, IConfiguration conf)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = conf;

            RequestCollection = DB.GetCollection<MongoWebRequest>(Tools.GetLogTableName(Configuration));

            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<ApiWebRequest, MongoWebRequest>()
                    .ForMember(dest => dest.RemoteIpAddress, x => x.MapFrom(req => req.RemoteIpAddress.ToString()));

                cfg.CreateMap<MongoWebRequest, ApiWebRequest>()
                    .ForMember(dest => dest.RemoteIpAddress, x => x.MapFrom(req => IPAddress.Parse(req.RemoteIpAddress)));
            });

            Mapper = config.CreateMapper();
        }

        [HttpPost]
        public async Task<IActionResult> Store([FromBody] ApiWebRequest param)
        {
            try
            {
                await DB.GetCollection<MongoWebRequest>(Tools.GetLogTableName(Configuration)).InsertOneAsync(Mapper.Map<MongoWebRequest>(param));
                return ApiResult<object>.Ok("Log inserted successfully");
            }
            catch (Exception e)
            {

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Log insertion error :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpPost("get")]
        public async Task<IActionResult> Get([FromBody] KendoGrid param)
        {
            try
            {
                var filter = KendoMongoBuilder<MongoWebRequest>.BuildFilter(param);
                var sort = KendoMongoBuilder<MongoWebRequest>.BuildSort(param);
                var result = DB.GetCollection<MongoWebRequest>(Tools.GetLogTableName(Configuration))
                    .Find(filter)
                    .Limit(param.Take)
                    .Skip(param.Skip)
                    .Sort(sort)
                    .ToList();

                var total = DB.GetCollection<MongoWebRequest>(Tools.GetLogTableName(Configuration)).CountDocuments(filter);

                return ApiResult<List<MongoWebRequest>>.Ok(result,total);
            }
            catch (Exception e)
            {

                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Fetching log error :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpPost("uniqueIdentities")]
        public async Task<IActionResult> UniqueIdentities([FromBody] DateRange param)
        {
            try
            {
                var identities = await RequestCollection.DistinctAsync(x => x.Identity, x => x.Timestamp >= param.Start && x.Timestamp <= param.Finish);
                return ApiResult<List<string>>.Ok(identities.ToList());
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Fetching unique identities error :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpPost("count")]
        public async Task<IActionResult> Count([FromBody] DateRange param)
        {
            try
            {
                var count = await RequestCollection.CountDocumentsAsync(x => x.Timestamp >= param.Start && x.Timestamp <= param.Finish);
                return ApiResult<long>.Ok(count);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Counting logger error :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("ipAddresses")]
        public async Task<IActionResult> IPAddresses([FromBody] DateRange param)
        {
            try
            {
                var ips = await RequestCollection.DistinctAsync(x => x.RemoteIpAddress, x => x.Timestamp >= param.Start && x.Timestamp <= param.Finish);
                return ApiResult<List<IPAddress>>.Ok(ips.ToEnumerable()
                    .Select(IPAddress.Parse)
                    .ToList());
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Fetching ip addresses error :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpGet("get/{identity}")]
        public async Task<IActionResult> RequestByIdentity(string identity)
        {
            try
            {
                var identities = await RequestCollection.FindAsync(x => x.Identity == identity);
                return ApiResult<List<ApiWebRequest>>.Ok(
                    identities
                        .ToEnumerable()
                        .Select(x => Mapper.Map<ApiWebRequest>(x))
                        .ToList()
                    );
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Fetching request by identity error :\n{Format.ExceptionString(e)}");
            }

        }

        [HttpDelete("purge")]
        public async Task<IActionResult> PurgeRequestAsync()
        {
            try
            {
                var result = await RequestCollection.DeleteManyAsync(x => true);
                return ApiResult<DeleteResult>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Purging data error :\n{Format.ExceptionString(e)}");
            }
        }

        [HttpPost("inTimeRange")]
        public async Task<IActionResult> InTimeRange([FromBody] DateRange param)
        {
            try
            {
                var result = (await (await RequestCollection
                    .FindAsync(x => x.Timestamp >= param.Start && x.Timestamp <= param.Finish)).ToListAsync())
                    .Select(x => Mapper.Map<ApiWebRequest>(x))
                    .ToList();
                return ApiResult<List<ApiWebRequest>>.Ok(result);
            }
            catch (Exception e)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Error fetching data error :\n{Format.ExceptionString(e)}");
            }
        }
    }
}