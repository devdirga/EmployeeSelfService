using System;
using System.Net;

namespace KANO.Core.Lib.Middleware.ServerSideAnalytics
{
    public class WebRequest
    {
        public DateTime Timestamp { get; set; }

        public string Identity { get; set; }

        public IPAddress RemoteIpAddress { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string UserAgent { get; set; }

        public string Referer { get; set; }

        public bool IsWebSocket { get; set; }

        public int StatusCode { get; set; }

        public string Response { get; set; }
        
        public string Request { get; set; }

        public CountryCode CountryCode { get; set; }

    }
}