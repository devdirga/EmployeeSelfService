using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class FetchParam
    {
        public string EmployeeID { set; get; }
        public int Limit { set; get; }
        public int Offset { set; get; }
        public string Filter { set; get; }
    }
        
    public class ParamTaskFilter
    {
        public string Username { set; get; }
        public int Limit { set; get; }
        public int Offset { set; get; }
        public bool ActiveOnly { set; get; }
        public DateRange Range { set; get; }
    }
}
