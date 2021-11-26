using KANO.Core.Lib.Auth;
using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public interface IUserAuthService
    {
        Task<UserAuthPermission> Authenticate(string username, string password, string policy = "RESTAPI");
        Task<User> GetUser(string username, string password);
    }

    public class UserAuthService : IUserAuthService
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        private IConfiguration Configuration;

        public UserAuthService(IMongoManager mongo)
        {
            Mongo = mongo;
            DB = Mongo.Database();
        }

        public UserAuthService(IConfiguration config)
        {
            Configuration = config;
        }


        /// <summary>
        /// Authenticate username and password against a certain policy.
        /// </summary>
        /// <param name="username">The username of the user</param>
        /// <param name="password">The password of the user</param>
        /// <param name="policy">The policy name to check permission</param>
        /// <returns></returns>
        public Task<UserAuthPermission> Authenticate(string username, string password, string policy = "RESTAPI")
        {
            return Task.Run(async () =>
            {
                var usr = await this.GetUser(username, password);
                return usr != null ? UserAuthPermission.Allow : UserAuthPermission.Deny;
            });
        }

        public Task<User> GetUser(string username, string password)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                return Task.Run(() =>
                {
                    User usr;
                    if (Mongo != null)
                    {
                        usr = DB.GetCollection<User>().Find(x => x.Username.ToLowerInvariant() == username).FirstOrDefault();                        
                    }
                    else 
                    {
                        var userChecker = new UserChecker(Configuration);
                        usr = userChecker.Get(username);
                    }

                    if (usr?.VerifyPassword(password) == Microsoft.AspNetCore.Identity.PasswordVerificationResult.Success)
                        return usr;

                    return null;
                });
            }
            return null;
        }
    }

    public enum UserAuthPermission
    {
        Allow = 1,
        Deny = 0
    }
}
