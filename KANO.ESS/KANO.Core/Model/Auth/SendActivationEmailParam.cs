using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class SendActivationEmailParam
    {
        public string EmployeeID { get; set; }
        public string Email { get; set; }

        public string BaseURL { get; set; }
    }
}
