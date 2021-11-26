using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class OcelotConfiguration
    {
        public string DownstreamPathTemplate { set; get; }
        public string DownstreamScheme { set; get; }
        public List<OcelotHostAndPort> DownstreamHostAndPorts { set; get; } = new List<OcelotHostAndPort>();
        public bool DangerousAcceptAnyServerCertificateValidator { set; get; }
        public string UpstreamPathTemplate { set; get; }
    }

    public class OcelotHostAndPort { 
        public string Host { set; get; }
        public int Port { set; get; }
    }
}
