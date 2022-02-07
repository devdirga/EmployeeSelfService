using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;
using KANO.Core.Lib.Extension;
using KANO.Core.Lib;
using RestSharp;
using Newtonsoft.Json;
using KANO.Core.Model;
using KANO.Core.Service;
using KANO.Core.Lib.Helper;
using KANO.Core.Lib.Auth;
using Microsoft.AspNetCore.Hosting;
using System.Drawing;
using System.IO;
using MongoDB.Driver;
using System.Linq;
using System.Collections.Generic;
using KANO.Core.Service.Odoo;
using Microsoft.Extensions.Primitives;
using KANO.Core.Lib.Response;

namespace KANO.ESS.Areas.Site
{
    [Area("Site")]
    public class AuthController : Controller
    {
        private string AuthType;
        private IConfiguration Configuration;
        private IUserSession Session;
        private IHostingEnvironment Env;
        private readonly String Api = "api/auth/";
        private readonly String BearerAuth = "Bearer ";
        public AuthController(IConfiguration config, IUserSession session, IHostingEnvironment env)
        {
            Configuration = config;
            Session = session;
            Env = env;
        }

        [AllowAnonymous]
        public IActionResult Index()
        {
            var loggedin = User.Identity?.IsAuthenticated;
            if (loggedin == true)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "ESS" });
            }

            return RedirectToAction("Login", "Auth", new { area = "Site" });
        }

        public async Task<IActionResult> DoLogout()
        {
            HttpContext.Session.SetString(CustomClaimTypes.UserPageAction, "");
            HttpContext.Session.SetString("ref", "");
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Auth", new { area = "Site" });
        }

        [AllowAnonymous]
        public IActionResult NotSupported()
        {
            ViewBag.Title = "Not Supported";
            return View();
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            var loggedin = User.Identity?.IsAuthenticated;
            if (loggedin == true)
            {
                var pageRef = HttpContext.Session.GetString("ref") ?? "";
                if (string.IsNullOrWhiteSpace(pageRef))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "ESS" });
                }
                return Redirect(pageRef);
            }

            if (loggedin == false && AuthType == "Windows")
            {
                return View("UnauthorizedWindows");
            }

            ViewBag.Title = "Login";
            return View();
        }

        public bool AcceptAllCertifications(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certification, System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DoLogin([FromBody] LoginParam param)
        {
            HttpContext.Session.SetString(CustomClaimTypes.UserPageAction, "");
            var pageRef = HttpContext.Session.GetString("ref") ?? "";
            try
            {
                IRestResponse authResponse = null;
				var odooSessionID = "";
                var tasks = new List<Task<TaskRequest<Exception>>>();

                // ESS Auth
                tasks.Add(Task.Run(() =>
                {
                    Exception error = null;
                    try
                    {
                        var client = new Client(Configuration);
                        var request = new Request("/api/auth", Method.POST);
                        request.AddJsonParameter(param);
                        authResponse = client.Execute(request);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    return TaskRequest<Exception>.Create("ess_auth", error);
                }));

				// Odoo Auth
                tasks.Add(Task.Run(() =>
                {
                    Exception error = null;
                    try
                    {
                        odooSessionID= OdooService.Authenticate(Configuration);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    return TaskRequest<Exception>.Create("odoo_auth", error);
                }));

                var t = Task.WhenAll(tasks);
                try
                {
                    t.Wait();
                }
                catch (Exception e)
                {
                    throw e;
                }

                // Combine result
                if (t.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var r in t.Result)
                    {
                        var e = (Exception)r.Result;
                        if (e != null)
                            throw e;
                    }
                }


                if (authResponse != null && authResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var authResult = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(authResponse.Content).Data;
                    if (!authResult.Success)
                    {
                        return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, authResult.Message);
                    }

                    this.generateEmployeeImage(authResult.User);

                    if (authResult.AuthState == AuthState.Authenticated)
                    {
                        // Generate Cookies
						authResult.User.OdooID = odooSessionID;
                        var claims = AuthPrincipal.GenerateClaims(authResult.User, Configuration);
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                        // Generate Action Configuration in Session
                        var jsonActionConfiguration = AuthPrincipal.GenerateActionConfiguration(authResult.User, Configuration);
                        HttpContext.Session.SetString(CustomClaimTypes.UserPageAction, jsonActionConfiguration);

                        var authProperties = new AuthenticationProperties { };

                        await HttpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            new ClaimsPrincipal(claimsIdentity),
                            authProperties);

                        // Get Landing Page						
                        var redirectTo = pageRef;
                        var userMenu = Session.GetMenu(param.EmployeeID, true);
                        var validMenu = userMenu.ToList().Find(x => x.Url != null && x.Url?.ToLower() == pageRef.ToLower() && x.Url != "#");
                        var isValidMenu = validMenu != null;

                        if (string.IsNullOrWhiteSpace(pageRef) || (!string.IsNullOrWhiteSpace(pageRef) && !isValidMenu))
                        {
                            var firstMenu = userMenu.ToList().Find(x => x.Url != "#");
                            if (firstMenu != null)
                            {
                                redirectTo = firstMenu.Url;
                            }
                            else
                            {
                                redirectTo = "/ESS/Dashboard";
                            }
                        }
                        else if (isValidMenu)
                        {
                            redirectTo = (redirectTo != validMenu.Url) ? validMenu.Url : redirectTo;
                        }
                        HttpContext.Session.SetString("ref", redirectTo);

                        authResult.Data = redirectTo;
                        return ApiResult<AuthResult>.Ok(authResult, authResult.Message);
                    }
                }

                return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, $"Unable to authenticate with server :\n{authResponse.StatusCode} {authResponse.ErrorMessage}");
            }
            catch (Exception ex)
            {
                return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, ex.Message);
            }

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> DoActivate([FromBody] ActivationParam param)
        {
            HttpContext.Session.SetString(CustomClaimTypes.UserPageAction, "");
            var pageRef = HttpContext.Session.GetString("ref") ?? "";
            if (param == null || string.IsNullOrWhiteSpace(param.Password))
                return ApiResult.Error(HttpStatusCode.BadRequest, "Password or activation nonce is not provided!");

            if (string.IsNullOrWhiteSpace(param.Token))
                return ApiResult.Error(HttpStatusCode.BadRequest, "Invalid activation request");

            // Activating user
            var client = new Client(Configuration);
            var request = new Request("/api/auth/activate", Method.POST);
            request.AddJsonParameter(param);

            var actResponse = client.Execute(request);
            if (actResponse.StatusCode != HttpStatusCode.OK)
            {
                return ApiResult<object>.Error(HttpStatusCode.BadRequest, $"Unable to authenticate using activation server :\n{actResponse.StatusCode} {actResponse.ErrorMessage}");
            }

            var actResult = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(actResponse.Content).Data;
            if (!actResult.Success)
            {
                return ApiResult<object>.Error(HttpStatusCode.Unauthorized, actResult.Message);
            }

            this.generateEmployeeImage(actResult.User);

            // Generate Cookies
            var claims = AuthPrincipal.GenerateClaims(actResult.User, Configuration);
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Generate Action Configuration in Session
            var jsonActionConfiguration = AuthPrincipal.GenerateActionConfiguration(actResult.User, Configuration);
            HttpContext.Session.SetString(CustomClaimTypes.UserPageAction, jsonActionConfiguration);

            var authProperties = new AuthenticationProperties();
            await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

            // Get Landing Page					
            var redirectTo = pageRef;
            var userMenu = Session.GetMenu(actResult.User.Username, true);
            var validMenu = userMenu.ToList().Find(x => x.Url != null && x.Url?.ToLower() == pageRef.ToLower() && x.Url != "#");
            var isValidMenu = validMenu != null;

            if (string.IsNullOrWhiteSpace(pageRef) || (!string.IsNullOrWhiteSpace(pageRef) && !isValidMenu))
            {
                var firstMenu = userMenu.ToList().Find(x => x.Url != "#");
                if (firstMenu != null)
                {
                    redirectTo = firstMenu.Url;
                }
                else
                {
                    redirectTo = "/ESS/Dashboard";
                }
            }
            else if (isValidMenu)
            {
                redirectTo = (redirectTo != validMenu.Url) ? validMenu.Url : redirectTo;
            }
            HttpContext.Session.SetString("ref", redirectTo);

            actResult.Data = redirectTo;
            return ApiResult<AuthResult>.Ok(actResult, actResult.Message);
        }


        [AllowAnonymous]
        public IActionResult ActivateUser(string token = "")
        {
            if (string.IsNullOrEmpty(token))
            {
                ViewBag.Title = "Request User Activation";
                return View();
            }

            ViewBag.Title = "Activate User";
            ViewBag.token = token;
            return View("Activate");

        }


        [AllowAnonymous]
        public IActionResult RequestActivation([FromBody] SendActivationEmailParam param)
        {
            // Request activation email
            var client = new Client(Configuration);
            var request = new Request("/api/auth/activation/request", Method.POST);

            param.BaseURL = $"{HttpContext.Request.Scheme.ToString()}://{HttpContext.Request.Host.ToString()}/Site/Auth/ActivateUser";
            request.AddJsonParameter(param);

            var sendEmailResponse = client.Execute(request);
            if (sendEmailResponse.StatusCode == HttpStatusCode.OK)
            {
                try
                {
                    var result = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(sendEmailResponse.Content).Data;
                    if (result.Success)
                    {
                        var mailer = new Mailer(Configuration);
                        string Subject = "User Activation";
                        string Body = "Hi {0},<br/><br/>" +
                                            "Your user has been activated, please click link bellow to login<br/><br/><br/>" +
                                            "<a href='{1}' target='_blank' style='background-color:#F00;padding:5px;text-align:center;color:white;font-weight:bold;width:100px;' >Activation</a><br/><br/>" +
                                            "If the button didn't work in your browser, try this link instead:<br/>" +
                                            "<a href='{2}'>{3}</a><br/><br/>" +
                                            "Copyright © 2019 Terminal Petikemas Surabaya";
                        var linksActivation = $"{this.Request.Scheme.ToString()}://{this.Request.Host.ToString()}/Site/Auth/ActivateUser/" + result.Data;

                        var message = new MailMessage();
                        message.To.Add(param.Email);
                        message.Subject = Subject;
                        message.Body = string.Format(Body, result.Message, result.Data, result.Data, result.Data);
                        //message.Body = string.Format(Body, result.Message, result.Data);
                        mailer.SendMail(message);

                        result.Message = $"Activation mail has been sent to  {param.Email}";
                        return ApiResult<AuthResult>.Ok(result);
                    }

                    return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, result.Message);
                }
                catch (Exception e)
                {
                    return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, e.Message);
                }

            }

            return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, $"Unable to request user activation :\n{sendEmailResponse.StatusCode} {sendEmailResponse.ErrorMessage}");
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string token = "")
        {

            if (string.IsNullOrWhiteSpace(token))
            {
                ViewBag.Title = "Request Password Reset";
                return View("RequestResetPassword");
            }

            var client = new Client(Configuration);
            var request = new Request($"/api/auth/resetPassword/verifyToken/{token}", Method.GET);

            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(response.Content).Data;

                ViewBag.token = "";
                if (data.Success)
                {
                    ViewBag.token = token;
                    ViewBag.Title = "Reset Password";
                    return View();
                }
                else
                {
                    ViewBag.Title = "Request Password Reset";
                    return View("RequestResetPassword");
                }
            }

            ViewBag.Title = "Request Password Reset";
            return View("RequestResetPassword");

        }

        [AllowAnonymous]
        public IActionResult RequestResetPassword([FromBody] SendResetPasswordEmailParam param)
        {
            var client = new Client(Configuration);
            var request = new Request("/api/auth/resetPassword/request", Method.POST);

            param.BaseURL = $"{this.Request.Scheme.ToString()}://{this.Request.Host.ToString()}/Site/Auth/ResetPassword";
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(response.Content).Data;
                if (data != null && data.Success)
                {
                    try
                    {
                        var mailer = new Mailer(Configuration);
                        string Subject = "Reset Password";
                        string Body = "Dear {0},<br/><br/>" +
                                            "Please click on the below button to reset your password<br/><br/>" +
                                            "<a href='{1}'>Click here to reset</a><br/><br/>" +
                                            "If the button didn't work in your browser, try this link instead:<br/>" +
                                            "<a href='{2}'>{3}</a><br/><br/>" +
                                            "Copyright © 2019 TPS";

                        var message = new MailMessage();
                        message.To.Add(param.Email);
                        message.Subject = Subject;
                        message.Body = string.Format(Body, data.User.FullName, param.BaseURL + "/" + data.Data, param.BaseURL + "/" + data.Data, param.BaseURL + "/" + data.Data);
                        mailer.SendMail(message);

                        return ApiResult<object>.Ok($"Reset password mail has been sent to  {param.Email}");
                    }
                    catch (Exception e)
                    {
                        return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, e.Message);
                    }
                }
                return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, data.Message);
            }

            return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, $"Unable to request password reset :\n{response.StatusCode} {response.ErrorMessage}");
        }

        [AllowAnonymous]
        public IActionResult DoReset([FromBody] SendResetPasswordEmailParam param)
        {
            var client = new Client(Configuration);
            var request = new Request("/api/auth/resetPassword", Method.POST);
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(response.Content).Data;
                if (data != null && data.Success)
                {
                    return ApiResult<object>.Ok("Password has been reset successfully");
                }

                return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, data.Message);
            }

            return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, $"Unable to reset the password :\n{response.StatusCode} {response.ErrorMessage}");
        }

        public IActionResult ChangePassword([FromBody] ChangePasswordParam param)
        {
            param.EmployeeID = Session.Id();
            var client = new Client(Configuration);
            var request = new Request("/api/auth/changePassword", Method.POST);
            request.AddJsonParameter(param);

            var response = client.Execute(request);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return ApiResult<object>.Error(response.StatusCode, response.ErrorMessage);
            }

            var result = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(response.Content).Data;
            return ApiResult<AuthResult>.Ok(result);
        }

        private void generateEmployeeImage(User user)
        {
            var filename = $"{user.Id}.jpg";
            var profilePicture = "";
            var employeeProfilePictureDir = Path.Combine(Env.WebRootPath, "employee");
            var filepath = Path.Combine(employeeProfilePictureDir, filename);

            if (user.UserData.TryGetValue("profilePicture", out profilePicture))
            {
                if (!string.IsNullOrWhiteSpace(profilePicture))
                {
                    var bytes = Convert.FromBase64String(profilePicture);
                    Image image;
                    using (MemoryStream ms = new MemoryStream(bytes))
                    {
                        image = Image.FromStream(ms);
                        if (!Directory.Exists(employeeProfilePictureDir))
                        {
                            Directory.CreateDirectory(employeeProfilePictureDir);
                        }
                        //image.Save(filepath);
                        user.UserData["profilePicture"] = filename;
                        return;
                    }

                }
            }

            user.UserData["profilePicture"] = "";
        }

        /**
         * Function for ESS Mobile because ESS Mobile need Authentication except signin
         * Every function must authorize with token from signin function
         * This is for security
         */

        [HttpPost]
        [AllowAnonymous]
        public IActionResult SignIn([FromBody] LoginParam param)
        {
            HttpContext.Session.SetString(CustomClaimTypes.UserPageAction, String.Empty);
            String pageRef = HttpContext.Session.GetString("ref") ?? String.Empty;
            try
            {
                IRestResponse authResponse = null;
                String odooSessionID = String.Empty;
                var tasks = new List<Task<TaskRequest<Exception>>>
                {
                    Task.Run(() => {
                        Exception error = null;
                        try { authResponse = new Client(Configuration).Execute(new Request($"{Api}signin", Method.POST, param)); }
                        catch (Exception e) { error = e;}
                        return TaskRequest<Exception>.Create("ess_auth", error);
                    }),
                    Task.Run(() =>
                    {
                        Exception error = null;
                        try { odooSessionID = OdooService.Authenticate(Configuration); }
                        catch (Exception e) { error = e; }
                        return TaskRequest<Exception>.Create("odoo_auth", error);
                    })
                };

                var t = Task.WhenAll(tasks);
                try { t.Wait(); }
                catch (Exception e) { throw e; }

                if (t.Status == TaskStatus.RanToCompletion)
                {
                    foreach (var r in t.Result)
                    {
                        var e = (Exception)r.Result;
                        if (e != null) { throw e; }
                    }
                }

                if (authResponse != null && authResponse.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var authResult = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(authResponse.Content).Data;
                    authResult.User.OdooID = odooSessionID;
                    if (!authResult.Success)
                    {
                        return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, authResult.Message);
                    }
                    if (authResult.AuthState == AuthState.Authenticated)
                    {
                        return ApiResult<AuthResult>.Ok(authResult, authResult.Message);
                    }
                }
                else if (authResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return ApiResult<AuthResult>.Error(HttpStatusCode.Unauthorized, $"Invalid password or employee id");
                }
                return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, $"Unable to authenticate with server :\n{authResponse.StatusCode} {authResponse.ErrorMessage}");
            }
            catch (Exception ex) { return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, ex.Message); }
        }

        [AllowAnonymous]
        public IActionResult MRequestResetPassword([FromBody] SendResetPasswordEmailParam param)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            param.BaseURL = $"{this.Request.Scheme}://{this.Request.Host}/Site/Auth/ResetPassword";
            var response = new Client(Configuration).Execute(new Request($"{Api}resetpassword/request", Method.POST, param, "Authorization", bearerAuth));
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var data = JsonConvert.DeserializeObject<ApiResult<AuthResult>.Result>(response.Content).Data;
                if (data != null && data.Success)
                {
                    try
                    {
                        string Body = "Dear {0},<br/><br/>Please click on the below button to reset your password<br/><br/><a href='{1}'>Click here to reset</a><br/><br/>If the button didn't work in your browser, try this link instead:<br/><a href='{2}'>{3}</a><br/><br/>Copyright © 2019 TPS";
                        var message = new MailMessage()
                        {
                            Subject = "Reset Password",
                            Body = string.Format(Body, data.User.FullName, param.BaseURL + "/" + data.Data, param.BaseURL + "/" + data.Data, param.BaseURL + "/" + data.Data)
                        };
                        message.To.Add(param.Email);
                        new Mailer(Configuration).SendMail(message);
                        return ApiResult<object>.Ok($"Reset password mail has been sent to  {param.Email}");
                    }
                    catch (Exception e)
                    {
                        return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, e.Message);
                    }
                }
                return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, data.Message);
            }
            return ApiResult<AuthResult>.Error(HttpStatusCode.BadRequest, $"Unable to request password reset :\n{response.StatusCode} {response.ErrorMessage}");
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult MChangePassword([FromBody] ChangePasswordParam param)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<APIResponse>(JsonConvert.DeserializeObject<ApiResult<APIResponse>.Result>(
                (new Client(Configuration).Execute(new Request($"{Api}mchangepassword", Method.POST, param, "Authorization", bearerAuth))).Content));
        }

        [AllowAnonymous]
        public IActionResult MIsMustChangePassword(String token)
        {
            string bearerAuth = BearerAuth;
            if (Request.Headers.TryGetValue("Authorization", out StringValues authToken)) { bearerAuth = authToken; }
            return new ApiResult<APIResponse>(JsonConvert.DeserializeObject<ApiResult<APIResponse>.Result>(
                (new Client(Configuration).Execute(new Request($"{Api}mismustchangepassword/{token}", Method.GET, "Authorization", bearerAuth))).Content));
        }

        [AllowAnonymous]
        public IActionResult Policy()
        {
            return View("~/Areas/ESS/Views/Administration/Policy.cshtml");
        }

    }

}