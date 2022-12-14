using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;

namespace WMS_BE.Utils
{
    public class Mailing
    {
        public string smtp_from = ConfigurationManager.AppSettings["smtp_from"].ToString();
        public string smtp_from_alias = ConfigurationManager.AppSettings["smtp_from_alias"].ToString();
        public string smtp_username = ConfigurationManager.AppSettings["smtp_username"].ToString();
        public string smtp_password = ConfigurationManager.AppSettings["smtp_password"].ToString();
        public string smtp_host = ConfigurationManager.AppSettings["smtp_host"].ToString();
        public int smtp_port = Convert.ToInt32(ConfigurationManager.AppSettings["smtp_port"].ToString());

        public string forgot_pass_recipient = ConfigurationManager.AppSettings["forgot_pass_recipient"].ToString();


        public void SendEmail(List<string> recipients, String subject, String body)
        {
            using (var mail = new SmtpClient())
            {
                mail.Host = smtp_host;
                mail.Port = smtp_port;
                mail.DeliveryMethod = SmtpDeliveryMethod.Network;
                mail.Credentials =
                   new NetworkCredential(smtp_username, smtp_password);

                MailMessage message = new MailMessage();
                message.IsBodyHtml = true;
                message.From = new MailAddress(smtp_username, smtp_from_alias);

                foreach (string recipient in recipients)
                {
                    message.To.Add(recipient);
                }

                message.Subject = subject;
                message.Body = body;
                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.SubjectEncoding = System.Text.Encoding.UTF8;
                message.Priority = MailPriority.High;

                try
                {
                    //await mail.SendMailAsync(message);
                    mail.Send(message);
                }
                catch (SmtpException ex)
                {
                    String errMsg = string.Format("SendEmail - SmtpException : {0}", ex.Message);
                }
                catch (Exception ex)
                {
                    String errMsg = string.Format("SendEmail - SmtpException : {0}", ex.Message);
                }
            }
        }


        public bool SendSimpleEmail(String subject, String body)
        {
            bool result = false;
            using (var mail = new SmtpClient())
            {
                mail.Host = smtp_host;
                mail.Port = smtp_port;
                mail.DeliveryMethod = SmtpDeliveryMethod.Network;
                mail.Credentials =
                   new NetworkCredential(smtp_username, smtp_password);
                mail.EnableSsl = true;

                MailMessage message = new MailMessage();
                message.IsBodyHtml = true;
                message.From = new MailAddress(smtp_username, smtp_from_alias);

                message.To.Add(forgot_pass_recipient);

                message.Subject = subject;
                message.Body = body;
                message.BodyEncoding = System.Text.Encoding.UTF8;
                message.SubjectEncoding = System.Text.Encoding.UTF8;
                message.Priority = MailPriority.High;

                try
                {
                    mail.Send(message);
                    result = true;
                }
                catch (SmtpException ex)
                {
                    String errMsg = string.Format("SendEmail - SmtpException : {0}", ex.Message);
                }
                catch (Exception ex)
                {
                    String errMsg = string.Format("SendEmail - SmtpException : {0}", ex.Message);
                }
            }

            return result;
        }
    }
}