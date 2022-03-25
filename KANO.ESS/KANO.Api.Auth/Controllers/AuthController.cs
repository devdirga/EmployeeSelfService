using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KANO.Api.Auth.Service;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace KANO.Api.Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly IMongoManager Mongo;
        private readonly IMongoDatabase Db;
        private readonly IConfiguration Configuration;

        public AuthController(IMongoManager mongo, IConfiguration config)
        {
            Mongo = mongo;
            Db = Mongo.Database();
            Configuration = config;
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        [HttpPost]
        public IActionResult Login([FromBody] LoginParam p)
        {
            try {
                var authResult = new AuthService(Mongo, Configuration).Auth(p.EmployeeID, p.Email, p.Password);
                return ApiResult<AuthResult>.Ok(authResult);
            }
            catch (Exception e) { return ApiResult<AuthResult>.Error(e); }
        }

        [HttpPost]
        [Route("activate")]
        public IActionResult Activate([FromBody] ActivationParam p)
        {
            var activationResult = new AuthService(Mongo, Configuration).Activate(p.Token, p.Password);
            return ApiResult<AuthResult>.Ok(activationResult);
        }

        [HttpPost]
        [Route("activation/request")]
        public IActionResult SendMailActivation([FromBody] SendActivationEmailParam p)
        {
            var sendEmailResult = new AuthService(Mongo, Configuration).SendMailActivationUser(p.EmployeeID, p.Email, p.BaseURL);
            return ApiResult<AuthResult>.Ok(sendEmailResult);
        }

        [Route("resetPassword/request")]
        public IActionResult SendResetPasswordEmail([FromBody] SendResetPasswordEmailParam p)
        {
            var result = new AuthService(Mongo, Configuration).SendResetPassword(p.Email);
            return ApiResult<AuthResult>.Ok(result);
        }

        [HttpGet("resetPassword/verifyToken/{token}")]
        public IActionResult VerifyResetPasswordToken(string token)
        {
            var result = new AuthService(Mongo, Configuration).VerifyResetPasswordKey(token);
            return ApiResult<AuthResult>.Ok(result);
        }

        [HttpPost]
        [Route("resetPassword")]
        public IActionResult ResetPassword([FromBody] SendResetPasswordEmailParam p)
        {
            var result = new AuthService(Mongo, Configuration).ResetPassword(p.Token, p.Password);
            return ApiResult<AuthResult>.Ok(result);
        }

        [HttpPost]
        [Route("changePassword")]
        public IActionResult ChangePassword([FromBody] ChangePasswordParam p)
        {
            var result = new AuthService(Mongo, Configuration).ChangePassword(p.EmployeeID, p.Password, p.NewPassword);
            return ApiResult<AuthResult>.Ok(result);
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [HttpPost]
        [Route("signin")]
        public IActionResult SignIn([FromBody] LoginParam param)
        {
            try
            {
                AuthResult authResult = new AuthService(Mongo, Configuration).Auth(param.EmployeeID, param.Email, param.Password);
                if (authResult.AuthState == AuthState.Authenticated)
                {
                    var claims = new[]{
                        new Claim(JwtRegisteredClaimNames.UniqueName, authResult.User.Id ),
                        new Claim(JwtRegisteredClaimNames.Sub, authResult.User.Username),
                        new Claim(JwtRegisteredClaimNames.Jti, authResult.User.FullName),
                    };
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
                    var expire_minutes = Convert.ToDouble(Configuration["Tokens:ExpireMinutes"]);
                    var token = new JwtSecurityToken(Configuration["Tokens:Issuer"], Configuration["Tokens:Audience"],
                                claims,
                                expires: DateTime.Now.AddMinutes(expire_minutes),
                                signingCredentials: creds);
                    authResult.Data = new JwtSecurityTokenHandler().WriteToken(token);
                    return ApiResult<AuthResult>.Ok(authResult);
                }
                else
                {
                    return ApiResult<AuthResult>.Error(HttpStatusCode.Unauthorized, authResult.Message);
                }

            }
            catch (Exception e) { return ApiResult<AuthResult>.Error(e); }
        }

        [Authorize]
        [HttpPost("mchangepassword")]
        public IActionResult MChangePassword([FromBody] ChangePasswordParam param)
        {
            AuthResult auth = new AuthService
                (Mongo, Configuration).ChangePassword(param.EmployeeID, param.Password, param.NewPassword);
            if (auth.Success == true)
            {
                return ApiResult<AuthResult>.Ok(auth);
            }
            return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, auth.Message);
        }

        [Authorize]
        [Route("mresetpassword/request")]
        public IActionResult MSendResetPasswordEmail([FromBody] SendResetPasswordEmailParam param)
        {
            return ApiResult<AuthResult>.Ok(new AuthService
                (Mongo, Configuration).SendResetPassword(param.Email));
        }

        [Authorize]
        [HttpGet("mismustchangepassword/{token}")]
        public IActionResult MIsMustChangePassword(string token)
        {
            try
            {
                DateTime nw = DateTime.Now;
                DateTime lastChangePassword = Db.GetCollection<User>().Find(a => a.Username == token).FirstOrDefault().LastPasswordChangedDate;
                int tracehold = Db.GetCollection<Core.Model.Auth.ConfigPassword>().Find(a => a.Published == 1).FirstOrDefault().MustChangeDays;
                Double diffdays = (nw - lastChangePassword).TotalDays;
                return ApiResult<AuthResult>.Ok((diffdays >= tracehold) ? "true" : "false");
            }
            catch (Exception e) { return ApiResult<AuthResult>.Error(e); }
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }
    }
}
