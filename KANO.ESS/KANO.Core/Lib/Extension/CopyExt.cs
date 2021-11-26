using System.Collections.Generic;

namespace KANO.Core.Lib.Extension
{
    public interface IDeepClone<T>
    {
        T DeepClone();
    }

    public class IDeepCloneClass<T> where T : IDeepClone<T> { }
    public class IDeepCloneStruct<T> where T : struct { }

    public static class ListExt
    {
        // special string implementation
        public static Dictionary<T, string> DeepClone<T>(this Dictionary<T, string> list)
        {
            var obj = new Dictionary<T, string>();
            foreach (var it in list)
            {
                obj[it.Key] = it.Value;
            }

            return obj;
        }

        public static Dictionary<string, V> DeepClone<V>(this Dictionary<string, V> list, IDeepCloneClass<V> _ignore = null)
            where V : IDeepClone<V>
        {
            var obj = new Dictionary<string, V>();
            foreach (var it in list)
            {
                obj[it.Key] = it.Value.DeepClone();
            }

            return obj;
        }

        public static Dictionary<string, V> DeepClone<V>(this Dictionary<string, V> list, IDeepCloneStruct<V> _ignore = null)
          where V : struct
        {
            var obj = new Dictionary<string, V>();
            foreach (var it in list)
            {
                obj[it.Key] = it.Value;
            }

            return obj;
        }

        // Implementation for class supporting IDeepClone
        // using _ignore trick to support more than 1 type overload on same func name
        public static Dictionary<T, V> DeepClone<T, V>(this Dictionary<T, V> list, IDeepCloneClass<V> _ignore = null)
            where T : struct
            where V : IDeepClone<V>
        {
            var obj = new Dictionary<T, V>();
            foreach (var it in list)
            {
                obj[it.Key] = it.Value.DeepClone();
            }

            return obj;
        }

        // Implementation for primitive type and struct
        // using _ignore trick to support more than 1 type overload on same func name
        public static Dictionary<T, V> DeepClone<T, V>(this Dictionary<T, V> list, IDeepCloneStruct<V> _ignore = null)
            where T : struct
            where V : struct
        {
            var obj = new Dictionary<T, V>();
            foreach (var it in list)
            {
                obj[it.Key] = it.Value;
            }

            return obj;
        }

        // special string implementation
        public static List<string> DeepClone(this List<string> list)
        {
            var obj = new List<string>();
            obj.AddRange(list);

            return obj;
        }

        public static List<T> DeepClone<T>(this List<T> list) where T : IDeepClone<T>
        {
            var obj = new List<T>();
            foreach (var it in list)
            {
                obj.Add(it.DeepClone());
            }

            return obj;
        }
    }
}
