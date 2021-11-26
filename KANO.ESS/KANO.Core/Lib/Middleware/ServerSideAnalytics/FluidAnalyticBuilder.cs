using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;

namespace KANO.Core.Lib.Middleware.ServerSideAnalytics
{
    public class FluidAnalyticBuilder
    {
        private readonly IAnalyticStore _store;
        private IGeoIpResolver _geoIp;
        private List<Func<HttpContext, bool>> _exclude;

        internal FluidAnalyticBuilder(IAnalyticStore store)
        {
            _store = store;
        }

        internal async Task Run(HttpContext context, Func<Task> next)
        {
            var identity = context.UserIdentity();
            string request = "";
            context.Request.EnableBuffering();
            using (var bodyReader = new StreamReader(context.Request.Body, 
                encoding: Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 1024 * 1000,
                leaveOpen: true))
            {
                var bodyAsText = bodyReader.ReadToEnd();                                               
                if (string.IsNullOrWhiteSpace(bodyAsText) == false)
                {
                    request= bodyAsText;
                }                    

                context.Request.Body.Position = 0;
            }
                
                
            //Copy a pointer to the original response body stream            
            var originalBodyStream = context.Response.Body;

            //Create a new memory stream...
            using (var responseBody = new MemoryStream())
            {
                //...and use that for the temporary response body
                context.Response.Body = responseBody;

                //Pass the command to the next task in the pipeline
                await next.Invoke();

                //Format the response from the server
                var response = await formatResponse(context.Response);

                //Copy the contents of the new memory stream (which contains the response) to the original stream, which is then returned to the client.
                await responseBody.CopyToAsync(originalBodyStream);

                //This request should be filtered out ?
                if (_exclude?.Any(x => x(context)) ?? false)
                {
                    return;
                }

                //Let's build our structure with collected data
                var req = new WebRequest
                {
                    Timestamp = DateTime.Now,
                    Identity = identity,
                    RemoteIpAddress = context.Connection.RemoteIpAddress,
                    Method = context.Request.Method,
                    UserAgent = context.Request.Headers["User-Agent"],
                    Path = context.Request.Path.Value,
                    IsWebSocket = context.WebSockets.IsWebSocketRequest,
                    StatusCode = context.Response.StatusCode,
                    Response = response,
                    Request = request,

                    //Ask the store to resolve the geo code of gived ip address 
                    CountryCode = _geoIp != null
                                   ? await _geoIp.ResolveCountryCodeAsync(context.Connection.RemoteIpAddress)
                                   : CountryCode.World

                };

                //Store the request into the store
                await _store.StoreWebRequestAsync(req);                
            }
        }

        private async Task<string> formatRequest(HttpRequest request)
        {
            var body = request.Body;

            //This line allows us to set the reader for the request back at the beginning of its stream.
            request.EnableRewind();

            //We now need to read the request stream.  First, we create a new byte[] with the same length as the request stream...
            var buffer = new byte[Convert.ToInt32(request.ContentLength)];

            //...Then we copy the entire request stream into the new buffer.
            await request.Body.ReadAsync(buffer, 0, buffer.Length);

            //We convert the byte[] into a string using UTF8 encoding...
            var bodyAsText = Encoding.UTF8.GetString(buffer);

            //..and finally, assign the read body back to the request body, which is allowed because of EnableRewind()
            request.Body = body;

            return bodyAsText;
        }

        private async Task<string> formatResponse(HttpResponse response)
        {
            //We need to read the response stream from the beginning...
            response.Body.Seek(0, SeekOrigin.Begin);

            //...and copy it into a string
            string text = await new StreamReader(response.Body).ReadToEndAsync();

            //We need to reset the reader for the response so that the client can read it.
            response.Body.Seek(0, SeekOrigin.Begin);

            //Return the string for the response when status code is not 200
            return text;
        }

        public FluidAnalyticBuilder UseGeoIpResolver(IGeoIpResolver geoIp)
        {
            _geoIp = geoIp;
            return this;
        }

        public FluidAnalyticBuilder Exclude(Func<HttpContext, bool> filter)
        {
            if(_exclude == null) _exclude = new List<Func<HttpContext, bool>>();
            _exclude.Add(filter);
            return this;
        }

        public FluidAnalyticBuilder Exclude(IPAddress ip) => Exclude(x => Equals(x.Connection.RemoteIpAddress, ip));

        public FluidAnalyticBuilder LimitToPath(string path) => Exclude(x => Equals(x.Request.Path.StartsWithSegments(path)));

        public FluidAnalyticBuilder LimitToPath(string[] paths) {
            return  Exclude(x => !paths.Any(path => x.Request.Path.StartsWithSegments(path)));
        }

        public FluidAnalyticBuilder ExcludePath(params string[] paths)
        {
            return Exclude(x => paths.Any(path => x.Request.Path.StartsWithSegments(path)));
        }

        public FluidAnalyticBuilder ExcludeExtension(params string[] extensions)
        {
            return Exclude(x => extensions.Any(ext => x.Request.Path.Value.EndsWith(ext)));
        }

        public FluidAnalyticBuilder ExcludeLoopBack() => Exclude(x => IPAddress.IsLoopback(x.Connection.RemoteIpAddress));

        public FluidAnalyticBuilder ExcludeIp(IPAddress address) => Exclude(x => x.Connection.RemoteIpAddress.Equals(address));

        public FluidAnalyticBuilder ExcludeStatusCodes(params HttpStatusCode[] codes) => Exclude(context => codes.Contains((HttpStatusCode)context.Response.StatusCode));

        public FluidAnalyticBuilder ExcludeStatusCodes(params int[] codes) => Exclude(context => codes.Contains(context.Response.StatusCode));

        public FluidAnalyticBuilder LimitToStatusCodes(params HttpStatusCode[] codes) => Exclude(context => !codes.Contains((HttpStatusCode)context.Response.StatusCode));

        public FluidAnalyticBuilder LimitToStatusCodes(params int[] codes) => Exclude(context => !codes.Contains(context.Response.StatusCode));
    }
}