using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.DirectoryServices.AccountManagement;
using System.Security.Principal;
using System.DirectoryServices;

namespace KANO.Core.Lib.Auth
{
    public class DirectoryActiveUser
    {
        // Query using Sid of Windows User
        public static UserPrincipal DAUser(string Sid)
        {
            using (var search = new PrincipalSearcher(new UserPrincipal(new PrincipalContext(ContextType.Domain, Environment.UserDomainName))))
            {
                List<UserPrincipal> users = search.FindAll().Select(u => (UserPrincipal)u).ToList();
                return users.Where(x => x.Sid.Value.Equals(Sid)).FirstOrDefault();
            }
        }

        public static UserPrincipal FindByNt(WindowsIdentity id)
        {
            var sam = id.Name.Split('\\', 2);
            var ctx = new PrincipalContext(ContextType.Domain, sam[0]);
            var user = new UserPrincipal(ctx) { SamAccountName =  sam[1]};

            using (var search = new PrincipalSearcher(user))
            {
                var result = search.FindOne() as UserPrincipal;
                return result;
            }
        }
    }
}
