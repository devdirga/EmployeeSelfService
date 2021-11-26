using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;

namespace KANO.Core.Service.AX
{
    public class Credential
    {
        public string Domain { set; get; }
        public string Host{set;get;}
        public int Port{set;get;}
        public string Username{set;get;}
        public string Password{set;get;}
        public string Company{set;get;}
        public string UserPrincipalName{set;get;}
    }
}
