using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Extension
{
    public class RateCalc
    {
        public double OriginalValue { get; set; }
        public double TotalValue { get; set; }
        public double AmountTax { get; set; }
        public double Rate { get; set; }
    }
    public class MathExt
    {
        public static double FMRoundtoFixed(double val)
        {
            return Math.Round(val, 3, MidpointRounding.AwayFromZero);
        }

        public static double FMValueRound(double val)
        {
            return Math.Round(val, 4, MidpointRounding.AwayFromZero);
        }

        public static RateCalc ReverseCalc(double total, double rate)
        {
            double res = (1 / (1 + rate)) * total;

            var a = new RateCalc()
            {
                OriginalValue = res,
                TotalValue = total,
                Rate = rate,
                AmountTax = total - res
            };

            return a;
        }
    }
}
