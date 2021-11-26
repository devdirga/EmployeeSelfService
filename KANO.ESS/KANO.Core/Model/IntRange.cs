using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    public class IntRange
    {
        private int _from;
        private int _to;
        private int _total;

        public bool InRange(int dt)
        {
            return Start.CompareTo(dt) <= 0 && Finish.CompareTo(dt) >= 0;
        }
        public bool IsOverlap(IntRange dt)
        {
            return InRange(dt.Start) || InRange(dt.Finish);
        }

        public bool IsIntersect(IntRange dr)
        {
            return InRange(dr.Start) || InRange(dr.Finish) || dr.InRange(Start) || dr.InRange(Finish);
        }

        public IntRange(int start, int finish)
        {
            Start = start; Finish = finish;
        }
        public int Total
        {
            get
            {
                if (_total == 0)
                {
                    _total = (Finish - Start);
                }
                return _total;
            }

            set
            {
                _total = value;
                _to = _from + _total;
            }
        }
       
        public int Start
        {
            get { return _from; }
            set
            {
                _from = value;
                _to = _from + _total;
            }
        }

        public int Finish
        {
            get { return _to; }
            set
            {
                _to = value;
                _total = (Finish - Start);
            }
        }

        public IntRange()
        {
            _from = 0;
            _to =0;
        }
    }
}
