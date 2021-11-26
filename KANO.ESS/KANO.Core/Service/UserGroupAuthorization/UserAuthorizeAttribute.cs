using KANO.Core.Lib.Auth;
using KANO.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KANO.Core
{
    public class PageCodeAuthorizeAttribute : TypeFilterAttribute
    {
        public string PageCode { get; set; }
        public string SpecialAction { get; set; }
        public ActionGrant? ActionGrant { get; set; }

        public PageCodeAuthorizeAttribute(string pageCode, string specialAction) : base(typeof(PageCodeRequirementFilter))
        {
            PageCode = PageCode;
            SpecialAction = specialAction;
            Arguments = new object[] { pageCode + ":" + specialAction };
        }
        public PageCodeAuthorizeAttribute(string pageCode, ActionGrant action) : base(typeof(PageCodeRequirementFilter))
        {
            PageCode = PageCode;
            ActionGrant = action;
            Arguments = new object[] { pageCode + "." + action.ToString() };
        }
    }

    public class PageCodeRequirementFilter : IAuthorizationFilter
    {
        readonly string _param;

        public PageCodeRequirementFilter(string param)
        {
            _param = param;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var jsonConfiguration = context.HttpContext.Session.GetString(CustomClaimTypes.UserPageAction);
            if(string.IsNullOrWhiteSpace(jsonConfiguration)){
                context.Result = new ForbidResult();
                return;
            }
            var configuration = JsonConvert.DeserializeObject<List<string>>(jsonConfiguration);
            if(configuration.Find(x=>x == _param) == null){
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
