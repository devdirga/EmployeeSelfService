using Flurl;
using KANO.Core.Lib.Helper;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Extension
{
    public class Client
    {
        private const string DEFAULT_PATH_KEY = "Request";
        public string BaseURL;
        public RestClient Self;

        public IConfiguration Configuration { get; }

        public Client(IConfiguration config) {
            Self = init(config, "");
        }

        public Client(string baseURL)
        {
            Self = new RestClient(baseURL);
            Self.AddDefaultHeader("Content-Type", "application/json");
            BaseURL = baseURL;
        }

        public Client(IConfiguration config, string pathKey) {
            Self = init(config, pathKey);
        }        

        public IRestResponse Execute(Request request) {
            return Self.Execute(request.Self);
        }

        public IRestResponse Download(Request request)
        {
            return Self.Execute(request.Self);
        }

        public async Task<UploadResult> Upload(Request request)
        {
            using (var client = new HttpClient(handler: new HttpClientHandler(), disposeHandler: false))
            {                
                using (var response =  await client.PostAsync(Url.Combine(BaseURL, request.EndPoint), request.FormData))
                {
                    return new UploadResult {
                        Content = await response.Content.ReadAsStringAsync(),
                        ResponseMessage = response,
                    };
                }
            }
        }

        private RestClient init(IConfiguration config, string pathKey) {
            if (!string.IsNullOrWhiteSpace(pathKey))
            {
                pathKey += ":";
            }
            else 
            {
                pathKey = DEFAULT_PATH_KEY + ":";
            }

            var baseUrl = config[pathKey + "GatewayUrl"];
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new Exception(pathKey + "GatewayUrl is not set in the configuration!");
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            

            var uname = config[pathKey + "Username"];
            if (string.IsNullOrWhiteSpace(uname))
                throw new Exception(pathKey + "Username is not set in the configuration!");

            var pwd = config[pathKey + "Password"];
            if (string.IsNullOrWhiteSpace(pwd))
                throw new Exception(pathKey + "Password is not set in the configuration!");

            var c = new RestClient(baseUrl);
            c.Authenticator = new HttpBasicAuthenticator(uname, pwd);
            c.AddDefaultHeader("Content-Type", "application/json");
            BaseURL = baseUrl;
            return c;
        }
       
    }

    public class UploadResult { 
        public string Content { set; get; }
        public HttpResponseMessage ResponseMessage { set; get; }
    }
}
