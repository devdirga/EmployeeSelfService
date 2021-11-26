using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Extension
{
    public class Request
    {
        public RestRequest Self { get; set; }
        public MultipartFormDataContent FormData { get; }  = new MultipartFormDataContent();
        public string EndPoint { get; set; }

        public Request(string destination, Method method)
        {
            EndPoint = destination;
            Self = new RestRequest(destination, method);
        }

        public void AddFormDataParameter(string key, string value) {
            if (!string.IsNullOrWhiteSpace(value)) {
                FormData.Add(new StringContent(value), key);
            }            
        }

        public void AddFormDataFile(string key, IFormFile file)
        {
            var fileContent = new StreamContent(file.OpenReadStream())
            {
                Headers ={
                    ContentLength = file.Length,
                    ContentType = new MediaTypeHeaderValue(file.ContentType)
                }
            };
            
            FormData.Add(fileContent, key, file.FileName);
        }

        public void AddJsonParameter(object param)
        {
            if (!this.Self.AlwaysMultipartFormData)
            {
                Self.AddHeader("Accept", "application/json");
                Self.Parameters.Clear();
                Self.AddParameter("application/json; charset=utf-8", JsonConvert.SerializeObject(param), ParameterType.RequestBody);
            }
        }        

        public void AddQueryParameter(string key, string value)
        {
            Self.AddHeader("Content-Type", "text/html");
            if (!string.IsNullOrWhiteSpace(value))
            {
                Self.AddQueryParameter(key, value);
            }
        }
    }

}
