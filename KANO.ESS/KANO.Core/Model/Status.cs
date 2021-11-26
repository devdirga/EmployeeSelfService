using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{    
    public enum DocumentStatus : int
    {
        NeedVerification = 0,
        Verified = 1,
        Rejected = 2,
    }

    public enum ActionType : int
    {
        Create = 0,
        Update = 1,
        Delete = 2,
    }

    public enum NoYes : int
    {
        No = 0,
        Yes = 1,
    }
}
