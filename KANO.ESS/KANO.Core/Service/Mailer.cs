/*
 * 
 *  Usage Example:
 *  
    // ==== Let's say we want to welcome a new Tenant and send him/her an email.
    // ==== Get or create a tenant data:
    var param = new Tenant();
    param.Name = "Alice Silverlake";
    param.Code = "120558487530";

    // ==== Create the mailer
    var mailer = new Mailer(Mongo, Configuration);

    // ==== If you want to send the template as is (it will use recipient list from db), use this:
    bool res = await mailer.SendTemplateAsync("TestTemplate", param);

    // ==== If you want to add more recipients or CCs (this will merge recipients from db and the one you provide), use this:
    bool res = await mailer.SendTemplateAsync("TestTemplate", param, new string[] { "rachmad.sulaiman@eaciit.com" }, new string[] { "admin@eaciit.com" });

    // ==== If you want to further modify the template, use this: 
    var tmp = mailer.GetTemplate("TestTemplate");
    tmp.MailTo.Add("rachmad.sulaiman@eaciit.com");
    tmp.MailCC.Add("admin@eaciit.com");
    tmp.Body += "<br>Thank you";
    bool res = await mailer.SendTemplateAsync(tmp, param);
 * 
 * 
 * Notes: 
 * - Parameter type doesn't matter as long as it is a non-null object. 
 * - The template uses {{id}} as a placeholder. Please do not leave leading or trailing whitespaces.
 * - The id is retrieved from the passed parameter properties. For example: if Tenant object has Name property, you can use {{Name}} as a placeholder in the template.
 * 
 * */


using KANO.Core.Lib.Extension;
using KANO.Core.Model;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;

namespace KANO.Core.Service
{
    public class Mailer
    {
        private IMongoManager Mongo;
        private IMongoDatabase DB;
        public IConfiguration Configuration;

        /// <summary>
        /// Gets a list of attachments that will be sent for every Send calls
        /// </summary>
        public List<Attachment> Attachments { get; } = new List<Attachment>();
        /// <summary>
        /// Gets a list of the mail recipients. The addresses in this list will be appended to To field of MailMessage.
        /// </summary>
        public List<string> To { get; } = new List<string>();
        /// <summary>
        /// Gets a list of the mail CC. The addresses in this list will be appended to CC field of MailMessage.
        /// </summary>
        public List<string> CC { get; } = new List<string>();
        /// <summary>
        /// Gets a list of the mail BCC. The addresses in this list will be appended to Bcc field of MailMessage.
        /// </summary>
        public List<string> BCC { get; } = new List<string>();
        /// <summary>
        /// Gets a list of the mail reply address. The addresses in this list will be appended to ReplyTo field of MailMessage.
        /// </summary>
        public List<string> ReplyTo { get; } = new List<string>();
        /// <summary>
        /// Gets or sets the SMTP config name to load mail SMTP configuration from.
        /// </summary>
        public string SMTPConfigName { get; set; } = "Default";

        /// <summary>
        /// Gets a collection of named template parameters that will be use by the template engine to retrieve the value from.
        /// </summary>
        public Dictionary<string, object> TemplateParameters { get; } = new Dictionary<string, object>();

        public Mailer(IConfiguration _Configuration)
        {
            Configuration = _Configuration;
        }

        public Mailer(IMongoManager mongo, IConfiguration _Configuration)
        {
            Mongo = mongo;
            DB = Mongo.Database();
            Configuration = _Configuration;
        }

        /// <summary>
        /// Send an email.
        /// </summary>
        /// <param name="message">The mail message to send</param>
        /// <returns></returns>
        /// 
        public bool SendMail(MailMessage message)
        {
            var _user = Configuration["SMTP:SMTP_UserName_" + SMTPConfigName];
            var _pass = Configuration["SMTP:SMTP_Password_" + SMTPConfigName];
            var _from = Configuration["SMTP:SMTP_From_" + SMTPConfigName];

            return SendMail(message, _user, _pass, _from);
        }

        public bool SendMail(MailMessage message, string UserName, String Password)
        {
            var _from = Configuration["SMTP:SMTP_From_" + SMTPConfigName];

            return SendMail(message, UserName, Password, _from);
        }

