using KANO.Core.Lib;
using KANO.Core.Lib.Helper;
using KANO.Core.Lib.Auth;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public static class UserSessionServiceExtension
    {
        public static IServiceCollection AddUserSession(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddHttpContextAccessor();
            services.TryAdd(ServiceDescriptor.Transient<IUserSession, UserSession>());

            return services;
        }
    }

    public class NotAuthenticatedException : Exception { }

    public interface IUserSession
    {
        string Id();
        string DisplayName();
        string Email();
        string ProfilePict();
        string Thumbprint();
        string OdooSessionID();
        string UserData();
        string Role();
        bool IsRetirementRequestActive();
        bool HasSubordinate();
        DateTime GetRetirementRequestStart();
        DateTime GetRetirementRequestFinish();
        DateTime GetBirtdate();
        DateTime GetLastEmploymentDate();
        Task<bool> IsAuthorizePolicy(string policy);
        IEnumerable<MenuPage> GetMenu(string username, bool flat = false);
        IEnumerable<MenuPage> GetMenu(bool flat = false);
        IEnumerable<AccessGrant> GetAccessGrant();
        Dictionary<string, AccessGrant> GetPageAccess();
        // Task<bool> CanAccess(string page, string action);
        DateTime GetLastChangedPassword();
    }
    

    public class UserSession : IUserSession
    {        
        private IHttpContextAccessor _ctx;
        private IAuthorizationService _auth;
        private IConfiguration _cfg;

        public UserSession(IHttpContextAccessor ctx, IAuthorizationService auth, IConfiguration cfg)
        {
            _ctx = ctx;
            _auth = auth;
            _cfg = cfg;
        }

        public UserSession(IHttpContextAccessor ctx)
        {
            _ctx = ctx;
        }

        private void checkAuthenticated()
        {
            if (_ctx.HttpContext.User.Identity.IsAuthenticated == false)
                throw new NotAuthenticatedException();
        }

        private string GetClaim(string claimName)
        {
            checkAuthenticated();

            var claim = _ctx.HttpContext.User.FindFirst(claimName);
            return claim.Value;
        }

        public string DisplayName()
        {
            return GetClaim(ClaimTypes.NameIdentifier);
        }

        public string Email()
        {
            return GetClaim(ClaimTypes.Email);
        }

        public string Id()
        {
            return GetClaim(ClaimTypes.Name);
        }

        public string Thumbprint()
        {
            return GetClaim(ClaimTypes.Thumbprint);
        }

        public string OdooSessionID()
        {
            return GetClaim(CustomClaimTypes.OdooSessionID);
        }

        public string Hash()
        {
            return GetClaim(ClaimTypes.Hash);
        }

        public string UserData()
        {
            return GetClaim(ClaimTypes.UserData);
        }

        public string Role()
        {
            return GetClaim(ClaimTypes.Role);
        }
        
        public bool IsRetirementRequestActive()
        {            
            var lastDate =this.GetLastEmploymentDate();
            
            if (lastDate.Year == 1) return default;

            var max1 = lastDate.AddMonths(-17);
            var dateMax1 = new DateTime(max1.Year, max1.Month, 1);
            var min1 = lastDate.AddMonths(-15);
            var dateMin1 = new DateTime(min1.Year, min1.Month, 1);

            var max2 = lastDate.AddMonths(-11);
            var dateMax2 = new DateTime(max2.Year, max2.Month, 1);
            var min2 = lastDate.AddMonths(-9);
            var dateMin2 = new DateTime(min2.Year, min2.Month, 1);

            var now = DateTime.Now;

            return (dateMax1 <= now && dateMin1 > now) || (dateMax2 <= now && dateMin2 > now);
        }

        public DateTime GetRetirementRequestStart(){
            var lastDate = this.GetLastEmploymentDate();

            if (lastDate.Year == 1) return default;

            var max1 = lastDate.AddMonths(-17);
            var dateMax1 = new DateTime(max1.Year, max1.Month, 1);
            var min1 = lastDate.AddMonths(-15);
            var dateMin1 = new DateTime(min1.Year, min1.Month, 1);

            var max2 = lastDate.AddMonths(-11);
            var dateMax2 = new DateTime(max2.Year, max2.Month, 1);
            var min2 = lastDate.AddMonths(-9);
            var dateMin2 = new DateTime(min2.Year, min2.Month, 1);

            var now = DateTime.Now;

            if (now < dateMax1) {
                return dateMax1;
            }else if (dateMax1 <= now && dateMin1 >= now)
            {
                return dateMax1;
            }
            else if (dateMax2 <= now && dateMin2 >= now)
            {
                return dateMax2;
            }
            else if (now < dateMax2) {
                return dateMax2;
            }

            return default;
        }

        public DateTime GetRetirementRequestFinish()
        {
            var lastDate = this.GetLastEmploymentDate();

            if (lastDate.Year == 1) return default;

            var max1 = lastDate.AddMonths(-17);
            var dateMax1 = new DateTime(max1.Year, max1.Month, 1);
            var min1 = lastDate.AddMonths(-15);
            var dateMin1 = new DateTime(min1.Year, min1.Month, 1);

            var max2 = lastDate.AddMonths(-11);
            var dateMax2 = new DateTime(max2.Year, max2.Month, 1);
            var min2 = lastDate.AddMonths(-9);
            var dateMin2 = new DateTime(min2.Year, min2.Month, 1);

            var now = DateTime.Now;

            if (dateMax1 <= now && dateMin1 >= now)
            {
                return dateMin1;
            }
            else if (dateMax2 <= now && dateMin2 >= now)
            {
                return dateMin2;
            }

            return default;
        }

        public DateTime GetBirtdate()
        {
            // //FTP
            // var employeeID = this.Id();
            // if(employeeID == "7612050060"){
            //     return  DateTime.ParseExact("1969-08-17", "yyyy-MM-dd", null);           
            // }
            var strDate = GetClaim(ClaimTypes.DateOfBirth);
            return  DateTime.ParseExact(strDate, "yyyy-MM-dd", null);            
        }
        
        public DateTime GetLastEmploymentDate()
        {
            // //FTP
            // var employeeID = this.Id();
            // if(employeeID == "7612050060"){
            //     return  DateTime.ParseExact("2021-08-01", "yyyy-MM-dd", null);           
            // }

            var strData = this.UserData();
            if(strData.Length > 0){
                var data = JsonConvert.DeserializeObject<Dictionary<string,string>>(strData);
                var val = "";
                if(data.TryGetValue("lastEmploymentDate", out val)){
                    return  DateTime.ParseExact(val,"yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            }
            
            return default;
            //return new DateTime(2021, 9, 2);
        }

        public string ProfilePict()
        {
            throw new NotImplementedException();
        }

        [AllowAnonymous]
        public IEnumerable<MenuPage> GetMenu(bool flat = false)
        {
            var username = Id();            
            return this.GetMenu(username, flat);
        }

        [AllowAnonymous]
        public IEnumerable<MenuPage> GetMenu(string username, bool flat = false)
        {
            var client = new Client(_cfg);
            var request = new Request($"api/administration/page/user/{username}/{flat}", Method.GET);
            var response = client.Execute(request);

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var result = JsonConvert.DeserializeObject<ApiResult<List<MenuPage>>.Result>(response.Content);
                return result.Data;
            }

            return new List<MenuPage>();
        } 

		public async Task<bool> IsAuthorizePolicy(string policyName)
        {
            if (_auth == null) {
                return false;
            }
            checkAuthenticated();

            var policy = await _auth.AuthorizeAsync(_ctx.HttpContext.User, policyName);

            if (policy.Succeeded)
                return true;

            return false;
        }

		public IEnumerable<AccessGrant> GetAccessGrant()
		{
			//var accessJSON = this.GetClaim(CustomClaimTypes.UserGroupAccess);            
   //         if(string.IsNullOrWhiteSpace(accessJSON)){
                var username = Id();
                var client = new Client(_cfg);
                var request = new Request($"api/administration/group/get/user/{username}", Method.GET);
                var response = client.Execute(request);

                if (!string.IsNullOrWhiteSpace(response.Content))
                {
                    var access = new List<AccessGrant>();
                    if(!string.IsNullOrWhiteSpace(response.Content)){
                        var result = JsonConvert.DeserializeObject<ApiResult<List<Group>>.Result>(response.Content);
                        foreach(var group in result.Data)
                        {
                            foreach (var grant in group.Grant)
                            {
                                access.Add(grant);
                            }
                        }
                    }                    

                    return access.Distinct();
                }
            //}

            return new List<AccessGrant>();
		}

        public Dictionary<string, AccessGrant> GetPageAccess()
		{
			var accessGrants = this.GetAccessGrant();            
            var accessMap = new Dictionary<string, AccessGrant>();
            foreach(var accessGrant  in accessGrants){
                AccessGrant _accessGrant;
                if(accessMap.TryGetValue(accessGrant.PageID, out _accessGrant)){
                    accessGrant.CanCreate = _accessGrant.CanCreate || accessGrant.CanCreate;
                    accessGrant.CanRead = _accessGrant.CanRead || accessGrant.CanRead;
                    accessGrant.CanUpdate = _accessGrant.CanUpdate || accessGrant.CanUpdate;
                    accessGrant.CanDelete = _accessGrant.CanDelete || accessGrant.CanDelete;
                    accessGrant.CanUpload = _accessGrant.CanUpload || accessGrant.CanUpload;
                    accessGrant.CanDownload = _accessGrant.CanDownload || accessGrant.CanDownload;
                    
                }
                
                accessMap[accessGrant.PageID] = accessGrant;
            }
            return accessMap;
		}

        public bool HasSubordinate(){
            var strData = this.UserData();
            if(strData.Length > 0){
                var data = JsonConvert.DeserializeObject<Dictionary<string,string>>(strData);
                var val = "";
                if(data.TryGetValue("hasSubordinate", out val)){
                    return  val.Equals("True");
                }
            }
            return false;
        }

        public DateTime GetLastChangedPassword()
        {
            var username = Id();
            var client = new Client(_cfg);
            var request = new Request($"api/administration/user/get/detailuser/{username}", Method.GET);
            var response = client.Execute(request);
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var result = JsonConvert.DeserializeObject<ApiResult<User>.Result>(response.Content);
                User user = result.Data;
                return new DateTime(user.LastPasswordChangedDate.Year, user.LastPasswordChangedDate.Month, user.LastPasswordChangedDate.Day, user.LastPasswordChangedDate.Hour, user.LastPasswordChangedDate.Minute, user.LastPasswordChangedDate.Second, DateTimeKind.Utc);
                //return user.LastPasswordChangedDate;
            }
            return new DateTime();

        }
    }
}
