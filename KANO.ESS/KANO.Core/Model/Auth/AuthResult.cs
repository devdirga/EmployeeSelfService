using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class AuthResult
    {
        public AuthState AuthState { get; set; } = AuthState.NotAuthenticated;
        public bool Success{ get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
        public User User { get; set; }
    }

    public enum AuthState
    {
        NotAuthenticated = 0,
        NeedActivation = 1,
        Authenticated = 16
    }
}
