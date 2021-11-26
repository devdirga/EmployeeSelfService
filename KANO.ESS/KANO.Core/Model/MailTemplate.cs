using KANO.Core.Lib.Extension;
using KANO.Core.Service;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace KANO.Core.Model
{
    [Collection("MailTemplate")]
    public class MailTemplate
    {
        [BsonId]
        public string Id { get; set; } = "";
        public DateTime LastUpdate { get; set; } = DateTime.Now;
        public string Subject { get; set; } = "";
        public string Body { get; set; } = "";
        public List<string> MailTo { get; set; } = new List<string>();
        public List<string> MailCC { get; set; } = new List<string>();
        public string SMTPConfig { get; set; } = "Default";
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Gets a list of attachment that will be sent along with the template
        /// </summary>
        [BsonIgnore]
        public List<Attachment> Attachments { get; } = new List<Attachment>();

        public MailMessage Build(Dictionary<string, object> parameters = null)
        {
            var res = new MailMessage();
            res.Body = Body;
            res.Subject = Subject;
            foreach (var att in Attachments)
            {
                res.Attachments.Add(att);
            }
            foreach (var recp in MailTo)
                if (!string.IsNullOrWhiteSpace(recp)) res.To.Add(new MailAddress(recp.Trim().ToLower()));
            foreach (var recp in MailCC)
                if (!string.IsNullOrWhiteSpace(recp)) res.CC.Add(new MailAddress(recp.Trim().ToLower()));
            if (parameters != null && parameters.Count > 0)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(res.Body))
                    {
                        res.Body = Templating.Format(res.Body, parameters);
                    }
                }
                catch { }
                try
                {
                    if (!string.IsNullOrWhiteSpace(res.Subject))
                    {
                        res.Subject = Templating.Format(res.Subject, parameters);
                    }
                }
                catch { }
            }
            return res;
        }
        public MailMessage Build<T>(T param)
        {
            var res = new MailMessage();
            res.Body = Body;
            res.Subject = Subject;
            foreach (var att in Attachments)
            {
                res.Attachments.Add(att);
            }
            foreach (var recp in MailTo)
                if (!string.IsNullOrWhiteSpace(recp)) res.To.Add(new MailAddress(recp.Trim().ToLower()));
            foreach (var recp in MailCC)
                if (!string.IsNullOrWhiteSpace(recp)) res.CC.Add(new MailAddress(recp.Trim().ToLower()));
            if (param != null)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(res.Body))
                    {
                        res.Body = Templating.Format(res.Body, param);
                    }
                }
                catch { }
                try
                {
                    if (!string.IsNullOrWhiteSpace(res.Subject))
                    {
                        res.Subject = Templating.Format(res.Subject, param);
                    }
                }
                catch { }
            }
            return res;
        }
        public MailMessage Build(params object[] parameters)
        {
            var res = new MailMessage();
            res.Body = Body;
            res.Subject = Subject;
            foreach (var att in Attachments)
            {
                res.Attachments.Add(att);
            }
            foreach (var recp in MailTo)
                if (!string.IsNullOrWhiteSpace(recp)) res.To.Add(new MailAddress(recp.Trim().ToLower()));
            foreach (var recp in MailCC)
                if (!string.IsNullOrWhiteSpace(recp)) res.CC.Add(new MailAddress(recp.Trim().ToLower()));
            if (parameters != null && parameters.Length > 0)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(res.Body))
                    {
                        res.Body = Templating.Format(res.Body, parameters);
                    }
                }
                catch { }
                try
                {
                    if (!string.IsNullOrWhiteSpace(res.Subject))
                    {
                        res.Subject = Templating.Format(res.Subject, parameters);
                    }
                }
                catch { }
            }
            return res;
        }
    }
}
