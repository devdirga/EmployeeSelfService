using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class OdooToken
    {
        public string access_token { get; set; }
        public string access_token_validity { get; set; }
        public string token_type { get; set; }
        public string user_token { get; set; }
        public string refresh_token { get; set; }
    }
}
