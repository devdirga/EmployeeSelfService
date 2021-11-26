using KANO.Core.Lib.Auth;
using KANO.Core.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using static KANO.Core.Lib.Auth.DirectoryActiveUser;
using System.DirectoryServices.AccountManagement;
using Microsoft.Extensions.Configuration;

namespace KANO.Core.Lib.Middleware
{
    public class UnauthorizedWindowsIdentity : WindowsIdentity
    {
        public UnauthorizedWindowsIdentity(WindowsIdentity ntidentity) : base(ntidentity.Token) { }
        public override bool IsAuthenticated => false;
    }

    public class ADWindowsIdenitty : WindowsIdentity
    {
        private UserPrincipal _userPr;
        public ADWindowsIdenitty(WindowsIdentity ntidentity, UserPrincipal userPr) : base(ntidentity.Token){ _userPr = userPr; }
        public override bool IsAuthenticated => true;
        public override string Name => _userPr.UserPrincipalName;

        public string DisplayName => _userPr.DisplayName;
        public string Email => _userPr.EmailAddress;
        // upn, but loweredcase for comparison
        public string Id => _userPr.UserPrincipalName.ToLowerInvariant();

        // real UPN
        public string UserPrincipalName => _userPr.UserPrincipalName;

    }

    public class IdentityMiddlewareBuilder : IMiddleware
    {
        private readonly IConfiguration _conf;
        private readonly IMongoManager _mgr;
        private readonly string _authType;

        public IdentityMiddlewareBuilder(IConfiguration conf)
        {
            _authType = conf["Auth:Mode"];
            _conf = conf;
        }

        public IdentityMiddlewareBuilder(IMongoManager mgr, IConfiguration conf)
        {
            _mgr = mgr;
            _authType = conf["Auth:Mode"];
            _conf = conf;
        }

        public async Task InvokeAsync(HttpContext ctx, RequestDelegate next)
        {            
            var isauth = ctx.User?.Identity?.IsAuthenticated;

            // not authenticated, continue process
            if (isauth == false)
            {
                await next(ctx);
                return;
            }

            // authenticate, pull additional data from db or active directory
            // avoid checking by User context AuthenticateType because bug on windows security principal
            // https://github.com/aspnet/IISIntegration/issues/231
            switch (_authType)
            {
                case "Cookie":
                    bool foundByCookie = false;
                    if (_mgr != null)
                    {
                        foundByCookie = AuthPrincipal.AddClaim(ctx.User, _mgr.Database());
                    }
                    else {
                        foundByCookie = AuthPrincipal.CheckClaim(ctx.User, _conf);
                    }
                    
                    if (!foundByCookie)
                    {
                        await ctx.SignOutAsync();
                    }
                    break;

                case "Windows":
                    var wi = ctx.User.Identity as WindowsIdentity;
                    if (wi == null)
                    {
                        throw new Exception("Invalid identity");
                    }

                    var userpr = FindByNt(wi);
                    ctx.User = new ClaimsPrincipal(new ADWindowsIdenitty(wi, userpr));

                    bool foundByNt = false;
                    if (_mgr != null)
                    {
                        foundByNt = AuthPrincipal.AddClaimNt(ctx.User, _mgr.Database());
                    }
                    else {
                        foundByNt = AuthPrincipal.AddClaimNt(ctx.User, _conf);
                    }
                    
                    if (!foundByNt)
                    {
                        var ntid = ctx.User.Identity as WindowsIdentity;
                        if (ntid == null)
                            break;

                        ctx.User = new ClaimsPrincipal(new UnauthorizedWindowsIdentity(ntid));

                        var claims = new Claim[] {
                                new Claim(CustomClaimTypes.AuthType, "NTLM", null, AuthPrincipal.Issuer),
                            };
                        ctx.User.AddIdentity(new ClaimsIdentity(claims));
                    }
                    break;

                default:
                    throw new Exception("Unsupported Auth Mode");
            }

            await next(ctx);
        }
    }
}
