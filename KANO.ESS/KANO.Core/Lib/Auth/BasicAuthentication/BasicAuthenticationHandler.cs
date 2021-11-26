using KANO.Core.Lib.Auth.BasicAuthentication.Context;
using KANO.Core.Lib.Helper;
using KANO.Core.Model;
using KANO.Core.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Auth.BasicAuthentication
{
    public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
    {

        IUserAuthService userAuthService;
        IConfiguration configuration;
        public BasicAuthenticationHandler(
               IOptionsMonitor<BasicAuthenticationOptions> options,
               ILoggerFactory logger,
               UrlEncoder encoder,
               ISystemClock clock,
               IUserAuthService userAuthService, IConfiguration configuration)
               : base(options, logger, encoder, clock)
        {
            this.userAuthService = userAuthService;
            this.configuration = configuration;
        }

        protected new BasicAuthenticationEvents Events
        {
            get => (BasicAuthenticationEvents)base.Events;
            set => base.Events = value;
        }
        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new BasicAuthenticationEvents());
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.Fail("Missing Authorization Header");
            }

            UserAuthPermission policyPermission;
            User user;
            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':');
                var username = credentials[0];
                var password = credentials[1];
                policyPermission = await userAuthService.Authenticate(username, password);
                user = await userAuthService.GetUser(username, password);
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Authorization Header");
            }

            if (policyPermission != UserAuthPermission.Allow)
            {
                return AuthenticateResult.Fail("Invalid Username or Password");
            }

            var claims = AuthPrincipal.GenerateClaims(user,configuration);

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Context.Session.Set("ref", Encoding.ASCII.GetBytes(""));
            var authResult = await HandleAuthenticateOnceSafeAsync();
            var eventContext = new BasicAuthenticationChallengeContext(Context, Scheme, Options, properties)
            {
                AuthenticateFailure = authResult?.Failure
            };

            // Avoid returning error=invalid_token if the error is not caused by an authentication failure (e.g missing token).
            if (Options.IncludeErrorDetails && eventContext.AuthenticateFailure != null)
            {
                eventContext.Error = "invalid_token";
                eventContext.ErrorDescription = CreateErrorDescription(eventContext.AuthenticateFailure);
            }

            await Events.Challenge(eventContext);
            if (eventContext.Handled)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(eventContext.Error) || string.IsNullOrWhiteSpace(eventContext.ErrorDescription)) {
                eventContext.Error = "unauthorized_access";
                eventContext.ErrorDescription = $"User is not elligible to access \"{Response.HttpContext.Request.Path}\". Please login to continue.";
            }

            Context.Session.Set("ref", Encoding.ASCII.GetBytes(eventContext.Request.Path));
            Response.Redirect($"/Site/Auth/Login?m={Hasher.Encrypt(eventContext.Request.Path)}");
            return;            
        }

        private static string CreateErrorDescription(Exception authFailure)
        {
            IEnumerable<Exception> exceptions;
            if (authFailure is AggregateException agEx)
            {
                exceptions = agEx.InnerExceptions;
            }
            else
            {
                exceptions = new[] { authFailure };
            }

            var messages = new List<string>();

            foreach (var ex in exceptions)
            {
                // Order sensitive, some of these exceptions derive from others
                // and we want to display the most specific message possible.
                switch (ex)
                {
                    case SecurityTokenInvalidAudienceException _:
                        messages.Add("The audience is invalid");
                        break;
                    case SecurityTokenInvalidIssuerException _:
                        messages.Add("The issuer is invalid");
                        break;
                    case SecurityTokenNoExpirationException _:
                        messages.Add("The token has no expiration");
                        break;
                    case SecurityTokenInvalidLifetimeException _:
                        messages.Add("The token lifetime is invalid");
                        break;
                    case SecurityTokenNotYetValidException _:
                        messages.Add("The token is not valid yet");
                        break;
                    case SecurityTokenExpiredException _:
                        messages.Add("The token is expired");
                        break;
                    case SecurityTokenSignatureKeyNotFoundException _:
                        messages.Add("The signature key was not found");
                        break;
                    case SecurityTokenInvalidSignatureException _:
                        messages.Add("The signature is invalid");
                        break;
                }
            }

            return string.Join("; ", messages);
        }
    }

}
