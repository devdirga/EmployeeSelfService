using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace KANO.Core.Lib
{
    public class Email
    {
        public IConfiguration Configuration { get; }


        private string _host { get; set; }
        private int _port { get; set; }
        private string _from { get; set; }
        private string _user { get; set; }
        private string _pass { get; set; }
        private bool _ssl { get; set; }

        public Email(IConfiguration iConfig)
        {
            Configuration = iConfig;

            _host = Configuration["SMTP:SMTP_Host_Default"];
            _port = Convert.ToInt32(Configuration["SMTP:SMTP_Port_Default"]);
            _from = Configuration["SMTP:SMTP_From_Default"];
            _user = Configuration["SMTP:SMTP_UserName_Default"];
            _pass = Configuration["SMTP:SMTP_Password_Default"];
            _ssl = Convert.ToBoolean(Configuration["SMTP:SMTP_UseSSL_Default"]);
        }

        public bool SendEmail(string resetUrl, string[] toMails, out string message)
        {

            string Subject = "Reset your WEIS Password";
            String Body = "Hi,\n\nTo change your password please go to following link: " + resetUrl + "\n\nIf you have not requested to change your password, then please ignore this message.\n\nThanks";
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(_from);

            var xtomail = new List<string>();
            if (toMails != null && toMails.Count() > 0)
            {
                foreach (var t in toMails.ToList())
                {
                    if (!t.Trim().Equals(""))
                    {
                        mailMessage.To.Add(t);

                    }
                }
            }
            mailMessage.Body = Body;
            mailMessage.Subject = Subject;

            //var resp = Tools.SendMail(SMTPConfig, Subject, msgBody, xtomail.ToArray(), xccmail.ToArray(),

            System.Net.Mail.SmtpClient client = null;

            bool response =  false;
            try
            {
                using (client = new System.Net.Mail.SmtpClient(_host, _port))
                {
                    client.ServicePoint.MaxIdleTime = 1;
                    client.ServicePoint.ConnectionLimit = 1;
                    //client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    if (_ssl) client.EnableSsl = true;
                    if (String.IsNullOrEmpty(_user) == false)
                    {
                        client.UseDefaultCredentials = false;
                        //var credCache = new System.Net.CredentialCache();
                        //credCache.Add(host,port,"",new System.Net.NetworkCredential(userName, password));
                        //client.Credentials = credCache;
                        var cred = new System.Net.NetworkCredential(_user, _pass);
                        client.Credentials = cred;
                    }
                    client.Send(mailMessage);
                    mailMessage.Dispose();
                }
                message = "Email Sent";
                response =  true;
            }
            catch (Exception ex)
            {
                mailMessage.Dispose();
                throw ex;
            }
            return response;
        }

    }
}
