using KANO.Core.Lib.Auth.BasicAuthentication.Context;
using Microsoft.AspNetCore.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Auth.BasicAuthentication
{
    public class BasicAuthenticationOptions : AuthenticationSchemeOptions
    {
        public string Realm { get; set; }

        public bool IncludeErrorDetails { get; set; }
        public new BasicAuthenticationEvents Events
        {
            get { return (BasicAuthenticationEvents)base.Events; }
            set { base.Events = value; }
        }
        public string Challenge { get; set; } = BasicAuthenticationDefaults.AuthenticationScheme;
    }
}
