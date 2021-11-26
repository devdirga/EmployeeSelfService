using KANO.Core.Lib.Extension;
using KANO.Core.Lib.Helper;
using KANO.Core.Lib.Middleware;
using KANO.Core.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Net;

namespace KANO.Core.Lib.Auth
{
    public static class AuthPrincipal
    {
        public const string Issuer = "KANO.Core-authentication";             

        public static Claim[] GenerateClaims(User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return new Claim[] {
                new Claim(
                    CustomClaimTypes.OdooSessionID,
                    user.OdooID,
                    ClaimValueTypes.String, Issuer
                ),
                new Claim(
                    ClaimTypes.Name,
                    user.Username,
                    ClaimValueTypes.String, Issuer
                ),
                new Claim(
                    ClaimTypes.Thumbprint,
                    user.Id,
                    ClaimValueTypes.String, Issuer
                ),
                new Claim(
                    ClaimTypes.NameIdentifier,  
                    user.FullName,
                    ClaimValueTypes.String, Issuer
                ),
                new Claim(
                    ClaimTypes.Hash,
                    Hasher.Encrypt($"{user.Username}_{DateTime.Now.Ticks}"),
                    ClaimValueTypes.String, Issuer
                ),
                new Claim(
                    ClaimTypes.Email,
                    user.Email,
                    ClaimValueTypes.String, Issuer
                ),
                new Claim(
                    CustomClaimTypes.UserLastPasswordChange,
                    Format.DateToString(user.LastPasswordChangedDate,"yyyy-MM-dd hh:mm:ss"),
                    ClaimValueTypes.String, Issuer
                ),
                new Claim(
                    ClaimTypes.Role,
                    string.Join("|", user.Roles),
                    ClaimValueTypes.String, Issuer
                ),
                new Claim(
                    ClaimTypes.UserData,
                    JsonConvert.SerializeObject(user.UserData),
                    ClaimValueTypes.String, Issuer
                ),
            };
        }

        public static Claim[] GenerateClaims(User user, IConfiguration config)
        {

            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var claims = new List<Claim>();           
            if (user.Roles != null && user.Roles.Count > 0)
            {

                foreach (var role in user.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role, null, Issuer));
                }                
            }
            
            claims.AddRange(GenerateClaims(user));            

