using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KANO.Core.Service;

namespace KANO.Core.Model
{

    public class DateRange
    {
        private DateTime _from;
        private DateTime _to;
        private Double _days;

        public bool InRange(DateTime dt)
        {
            return Start <= dt && Finish >= dt;
        }
        public bool IsOverlap(DateRange dt)
        {
            return InRange(dt.Start) || InRange(dt.Finish);
        }

        public bool IsIntersect(DateRange dr)
        {
            return InRange(dr.Start) || InRange(dr.Finish) || dr.InRange(Start) || dr.InRange(Finish);
        }

        public DateRange(DateTime start, DateTime finish)
        {
            Start = start.ToLocalTime(); Finish = finish.ToLocalTime();
        }

        /// <summary>
        /// Get true monthly value based on date.
        /// Eg. 15 Jan ~ 14 Feb is a full month.
        /// </summary>
        public double TrueMonthly
        {
            get
            {
                if (Finish < Start) return -1;
                var sdate = Start.Date;
                var fdate = Finish.Date;

                var mon = 0;
                while (sdate.AddMonths(1) <= fdate)
                {
                    mon++;
                    sdate = sdate.AddMonths(1);
                }

                var dim = DateTime.DaysInMonth(sdate.Year, sdate.Month);
                fdate = fdate.AddDays(1);
                var delta = fdate - sdate;

                return mon + delta.TotalDays / dim;
            }
        }

        public double Month
        {
            get
            {
                var noofMonth = Finish.Subtract(Start).Days / (365.25 / 12);
                return noofMonth;
            }
        }

        public double Days
        {
            get
            {
                if (_days == 0)
                {
                    _days = (Finish - Start).TotalDays;
                }
                return _days;
            }

            set
            {
                _days = value;
                _to = _from.AddDays(_days);
            }
        }
        public double Hours
        {
            get
            {
                return (Finish - Start).TotalHours;
            }
        }
        public double Seconds
        {
            get
            {
                return (Finish - Start).TotalSeconds;
            }
        }
        public DateTime Start
        {
            get { return _from; }
            set
            {
                _from = value.ToLocalTime();
                _to = _from.AddDays(_days);
            }
        }

        public DateTime Finish
        {
            get { return _to; }
            set
            {
                _to = value.ToLocalTime();
                _days = (Finish - Start).TotalDays;
            }
        }

        public DateRange()
        {
            _from = Tools.DefaultDate;
            _to = Tools.DefaultDate;
        }

        public List<DateTime> EnumDate()
        {
            var res = new List<DateTime>();
            for (var d = Start.Date; d <= Finish.Date; d = d.AddDays(1))
                res.Add(d);
            return res;
        }
        public List<DateRange> EnumMonth()
        {
            var res = new List<DateRange>();
            for (var d = new DateTime(Start.Year, Start.Month, 1); d <= new DateTime(Finish.Year, Finish.Month, 1); d = d.AddMonths(1))
            {
                res.Add(new DateRange(d, new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month), 23, 59, 59, 999)));
            }
            return res;
        }
        public List<DateRange> EnumMonthlyProrate()
        {
            var res = new List<DateRange>();
            var d = Start;
            while (d < Finish)
            {
                var eom = new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month), 23, 59, 59, 999, DateTimeKind.Local);
                if (Finish < eom)
                    eom = Finish;
                var entry = new DateRange(d, eom);
                res.Add(entry);
                d = eom + new TimeSpan(0, 0, 0, 0, 1);
            }
            return res;
        }
        public List<DateRange> EnumYear()
        {
            var res = new List<DateRange>();
            for (var d = Start.Year; d <= Finish.Year; d++)
            {
                res.Add(new DateRange(new DateTime(d, 1, 1), new DateTime(d, 12, 31, 23, 59, 59, 999)));
            }
            return res;
        }
    }


}
