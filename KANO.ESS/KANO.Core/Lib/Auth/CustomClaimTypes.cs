using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Auth
{
    public static class CustomClaimTypes
    {
        public static readonly string
            AuthType = "AUTH_TYPE",
            UserGroup = "USER_GROUP_ID",
            UserPageAction = "USER_PAGE_ACTION",            
            UserGroupAccess = "USER_GROUP_DETAIL",
            UserLastPasswordChange = "LAST_PASSWORD_CHAGED",
            OdooSessionID = "USER_OODO_SESSION_ID";           
    }
}
