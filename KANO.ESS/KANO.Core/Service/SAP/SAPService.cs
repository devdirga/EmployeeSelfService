using KANO.Core.Lib.XML;
using KANO.Core.Model;
using KANO.Core.Service.SAP.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace KANO.Core.Service.SAP
{
    public class SAPService
    {
        public string APIEndPoint { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Domain { get; set; }

        public SAPService(string apiEndPoint, string username, string password)
        {
            APIEndPoint = apiEndPoint;
            Username = username;
            Password = password;
        }
        
        public SAPService(IConfiguration configuration)
        {
            APIEndPoint = configuration["SAP:APIEndPoint"];
            Username = configuration["SAP:Username"];
            Password = configuration["SAP:Password"];
        }

        public SAPService() { }

        private HttpClient CreateRequest()
        {
            var client = new HttpClient();
            var byteArray = Encoding.ASCII.GetBytes(Username + ":" + Password);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
            if (!string.IsNullOrWhiteSpace(Domain))
                client.DefaultRequestHeaders.Host = Domain;
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("CoHive-CTM", "1.0"));
            return client;
        }

        public async Task<HttpResponseMessage> SendRequest(SAPRequestBase request)
        {
            var client = CreateRequest();
            var body = request.BuildXML();
            var resp = await client.PostAsync(APIEndPoint, new StringContent(body, Encoding.UTF8, "application/soap+xml"));
            return resp;
        }
        public async Task<HttpResponseMessage> SendRequest<TBody>(SOAPEnvelope<TBody> data)
        {
            var client = CreateRequest();
            var body = XMLCodec.EncodeXML(data, new System.Xml.XmlWriterSettings() { OmitXmlDeclaration = true });
            var resp = await client.PostAsync(APIEndPoint, new StringContent(body, Encoding.UTF8, "application/soap+xml"));
            return resp;
        }
    }

    public abstract class SAPElementBase
    {
        [XMLAttribute(Name = "actionCode")]
        public string ActionCode { get; set; } = "01";

        public void SetActionCode(string code)
        {
            var elebase = typeof(SAPElementBase);
            var typ = this.GetType();
            foreach (var pi in typ.GetProperties())
            {
                if (pi.PropertyType.IsSubclassOf(elebase) && pi.CanRead)
                {
                    if (pi.GetValue(this) is SAPElementBase seb)
                        seb.SetActionCode(code);                    
                }
            }
            foreach (var fi in typ.GetFields())
            {
                if (fi.FieldType.IsSubclassOf(elebase))
                {
                    if (fi.GetValue(this) is SAPElementBase seb)
                        seb.SetActionCode(code);
                }
            }
            this.ActionCode = code;
        }
    }

    public abstract class SAPRequestBase
    {
        protected abstract string XMLTemplateName { get; }
        public virtual string BuildXML()
        {
            var template = GetXMLTemplate();
            if (template == null) return null;
            return Templating.Format(template, this);
        }

        public string GetXMLTemplate()
        {
            string contentRootPath = Directory.GetCurrentDirectory();
            var template = System.IO.Path.Combine(contentRootPath, "wwwroot", "assets", "sap", "xml", XMLTemplateName + ".xml");
            if (File.Exists(template))
            {
                using (var fi = File.OpenText(template))
                {
                    return fi.ReadToEnd();
                }
            }
            return null;
        }
    }

    public class SAPManageServiceProduction : SAPRequestBase
    {
        public string InternalID { get; set; }
        public string ProductCategoryID { get; set; }
        public string BaseMeasureUnitCode { get; set; }
        public string ValuationMeasureUnitCode { get; set; }
        public string Description { get; set; }
        public string SalesOrganisationID { get; set; }
        public string DistributionChannelCode { get; set; }
        public string LifeCycleStatusCode { get; set; }
        public string SalesMeasureUnitCode { get; set; }
        public string ItemGroupCode { get; set; }
        public string CompanyID { get; set; }

        protected override string XMLTemplateName => "ServiceProductBundleMaintainRequest";
    }

}
