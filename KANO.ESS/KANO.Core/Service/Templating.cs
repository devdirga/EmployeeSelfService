using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public static class Templating
    {
        /// <summary>
        /// Gets a value from a source object based on member name.
        /// </summary>
        /// <param name="path">The member name of the source object. Specifying this parameter as "this" will return the source object.</param>
        /// <param name="source">The source object to find value from</param>
        /// <returns>Returns the value specified path from source object. Will return null if path is invalid or source is null.</returns>
        public static object RetrieveValue(string path, object source)
        {
            if (source == null || string.IsNullOrWhiteSpace(path)) return null;

            var parts = path.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0) return null;

            var curPart = parts[0].Trim();

            if (curPart == "this") return source;

            object curObj = null;

            var srcType = source.GetType();
            try
            {
                var p = srcType.GetProperties().Where(pr => pr.Name == curPart).FirstOrDefault();
                if (p != null)
                {
                    if (p.CanRead)
                    {
                        curObj = p.GetValue(source);
                    }
                }
            }
            catch { }
            try
            {
                var p = srcType.GetFields().Where(pr => pr.Name == curPart).FirstOrDefault();
                if (p != null)
                {
                    curObj = p.GetValue(source);
                }
            }
            catch { }

            if (parts.Length == 1)
            {
                return curObj;
            }
            else if (parts.Length > 1 && curObj != null)
            {
                var nextPath = "";
                for (var i = 1; i < parts.Length; i++)
                {
                    nextPath += (nextPath != "" ? "." : "") + parts[i];
                }
                return RetrieveValue(nextPath, curObj);
            }

            return null;
        }

        /// <summary>
        /// Formats a string using double curly brackets {{param_member_name}} as templating placeholder.
        /// </summary>
        /// <param name="sourceText">The text to format</param>
        /// <param name="param">The parameter object as model to fetch the values</param>
        /// <param name="paramIdentifier">The identifier of the object for the template placeholder</param>
        /// <returns></returns>
        public static string Format(string sourceText, object param, string paramIdentifier = null)
        {
            string prefix = string.IsNullOrWhiteSpace(paramIdentifier) ? "" : paramIdentifier.Trim() + ".";
            string text = sourceText;

            if (!string.IsNullOrWhiteSpace(text) && param != null)
            {
                try
                {
                    var pattern = "\\{\\{[\\s]*([\\.0-9A-Za-z_]+)[\\s]*\\}\\}";
                    var regex = new Regex(pattern, RegexOptions.Compiled);
                    text = regex.Replace(text, (m) =>
                    {
                        if (!m.Success)
                            return m.Value;
                        if (m.Groups.Count < 2 || string.IsNullOrWhiteSpace(m.Groups[1].Value))
                            return m.Value;
                        var path = m.Groups[1].Value;
                        if (path.StartsWith(prefix) || prefix == "")
                        {
                            path = path.Substring(prefix.Length);
                            if (!string.IsNullOrWhiteSpace(path))
                            {
                                var val = RetrieveValue(path, param);
                                if (val == null) return m.Value;
                                try
                                {
                                    return val.ToString();
                                }
                                catch { }
                            }
                        }
                        return m.Value;
                    });
                }
                catch { }
            }

            return text;
        }

        /// <summary>
        /// Formats a string using double curly brackets {{param_member_name}} as templating placeholder.
        /// Each parameter passed will be assigned a param identifier which is the index of the parameter, starting from zero.
        /// </summary>
        /// <param name="sourceText">The text to format</param>
        /// <param name="parameters">The parameters to be used as object models</param>
        /// <returns></returns>
        public static string Format(string sourceText, params object[] parameters)
        {
            var text = sourceText;
            if (parameters != null && parameters.Length > 0)
            {
                var idx = 0;
                foreach (var o in parameters)
                {
                    if (o == null) { idx++; continue; }
                    try
                    {
                        text = Format(text, o, idx.ToString());
                    }
                    catch { }
                    idx++;
                }
            }
            return text;
        }

        public static string Format(string sourceText, Dictionary<string, object> parameters)
        {
            if (parameters == null) return sourceText;
            var txt = sourceText;
            foreach (var kv in parameters)
            {
                txt = Format(txt, kv.Value, kv.Key);
            }
            return txt;
        }

        public static class Charsets
        {
            public static string Number { get; } = "0123456789";
            public static string Numeric { get => Number + ".,-Ee+"; }
            public static string LowerAlpha { get; } = "abcdefghijklmnopqrstuvwxyz";
            public static string UpperAlpha { get => LowerAlpha.ToUpper(); }
            public static string Aplha { get => LowerAlpha + UpperAlpha; }
            public static string AlphaNumeric { get => LowerAlpha + UpperAlpha + Number; }
        }

        public static string GetCharInSet(string sourceText, IEnumerable<char> charset)
        {
            charset = charset ?? new char[0];
            sourceText = sourceText ?? "";
            string res = "";
            foreach (var c in sourceText)
            {
                if (charset.Contains(c)) res += c;
            }
            return res;
        }

        public static string FormatPhoneNumber(string phoneNumber, int countryCode = -1, bool addSeparator = false)
        {
            var pureNumber = GetCharInSet(phoneNumber, Charsets.Number + "+");
            if (pureNumber.Length > 2)
                pureNumber = pureNumber.Substring(0, 1) + pureNumber.Substring(1).Replace("+", ""); 

            if (pureNumber.StartsWith("0") && countryCode > 0) pureNumber = "+" + countryCode + pureNumber.Substring(1);

            return pureNumber;
        }
    }
}