        public bool SendMail(MailMessage message, string UserName, String Password, String From)
        {
            if (message == null) throw new ArgumentNullException("message");
            try
            {
                foreach (var att in Attachments) message.Attachments.Add(att);
                foreach (var to in To) message.To.Add(to);
                foreach (var cc in CC) message.CC.Add(cc);
                foreach (var bcc in BCC) message.Bcc.Add(bcc);
                foreach (var replyTo in ReplyTo) message.ReplyToList.Add(replyTo);

                var _host = Configuration["SMTP:SMTP_Host_" + SMTPConfigName];
                var _port = Convert.ToInt32(Configuration["SMTP:SMTP_Port_" + SMTPConfigName]);
                var _from = From;
                var _user = UserName;
                var _pass = Password;
                var _ssl = Convert.ToBoolean(Configuration["SMTP:SMTP_UseSSL_" + SMTPConfigName]);

                message.From = new MailAddress(_from);
                message.IsBodyHtml = true;

                using (var client = new SmtpClient(_host, _port))
                {
                    client.ServicePoint.MaxIdleTime = 1;
                    client.ServicePoint.ConnectionLimit = 1;
                    if (_ssl) client.EnableSsl = true;
                    if (!string.IsNullOrEmpty(_user))
                    {
                        client.UseDefaultCredentials = false;
                        var cred = new System.Net.NetworkCredential(_user, _pass);
                        client.Credentials = cred;
                    }
                    client.Send(message);
                }
            } 
            catch (Exception e)
            {
                throw e;
                return false;
            }
            return true;
        }
        /// <summary>
        /// Send an email asynchronously.
        /// </summary>
        /// <param name="message">The mail message to send</param>
        /// <returns></returns>
        public Task<bool> SendMailAsync(MailMessage message)
        {
            return Task.Run(() =>
            {
                return SendMail(message);
            });
        }

        /// <summary>
        /// Gets a mail template from the database.
        /// </summary>
        /// <param name="templateId">The id of the template.</param>
        /// <returns>MailTemplate object if succeed, null otherwise.</returns>
        public MailTemplate GetTemplate(string templateId)
        {
            if (templateId == null) return null;
            return DB.GetCollection<MailTemplate>().Find(a => a.Id == templateId).FirstOrDefault();
        }

        /// <summary>
        /// Save a template to the database.
        /// </summary>
        public bool SaveTemplate(MailTemplate template)
        {
            if (template == null) throw new ArgumentNullException("template");
            if (string.IsNullOrEmpty(template.Id)) throw new ArgumentNullException("template.Id");
            try
            {
                DB.Save(template);
                return true;
            }
            catch { }
            return false;
        }

        /// <summary>
        /// Creates a new empty template.
        /// </summary>
        public MailTemplate NewTemplate()
        {
            return new MailTemplate();
        }

        public bool SendTemplate(MailTemplate template)
        {
            if (template == null) return false;
            var mail = template.Build(TemplateParameters);
            return SendMail(mail);
        }

        /// <summary>
        /// Sends an email using template object with specified param.
        /// </summary>
        /// <typeparam name="T">Any non-null object</typeparam>
        /// <param name="template">The template object to send</param>
        /// <param name="param">The parameter object to get data from</param>
        /// <param name="SMTPConfig">The smtp configuration name</param>
        /// <returns>True if succeed, false otherwise</returns>
        public bool SendTemplate<T>(MailTemplate template, T param) 
        {
            if (template == null) return false;

            var mail = template.Build(param);

            return SendMail(mail);
        }
        /// <summary>
        /// Sends an email using template with specified template id and param.
        /// </summary>
        /// <typeparam name="T">Any non-null object</typeparam>
        /// <param name="templateId">The id of the template</param>
        /// <param name="param">The parameter object to get data from</param>
        /// <param name="SMTPConfig">The smtp configuration name</param>
        /// <returns></returns>
        public bool SendTemplate<T>(string templateId, T param)
        {
            var tmp = GetTemplate(templateId);
            if (tmp == null) return false;
            return SendTemplate(tmp, param);
        }

        /// <summary>
        /// Sends an email using template object with specified param.
        /// </summary>
        /// <typeparam name="T">Any non-null object</typeparam>
        /// <param name="template">The template object to send</param>
        /// <param name="param">The parameter object to get data from</param>
        /// <param name="SMTPConfig">The smtp configuration name</param>
        /// <returns>True if succeed, false otherwise</returns>
        public Task<bool> SendTemplateAsync<T>(MailTemplate template, T param)
        {
            return Task.Run(() =>
            {
                return SendTemplate(template, param);
            });
        }
        /// <summary>
        /// Sends an email using template with specified template id and param.
        /// </summary>
        /// <typeparam name="T">Any non-null object</typeparam>
        /// <param name="templateId">The id of the template</param>
        /// <param name="param">The parameter object to get data from</param>
        /// <param name="SMTPConfig">The smtp configuration name</param>
        /// <returns></returns>
        public Task<bool> SendTemplateAsync<T>(string templateId, T param)
        {
            return Task.Run(() =>
            {
                return SendTemplate(templateId, param);
            });
        }
    }
}
