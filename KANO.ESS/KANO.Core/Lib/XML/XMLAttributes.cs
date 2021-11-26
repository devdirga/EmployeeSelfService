using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KANO.Core.Lib.XML
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class XMLElementAttribute : Attribute
    {
        public string LocalName { get; set; }
        public string Prefix { get; set; }
        public string Content { get; set; }
        public XMLElementAttribute(string ns, string localName)
        {
            Prefix = ns;
            LocalName = localName;
        }
        public XMLElementAttribute(string localName)
        {
            LocalName = localName;
        }
        public XMLElementAttribute()
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
    public class XMLNSAttribute : Attribute
    {
        public string xmlns { get; set; }
        public string Value { get; set; }
        public XMLNSAttribute(string ns, string value)
        {
            xmlns = ns;
            Value = value;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class XMLAttributeAttribute : Attribute
    {
        public string Name { get; set; }

        public XMLAttributeAttribute() { }

        public XMLAttributeAttribute(string name) { Name = name; }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class XMLUnwindAttribute:Attribute
    {

    }
}
