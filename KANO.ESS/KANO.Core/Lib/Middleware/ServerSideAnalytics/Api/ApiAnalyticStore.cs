using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Threading.Tasks;
using AutoMapper;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;


namespace KANO.Core.Lib.Middleware.ServerSideAnalytics.Api
{
    public class ApiAnalyticStore : IAnalyticStore
    {
        private static readonly IMapper Mapper;

        private readonly IConfiguration _configuration;

        static ApiAnalyticStore()
        {
             var config = new MapperConfiguration(cfg =>
             {
                 cfg.CreateMap<WebRequest, ApiWebRequest>()
                     .ForMember(dest => dest.RemoteIpAddress, x => x.MapFrom(req => req.RemoteIpAddress.ToString()));

                 cfg.CreateMap<ApiWebRequest, WebRequest>()
                     .ForMember(dest => dest.RemoteIpAddress, x => x.MapFrom(req => IPAddress.Parse(req.RemoteIpAddress)));
             });

            Mapper = config.CreateMapper();
        }

        public ApiAnalyticStore(IConfiguration configuration)
        {
            _configuration = configuration;
        }       

        public Task StoreWebRequestAsync(WebRequest webRequest)
        {            
            var client = new Client(_configuration);
            var request = new Request($"api/common/logger", Method.POST);
            request.AddJsonParameter(Mapper.Map<ApiWebRequest>(webRequest));
            var response = client.Execute(request);
            
            return Task.FromResult<HttpStatusCode>(response.StatusCode);            
        }

        public Task<long> CountUniqueIdentitiesAsync(DateTime day)
        {
            var from = day.Date;
            var to = day + TimeSpan.FromDays(1);
            return CountUniqueIdentitiesAsync(from, to);
        }

        public async Task<long> CountUniqueIdentitiesAsync(DateTime from, DateTime to)
        {
            return (await UniqueIdentitiesAsync(from, to)).LongCount();
        }

        public Task<IEnumerable<string>> UniqueIdentitiesAsync(DateTime day)
        {
            var from = day.Date;
            var to = day + TimeSpan.FromDays(1);
            return UniqueIdentitiesAsync(from, to);
        }

        public async Task<IEnumerable<string>> UniqueIdentitiesAsync(DateTime @from, DateTime to)
        {
            var result = new List<string>();
            var client = new Client(_configuration);
            var request = new Request($"api/common/logger/uniqueIdentities", Method.POST);
            request.AddJsonParameter(new DateRange { 
                Start=from,
                Finish=to,
            });
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK) {
                result = JsonConvert.DeserializeObject<ApiResult<List<string>>.Result>(response.Content).Data;
            }
            
            return result.AsEnumerable();
        }

        public Task<long> CountAsync(DateTime from, DateTime to)
        {
            long result = 0;
            var client = new Client(_configuration);
            var request = new Request($"api/common/logger/count", Method.POST);
            request.AddJsonParameter(new DateRange
            {
                Start = from,
                Finish = to,
            });
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ApiResult<long>.Result>(response.Content).Data;
            }

            return Task.FromResult<long>(result);
        }

        public Task<IEnumerable<IPAddress>> IpAddressesAsync(DateTime day)
        {
            var from = day.Date;
            var to = day + TimeSpan.FromDays(1);
            return IpAddressesAsync(from, to);
        }

        public async Task<IEnumerable<IPAddress>> IpAddressesAsync(DateTime from, DateTime to)
        {
            var result = new List<IPAddress>();
            var client = new Client(_configuration);
            var request = new Request($"api/common/logger/ipAddresses", Method.POST);
            request.AddJsonParameter(new DateRange
            {
                Start = from,
                Finish = to,
            });
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ApiResult<List<IPAddress>>.Result>(response.Content).Data;
            }

            return result;         
        }

        public async Task<IEnumerable<WebRequest>> RequestByIdentityAsync(string identity)
        {
            var result = new List<WebRequest>();
            var client = new Client(_configuration);
            var request = new Request($"api/common/logger/get/{identity}", Method.GET);            
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ApiResult<List<WebRequest>>.Result>(response.Content).Data;
            }

            return result.Select(x=> Mapper.Map<WebRequest>(x));
        }

        public Task PurgeRequestAsync() {
            DeleteResult result = null;
            var client = new Client(_configuration);
            var request = new Request($"api/common/logger/purge", Method.DELETE);
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ApiResult<DeleteResult>.Result>(response.Content).Data;
            }

            return Task.FromResult<DeleteResult>(result);
        } 

        public async Task<IEnumerable<WebRequest>> InTimeRange(DateTime from, DateTime to)
        {
            var result = new List<WebRequest>();
            var client = new Client(_configuration);
            var request = new Request($"api/common/logger/inTimeRange", Method.POST);
            request.AddJsonParameter(new DateRange
            {
                Start = from,
                Finish = to,
            });
            var response = client.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                result = JsonConvert.DeserializeObject<ApiResult<List<WebRequest>>.Result>(response.Content).Data;
            }

            return result.Select(x => Mapper.Map<WebRequest>(x));
        }
    }
}