            return claims.ToArray();
        }

        public static string GenerateActionConfiguration(User user, IConfiguration config) {
            var client = new Client(config);
            var request = new Request($"api/administration/group/get/user/{user.Username}", Method.GET);
            var response = client.Execute(request);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception(response.StatusDescription);
            }

            var result = JsonConvert.DeserializeObject<ApiResult<List<Group>>.Result>(response.Content);
            var groups = new List<Group>(result.Data).FindAll(g => user.Roles.Contains(g.Id));

            var actions = new List<string>();
            var access = new List<AccessGrant>();
            if (groups.Any())
            {
                foreach (var group in groups)
                {
                    foreach (var grant in group.Grant)
                    {
                        access.Add(grant);
                        try
                        {
                            if ((grant.Actions & ActionGrant.Read) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Read.ToString());
                            if ((grant.Actions & ActionGrant.Create) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Create.ToString());
                            if ((grant.Actions & ActionGrant.Update) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Update.ToString());
                            if ((grant.Actions & ActionGrant.Delete) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Delete.ToString());
                            if ((grant.Actions & ActionGrant.Upload) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Upload.ToString());
                            if ((grant.Actions & ActionGrant.Download) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Download.ToString());
                            if (grant.SpecialActions != null && grant.SpecialActions.Count > 0)
                                actions.AddRange(grant.SpecialActions.Select(sa => grant.PageCode + ":" + sa).ToArray());
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }

                //claims.Add(new Claim(CustomClaimTypes.UserGroupAccess, JsonConvert.SerializeObject(access.Distinct())));
            }

            return JsonConvert.SerializeObject(actions.Distinct());
        }

        public static bool AddClaim(ClaimsPrincipal id, IMongoDatabase db)
        {
            var user = db.GetCollection<User>().Find(x => x.Username == id.Identity.Name).FirstOrDefault();
            if (user == null)
                return false;

            var claims = new List<Claim>();
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role, null, Issuer));
            }

            if (user.Roles != null && user.Roles.Count > 0)
            {
                var grps = db.GetCollection<Group>().Find(g => user.Roles.Contains(g.Id)).ToList();
                var actions = new List<string>();
                foreach(var g in grps)
                {
                    foreach(var grant in g.Grant)
                    {
                        try
                        {
                            if ((grant.Actions & ActionGrant.Read) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Read.ToString());
                            if ((grant.Actions & ActionGrant.Create) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Create.ToString());
                            if ((grant.Actions & ActionGrant.Update) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Update.ToString());
                            if ((grant.Actions & ActionGrant.Delete) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Delete.ToString());
                            if ((grant.Actions & ActionGrant.Upload) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Upload.ToString());
                            if ((grant.Actions & ActionGrant.Download) > 0)
                                actions.Add(grant.PageCode + "." + ActionGrant.Download.ToString());
                            if (grant.SpecialActions != null && grant.SpecialActions.Count > 0)
                                actions.AddRange(grant.SpecialActions.Select(sa => grant.PageCode + ":" + sa).ToArray());
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
                var acts = actions.Distinct();
                foreach(var act in acts)
                {
                    claims.Add(new Claim(CustomClaimTypes.UserPageAction, act, null, Issuer));
                }
            }

            claims.AddRange(GenerateClaims(user));
            id.AddIdentity(new ClaimsIdentity(claims));

            return true;
        }

        public static bool AddClaim(ClaimsPrincipal id, IConfiguration config)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var userChecker = new UserChecker(config);
            var user = userChecker.Get(id.Identity.Name);
            if (user == null)
            {
                return false;
            }


            var claims = new List<Claim>();

            claims.AddRange(GenerateClaims(user, config));
            id.AddIdentity(new ClaimsIdentity(claims));

            return true;
        }

        public static bool CheckClaim(ClaimsPrincipal id, IConfiguration config)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var userChecker = new UserChecker(config);
            var user = userChecker.Get(id.Identity.Name);

            return (user != null);            
        }

        public static bool AddClaimNt(ClaimsPrincipal id, IMongoDatabase db)
        {
            var identityNt = id.Identity as ADWindowsIdenitty;
            if (identityNt == null)
                return false;

            var keyword = identityNt.Id.NormalizeSearch();
            var user = db.GetCollection<User>().Find(x => x.Username == keyword).FirstOrDefault();
            if (user == null)
                return false;

            var claims = new List<Claim>();
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role, null, Issuer));
            }

            claims.AddRange(GenerateClaims(user));

            id.AddIdentity(new ClaimsIdentity(claims));

            return true;
        }
        public static bool AddClaimNt(ClaimsPrincipal id, IConfiguration config)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var identityNt = id.Identity as ADWindowsIdenitty;
            if (identityNt == null)
                return false;

            var keyword = identityNt.Id.NormalizeSearch();

            var userChecker = new UserChecker(config);
            var user = userChecker.Get(keyword);

            if (user == null)
            {
                return false;
            }

            return addClaimNt(id, user);
        }

        public static bool CheckClaimNt(ClaimsPrincipal id, IConfiguration config)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));

            var identityNt = id.Identity as ADWindowsIdenitty;
            if (identityNt == null)
                return false;

            var keyword = identityNt.Id.NormalizeSearch();

            var userChecker = new UserChecker(config);
            var user = userChecker.Get(keyword);

            return (user != null);            
        }

        private static bool addClaimNt(ClaimsPrincipal id, User user) {            
            var claims = new List<Claim>();
            
            claims.AddRange(GenerateClaims(user));
            id.AddIdentity(new ClaimsIdentity(claims));

            return true;
        }       
    }
}
