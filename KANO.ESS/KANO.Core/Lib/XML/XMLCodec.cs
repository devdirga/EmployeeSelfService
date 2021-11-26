using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace KANO.Core.Lib.XML
{
    public static class XMLCodec
    {
        public static string DefaultDateTimeFormat { get; set; } = "yyyy-MM-ddTHH:mm:ss.1234567Z";
        public static TAttribute[] GetAttribute<TAttribute>(Type type) where TAttribute : Attribute
        {
            var res = type.GetCustomAttributes(typeof(TAttribute), true);
            if (res == null) return null;
            var ret = new List<TAttribute>();
            foreach (var itm in res)
            {
                if (itm is TAttribute a)
                    ret.Add(a);
            }
            return ret.ToArray();
        }
        public static TAttribute[] GetAttribute<TAttribute, TSource>() where TAttribute : Attribute
        {
            return GetAttribute<TAttribute>(typeof(TSource));
        }
        public static TAttribute[] GetAttribute<TAttribute>(PropertyInfo pi) where TAttribute : Attribute
        {
            var res = pi.GetCustomAttributes(typeof(TAttribute), true);
            if (res == null) return null;
            var ret = new List<TAttribute>();
            foreach (var itm in res)
            {
                if (itm is TAttribute a)
                    ret.Add(a);
            }
            return ret.ToArray();
        }

        public static Dictionary<PropertyInfo, XMLElementAttribute> GetElementProperties(object obj)
        {
            if (obj == null) return new Dictionary<PropertyInfo, XMLElementAttribute>();
            var res = new Dictionary<PropertyInfo, XMLElementAttribute>();
            var typ = obj.GetType();
            foreach (var pi in typ.GetProperties())
            {
                var attr = GetAttribute<XMLElementAttribute>(pi);
                if (attr.Length == 1)
                {
                    res.Add(pi, attr.First());
                    continue;
                }

                var tattr = GetAttribute<XMLElementAttribute>(pi.PropertyType);
                if (tattr.Length == 1)
                {
                    res.Add(pi, tattr.First());
                    continue;
                }

                res.Add(pi, null);
            }
            return res;
        }
        public static Dictionary<PropertyInfo, XMLAttributeAttribute> GetAttributeProperties(object obj)
        {
            if (obj == null) return new Dictionary<PropertyInfo, XMLAttributeAttribute>();
            var res = new Dictionary<PropertyInfo, XMLAttributeAttribute>();
            var typ = obj.GetType();
            foreach (var pi in typ.GetProperties())
            {
                var attr = GetAttribute<XMLAttributeAttribute>(pi);
                if (attr.Length == 1)
                {
                    res.Add(pi, attr.First());
                    continue;
                }

                var tattr = GetAttribute<XMLAttributeAttribute>(pi.PropertyType);
                if (tattr.Length == 1)
                {
                    res.Add(pi, tattr.First());
                }
            }
            return res;
        }

        public static string EncodeXML<T>(T obj, XmlWriterSettings settings = null)
        {
            using (var fo = new StringWriter())
            {
                XmlWriter writer = settings != null ? XmlWriter.Create(fo, settings) : XmlWriter.Create(fo);
                _EncodeTag(obj, writer);

                writer.Flush();

                return fo.ToString();
            }
        }

        private static string ToXMLString(object obj)
        {
            if (obj is DateTime dt) return dt.ToString(DefaultDateTimeFormat);
            if (obj is bool b) return b.ToString().ToLower();

            return obj?.ToString() ?? "";
        }

        private static void _EncodeTag<T>(T obj, XmlWriter writer, XMLElementAttribute forceTag = null)
        {
            if (obj == null)
                return;
            var typ = obj.GetType();

            XMLElementAttribute oTag = GetAttribute<XMLElementAttribute>(typ).FirstOrDefault();
            var xmltag = forceTag;
            if (xmltag != null && string.IsNullOrWhiteSpace(xmltag.LocalName))
            {
                xmltag.LocalName = typ.Name;
            }
            if (xmltag == null)
            {
                if (oTag != null && string.IsNullOrWhiteSpace(oTag.LocalName))
                    oTag.LocalName = typ.Name;
                xmltag = oTag;
            }
            // skip non tagged
            if (xmltag == null)
                return;

            // GetXMLNSs
            var xmlns = GetAttribute<XMLNSAttribute>(typ).ToList();

            // Write Start Element
            if (!string.IsNullOrWhiteSpace(xmltag.Prefix))
                writer.WriteStartElement(xmltag.Prefix, xmltag.LocalName, xmlns.Where(x => x.xmlns == xmltag.Prefix).FirstOrDefault()?.Value);
            else
                writer.WriteStartElement(xmltag.LocalName);

            // Write XMLNSs
            if (!string.IsNullOrWhiteSpace(xmltag.Prefix))
                xmlns = xmlns.Where(x => x.xmlns != xmltag.Prefix).ToList();
            foreach (var ns in xmlns)
            {
                writer.WriteAttributeString("xmlns", ns.xmlns, null, ns.Value);
            }

            if (oTag != null)
            {
                // Write Attributes
                var pis = typ.GetProperties();
                foreach (var pi in pis)
                {
                    if (pi.CanRead)
                    {
                        var atr = GetAttribute<XMLAttributeAttribute>(pi).FirstOrDefault();
                        if (atr != null)
                            _EncodeAttribute(pi.GetValue(obj), atr, pi.Name, writer);
                    }
                }
                if (!string.IsNullOrWhiteSpace(oTag.Content))
                {
                    // Write content instead of child
                    var pi = typ.GetProperty(oTag.Content);
                    if (pi != null && pi.CanRead)
                        writer.WriteString(pi.GetValue(obj)?.ToString() ?? "");
                    var fi = typ.GetField(oTag.Content);
                    if (fi != null)
                        writer.WriteString(fi.GetValue(obj)?.ToString() ?? "");
                }
                else
                {
                    // Write Child Elements
                    foreach (var pi in pis)
                    {
                        if (pi.CanRead)
                        {
                            var unw = GetAttribute<XMLUnwindAttribute>(pi).FirstOrDefault();
                            var tag = GetAttribute<XMLElementAttribute>(pi).FirstOrDefault();
                            if (tag != null && string.IsNullOrWhiteSpace(tag.LocalName))
                                tag.LocalName = pi.Name;
                            var val = pi.GetValue(obj);
                            if (unw != null)
                            {
                                if (val is IEnumerable enobj)
                                {
                                    foreach (var o in enobj)
                                    {
                                        _EncodeTag(o, writer, tag);
                                    }
                                }
                            }
                            else
                            {
                                _EncodeTag(val, writer, tag);
                            }
                        }
                    }
                }
            }
            else
            {
                writer.WriteString(ToXMLString(obj));
            }


            // Write End Element
            writer.WriteEndElement();
        }

        private static void _EncodeAttribute(object obj, XMLAttributeAttribute attribute, string defaultName, XmlWriter writer)
        {
            // Write Start Attribute
            writer.WriteStartAttribute(attribute.Name ?? defaultName);

            writer.WriteString(ToXMLString(obj));

            // Write End Attribute
            writer.WriteEndAttribute();
        }

        public static T DecodeXML<T>(string xml, XmlReaderSettings settings = null) where T : new()
        {
            using (var fi = new StringReader(xml))
            {
                XmlReader reader = settings != null ? XmlReader.Create(fi, settings) : XmlReader.Create(fi);
                var res = new T();

                // find first element
                if (!reader.EOF && reader.NodeType != XmlNodeType.Element)
                {
                    reader.Read();
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    _DecodeAttributes(res, reader);
                    if (reader.IsEmptyElement)
                        reader.Skip();
                    else
                        _DecodeNode(res, reader);
                }

                return res;
            }
        }

        private static void _DecodeNode(object context, XmlReader reader, XMLElementAttribute elementContext = null)
        {
            while (!reader.EOF)
            {
                reader.Read();
                // Decodes what the context contains
                if (reader.NodeType == XmlNodeType.Element)
                {
                    _DecodeElement(context, reader, elementContext);
                }
                else if (reader.NodeType == XmlNodeType.Text)
                {
                    _DecodeText(context, reader);
                }
                else if (reader.NodeType == XmlNodeType.EndElement)
                {
                    return;
                }
            }
        }

        private static void _DecodeText(object context, XmlReader reader)
        {
            var atr = GetAttribute<XMLElementAttribute>(context.GetType()).FirstOrDefault();
            if (atr != null)
            {
                var pi = context.GetType().GetProperty(atr.Content);
                if (pi != null && pi.CanWrite)
                {
                    try
                    {
                        pi.SetValue(context, reader.Value);
                    }
                    catch { }
                }
            }
        }

        private static void _DecodeElement(object parent, XmlReader reader, XMLElementAttribute elementContext = null)
        {
            // find matching property inside parent context
            var prop = GetElementProperties(parent).Where(p => p.Key.Name?.ToLower() == reader.LocalName?.ToLower() || p.Value?.LocalName?.ToLower() == reader.LocalName?.ToLower()).FirstOrDefault();
            var unw = prop.Key != null ? GetAttribute<XMLUnwindAttribute>(prop.Key).FirstOrDefault() : null;
            if (prop.Key != null && (prop.Value != null || unw != null) && prop.Key.CanWrite)
            {
                var ptype = prop.Key.PropertyType;
                if (ptype.GetInterface("IList") != null && unw != null)
                {
                    var col = prop.Key.GetValue(parent) ?? Activator.CreateInstance(ptype);
                    prop.Key.SetValue(parent, col);

                    if (col is IList li)
                    {
                        var nv = Activator.CreateInstance(col.GetType().GetGenericArguments().First());
                        li.Add(nv);
                        _DecodeAttributes(nv, reader);
                        _DecodeNode(nv, reader, prop.Value);
                    }
                }
                else if (_IsContentType(ptype))
                {
                    if (!reader.IsEmptyElement)
                    {
                        var val = reader.ReadInnerXml();
                        if (prop.Key.CanWrite)
                        {
                            prop.Key.SetValue(parent, val);
                            /*
                            reader.Read();
                            while (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace)
                                reader.Read();
                            if (reader.NodeType != XmlNodeType.EndElement)
                                throw new Exception("Expecting EndElement, " + reader.NodeType.ToString() + " found!");
                            */
                        }
                    }
                    else reader.Skip();
                }
                else
                {
                    var nc = Activator.CreateInstance(prop.Key.PropertyType);
                    prop.Key.SetValue(parent, nc);
                    _DecodeAttributes(nc, reader);
                    _DecodeNode(nc, reader, prop.Value);
                }
            }
            else
            {
                reader.Skip();
            }
        }

        private static bool _IsContentType(Type type)
        {
            return type == typeof(string) || type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float);
        }

        private static void _DecodeAttributes(object obj, XmlReader reader)
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                if (reader.AttributeCount <= 0) return;
                if (!reader.MoveToFirstAttribute())
                {
                    reader.MoveToElement();
                    return;
                }
            }
            if (reader.NodeType == XmlNodeType.Attribute)
            {
                var pis = GetAttributeProperties(obj).Where(a => a.Key?.Name.ToLower() == reader.Name?.ToLower() || a.Value.Name == reader.Name).FirstOrDefault();
                if (pis.Key != null && pis.Key.CanWrite)
                {
                    try
                    {
                        pis.Key.SetValue(obj, reader.Value);
                    }
                    catch { }
                }
                if (!reader.MoveToNextAttribute())
                {
                    reader.MoveToElement();
                    return;
                }
                else
                {
                    _DecodeAttributes(obj, reader);
                }
            }
        }
    }
}
