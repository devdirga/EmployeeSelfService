using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Auth
{
    public class UserChecker
    {
        private IConfiguration Configuration;

        public UserChecker(IConfiguration config) {
            Configuration = config;
        }

        public User Get(string employeeID) {
            var client = new Client(Configuration);
            var request = new Request($"api/employee/user/{employeeID}", Method.GET);
            var response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return null;
            }

            var result = JsonConvert.DeserializeObject<ApiResult<User>.Result>(response.Content);
            if (result.Data == null)
            {
                return null;
            }
            return result.Data;
        }
    }
}
