using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Auth.BasicAuthentication.Context
{
    public class BasicAuthenticationEvents
    {
        public virtual Task Challenge(BasicAuthenticationChallengeContext context) => OnChallenge(context);
        public Func<BasicAuthenticationChallengeContext, Task> OnChallenge { get; set; } = context => Task.CompletedTask;
    }
}
