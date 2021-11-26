using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public class HelloSignProvider
    {
        public string APIKey { get; }
        public HelloSignProvider(string apikey)
        {
            APIKey = apikey;
        }

        public async Task<HttpFileResult> GetDocument(string signID)
        {
            return await GetDocument(APIKey, signID);
        }

        public static async Task<HttpFileResult> GetDocument(string apikey, string signID)
        {
            var uri = $"https://api.hellosign.com/v3/signature_request/files/{signID}";

            var client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(apikey + ":");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            var res = await client.GetAsync(uri);
            return new HttpFileResult(res);
        }
    }

    public class HttpFileResult
    {
        public string Filename { get; }
        public HttpResponseMessage Response { get; }
        public async Task<Stream> GetStreamAsync()
        {
            return await Response.Content.ReadAsStreamAsync();
        }
        public async Task<byte[]> GetBufferAsync()
        {
            return await Response.Content.ReadAsByteArrayAsync();
        }

        public async void SaveToFile(string path)
        {
            var strm = await GetStreamAsync();
            var dir = Path.GetFileName(path);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (File.Exists(path)) File.Delete(path);
            using(var fo = File.Create(path))
            {
                strm.Seek(0, SeekOrigin.Begin);
                strm.CopyTo(fo);
                fo.Flush();
            }
        }

        public HttpFileResult(HttpResponseMessage httpResponse)
        {
            Response = httpResponse;
            Filename = null;

            if (Response.StatusCode == HttpStatusCode.OK)
            {
                if (Response.Content.Headers.ContentDisposition.DispositionType == "attachment")
                {
                    Filename = Response.Content.Headers.ContentDisposition.FileName;
                }
            }
        }
    }
}
