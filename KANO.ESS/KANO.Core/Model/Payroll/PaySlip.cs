using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model.Payroll
{
    public class PaySlip: BaseDocumentVerification
    {
        public DateTime PaySlipMonth { get; set; }
        /// <summary>
        /// ex: 201901, 
        /// for : January 2019
        /// </summary>
        public string ProcessID { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string MonthYear { get; set; }
        public string Department { get; set; }
        public string CycleTimeDescription { get; set; }
        public double Amount { get; set; }
        public double AmountNetto { get; set; }
        public string YearMonth { get; set; }

    }
}
