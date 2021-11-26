using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using KANO.Api.Auth.Service;
using KANO.Core.Lib;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace KANO.Api.Auth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : Controller
    {

        private IMongoManager Mongo;
        private IMongoDatabase Db;
        private IConfiguration Configuration;

        public AuthController(IMongoManager mongo, IConfiguration config)
        {
            Mongo = mongo;
            Db = Mongo.Database();
            Configuration = config;

            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
        }

        // POST api/auth
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginParam param)
        {
            try
            {
                var authService = new AuthService(Mongo, Configuration);
                var authResult = authService.Auth(param.EmployeeID, param.Email, param.Password);

                return ApiResult<AuthResult>.Ok(authResult);
            }
            catch (Exception e)
            {

                return ApiResult<AuthResult>.Error(e);
            }
            
        }

        // POST api/auth/activate
        [HttpPost]
        [Route("activate")]
        public async Task<IActionResult> Activate([FromBody] ActivationParam param)
        {
            var authService = new AuthService(Mongo, Configuration);
            var activationResult = authService.Activate(param.Token, param.Password);

            return ApiResult<AuthResult>.Ok(activationResult);
        }

        [HttpPost]
        [Route("activation/request")]
        public async Task<IActionResult> SendMailActivation([FromBody] SendActivationEmailParam param)
        {
            var authService = new AuthService(Mongo, Configuration);
            var sendEmailResult = authService.SendMailActivationUser(param.EmployeeID, param.Email, param.BaseURL);
            return ApiResult<AuthResult>.Ok(sendEmailResult);
        }

        /*[HttpPost]
        [Route("activation/request")]
        public async Task<IActionResult> SendMailActivation([FromBody] SendActivationEmailParam param)
        {
            var authService = new AuthService(Mongo, Configuration);
            var sendEmailResult = authService.SendMailActivationUser(param.EmployeeID, param.Email);
            return ApiResult<AuthResult>.Ok();
        }*/

        [Route("resetPassword/request")]
        public async Task<IActionResult> SendResetPasswordEmail([FromBody] SendResetPasswordEmailParam param)
        {
            var authService = new AuthService(Mongo, Configuration);
            var result = authService.SendResetPassword(param.Email);
            return ApiResult<AuthResult>.Ok(result);
        }

        [HttpGet("resetPassword/verifyToken/{token}")]
        public async Task<IActionResult> VerifyResetPasswordToken(string token)
        {
            var authService = new AuthService(Mongo, Configuration);
            // var result = authService.VerifyResetPasswordToken(token);
            var result = authService.VerifyResetPasswordKey(token);
            return ApiResult<AuthResult>.Ok(result);
        }

        [HttpPost]
        [Route("resetPassword")]
        public async Task<IActionResult> ResetPassword([FromBody] SendResetPasswordEmailParam param)
        {
            var authService = new AuthService(Mongo, Configuration);
            var result = authService.ResetPassword(param.Token, param.Password);
            return ApiResult<AuthResult>.Ok(result);
        }

        [HttpPost]
        [Route("changePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordParam param)
        {
            var authService = new AuthService(Mongo, Configuration);
            var result = authService.ChangePassword(param.EmployeeID,param.Password, param.NewPassword);
            return ApiResult<AuthResult>.Ok(result);
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return ApiResult.Ok(Tools.ConfigChecksum(Configuration), "success");
        }
    }
}
