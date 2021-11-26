using KANO.Core.Lib.XML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace KANO.Core.Service.SAP.Models
{
    [XMLElement("soap", "Envelope")]
    [XMLNS("soap", "http://www.w3.org/2003/05/soap-envelope")]
    [XMLNS("glob", "http://sap.com/xi/SAPGlobal20/Global")]
    [XMLNS("y6m", "http://0030096872-one-off.sap.com/Y6M7YOMOY_")]
    [XMLNS("glob1", "http://sap.com/xi/AP/Globalization")]
    public class SOAPEnvelope<TBodyContent>
    {
        public SOAPHeader Header { get; } = new SOAPHeader();
        public SOAPBody<TBodyContent> Body { get; set; } = new SOAPBody<TBodyContent>();
    }

    [XMLElement("soap", "Header")]
    public class SOAPHeader
    {

    }

    [XMLElement("soap", "Body")]
    public class SOAPBody<T>
    {
        public T Content { get; set; }
    }

    [XMLElement("BasicMessageHeader")]
    public class SOAPBasicMessageHeader
    {

    }

    [XMLElement("BusinessDocumentBasicMessageHeader")]
    public class BusinessDocumentBasicMessageHeader
    {

    }

}
