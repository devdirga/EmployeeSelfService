using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace KANO.Core.Lib.Extension
{
    public interface IDiffByKey
    {
        string Key();
    }

    public class Diff: IDeepClone<Diff>
    {
        public string Field;
        public string Before;
        public string After;

        public Diff DeepClone()
        {
            return (Diff)MemberwiseClone();
        }

        public Diff(string field, string oldv, string newv)
        {
            Field = field;
            Before = oldv;
            After = newv;
        }
    }

    public static class DiffExt
    {
        const string DATETIME_FORMAT = "yyyy-MM-ddTHH:mm:ss.fffffffK";

        protected class CompareObj
        {
            public object ValA { get; set; }
            public object ValB { get; set; }
        }

        private static void appendDiff(string field, List<Diff> diffs)
        {
            foreach(var df in diffs)
            {
                df.Field = $"{field}.{df.Field}";
            }
        }

        private static List<Diff> propCompareDictionary(Type childtype, object propA, object propB, string strFormat = "[{0}]")
        {
            var mapping = new Dictionary<string, CompareObj>();

            if (propA is IDictionary mapA)
            {
                var enumA = mapA.GetEnumerator();
                while (enumA.MoveNext())
                {
                    var key = enumA.Key.ToString();
                    var val = enumA.Value;

                    mapping[key] = new CompareObj
                    {
                        ValA = enumA.Value,
                        ValB = null,
                    };
                }
            }

            if (propB is IDictionary mapB)
            {
                var enumB = mapB.GetEnumerator();
                while (enumB.MoveNext())
                {
                    var key = enumB.Key.ToString();
                    var val = enumB.Value;

                    if (mapping.ContainsKey(key))
                    {
                        mapping[key].ValB = val;
                        continue;
                    }

                    mapping[key] = new CompareObj
                    {
                        ValA = null,
                        ValB = enumB.Value,
                    };
                }
            }

            var childList = new List<Diff>();
            foreach (var mlist in mapping)
            {
                var fieldname = string.Format(strFormat, JsonConvert.SerializeObject(mlist.Key));
                propCompare(childtype, fieldname, mlist.Value.ValA, mlist.Value.ValB, childList);
            }

            return childList;
        }

        private static void propCompare(Type type, string fname, object propA, object propB, List<Diff> diff)
        {
            // both is null
            if (propA == null && propB == null)
                return;

            // base type info
            var nullable_type = Nullable.GetUnderlyingType(type);

            // special handle for DateTime
            if (type == typeof(DateTime))
            {
                DateTime? dateA = null;
                DateTime? dateB = null;

                if (propA != null)
                    dateA = (DateTime)propA;
                if (propB != null)
                    dateB = (DateTime)propB;
                if (dateA == dateB)
                    return;

                diff.Add(new Diff(fname, dateA?.ToString(DATETIME_FORMAT), dateB?.ToString(DATETIME_FORMAT)));
                return;
            }

            if (nullable_type == typeof(DateTime))
            {
                var dateA = propA as DateTime?;
                var dateB = propB as DateTime?;
                if (dateA == dateB)
                    return;

                diff.Add(new Diff(fname, dateA?.ToString(DATETIME_FORMAT), dateB?.ToString(DATETIME_FORMAT)));
                return;
            }

            // value type and string
            if (type == typeof(string) || nullable_type == typeof(string))
            {
                if (Equals(propA, propB))
                    return;

                var valA = propA as string;
                var valB = propB as string;

                diff.Add(new Diff(fname, valA, valB));
                return;
            }

            // numeric type
            if (type.IsValueType || (nullable_type != null && nullable_type.IsValueType))
            {
                if (Equals(propA, propB))
                    return;

                var valA = JsonConvert.SerializeObject(propA);
                var valB = JsonConvert.SerializeObject(propB);

                diff.Add(new Diff(fname, valA, valB));
                return;
            }

            // dictionary
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                var childList = propCompareDictionary(type.GetGenericArguments()[1], propA, propB);

                appendDiff(fname, childList);
                diff.AddRange(childList);
                return;
            }
            
            // is enumerable
            if (typeof(ICollection).IsAssignableFrom(type))
            {
                var listA = propA as ICollection;
                var listB = propB as ICollection;

                var childtype = type.GetGenericArguments()[0];
                if (typeof(IDiffByKey).IsAssignableFrom(childtype))
                {
                    var mappedA = new Dictionary<string, object>();
                    if (listA != null)
                    {
                        foreach(var itemA in listA)
                        {
                            var haskeyA = itemA as IDiffByKey;
                            mappedA[haskeyA.Key()] = haskeyA;
                        }
                    }

                    var mappedB = new Dictionary<string, object>();
                    if (listB != null)
                    {
                        foreach (var itemB in listB)
                        {
                            var haskeyB = itemB as IDiffByKey;
                            mappedB[haskeyB.Key()] = haskeyB;
                        }
                    }

                    var childListfromdic = propCompareDictionary(childtype, mappedA, mappedB, "[key:{0}]");

                    appendDiff(fname, childListfromdic);
                    diff.AddRange(childListfromdic);
                }

                var max = 0;
                if (listA != null)
                {
                    max = listA.Count;
                }

                if (listB != null && max < listB.Count)
                {
                    max = listB.Count;
                }

                var childList = new List<Diff>();
                var enumA = listA?.GetEnumerator();
                var enumB = listB?.GetEnumerator();
                for (var i = 0; i < max; i++)
                {
                    object curA = null;
                    object curB = null;

                    if (listA != null && i < listA.Count && enumA?.MoveNext() == true)
                    {
                        curA = enumA.Current;
                    }

                    if (listB != null && i < listB.Count && enumB?.MoveNext() == true)
                    {
                        curB = enumB.Current;
                    }

                    propCompare(childtype, $"[{i}]", curA, curB, childList);
                }

                appendDiff(fname, childList);
                diff.AddRange(childList);
                return;
            }

            // child object
            if (type is object)
            {
                if (propA != null && propB == null)
                {
                    diff.Add(new Diff(fname, "{}", null));
                }

                if (propA == null && propB != null)
                {
                    diff.Add(new Diff(fname, null, "{}"));
                }

                var childList = Diff(propA, propB);

                appendDiff(fname, childList);
                diff.AddRange(childList);
                return;
            }

            throw new Exception("Unknown type");
        }

        public static List<Diff> Diff<T>(T a, T b) where T: class
        {
            var diff = new List<Diff>();

            // both is null
            if (a == null && b == null)
                return diff;

            PropertyInfo[] properties = new PropertyInfo[] { };
            
            // a is not null
            if (a != null)
                properties = a.GetType().GetProperties();
            // b is not null
            else
                properties = b.GetType().GetProperties();

            foreach (PropertyInfo prop in properties)
            {
                var propA = a == null? null : prop.GetValue(a);
                var propB = b == null? null : prop.GetValue(b);

                propCompare(prop.PropertyType, prop.Name, propA, propB, diff);
            }

            return diff;
        }
    }
}
