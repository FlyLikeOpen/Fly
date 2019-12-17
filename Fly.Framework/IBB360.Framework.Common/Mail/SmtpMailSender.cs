using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Fly.Framework.Common
{
    public static class SmtpMailSender
    {
        private static MailMessage BuildMailMessage(string fromAddress, string fromDisplay, string[] toAddressList,
            string[] ccAddressList, string[] bccAddressList, string subject, string body,
            string[] attachmentFilePathList, Encoding charset, bool isBodyHtml, MailPriority priority)
        {
            MailMessage emailInfo = new MailMessage();
            emailInfo.From = new MailAddress(fromAddress, fromDisplay, charset);
            if (toAddressList != null && toAddressList.Length > 0)
            {
                foreach (string mailto in toAddressList)
                {
                    if (!string.IsNullOrWhiteSpace(mailto))
                    {
                        emailInfo.To.Add(mailto);
                    }
                }
            }

            if (ccAddressList != null && ccAddressList.Length > 0)
            {
                foreach (string cc in ccAddressList)
                {
                    if (!string.IsNullOrWhiteSpace(cc))
                    {
                        emailInfo.CC.Add(cc);
                    }
                }
            }

            if (bccAddressList != null && bccAddressList.Length > 0)
            {
                foreach (string bcc in bccAddressList)
                {
                    if (!string.IsNullOrWhiteSpace(bcc))
                    {
                        emailInfo.Bcc.Add(bcc);
                    }
                }
            }

            if (attachmentFilePathList != null && attachmentFilePathList.Length > 0)
            {
                foreach (string file in attachmentFilePathList)
                {
                    if (!string.IsNullOrWhiteSpace(file))
                    {
                        emailInfo.Attachments.Add(new Attachment(file));
                    }
                }
            }

            emailInfo.Subject = subject;
            emailInfo.Body = body;
            emailInfo.Priority = priority;
            emailInfo.IsBodyHtml = isBodyHtml;
            emailInfo.BodyEncoding = charset;
            emailInfo.SubjectEncoding = charset;
            return emailInfo;
        }

        public static void SendMail(string fromAddress, string fromDisplay, string[] toAddressList,
            string[] ccAddressList, string[] bccAddressList, string subject, string body,
            string[] attachmentFilePathList, Encoding charset, bool isBodyHtml, MailPriority priority,
            string address, int port, string username, string password, bool enableSsl)
        {
            var emailInfo = BuildMailMessage(fromAddress, fromDisplay, toAddressList, ccAddressList, bccAddressList, subject, body, attachmentFilePathList,
                charset, isBodyHtml, priority);
            SmtpClient smtpMail = new SmtpClient(address, port);
            smtpMail.Credentials = new NetworkCredential(username, password);
            smtpMail.EnableSsl = enableSsl;
            smtpMail.Send(emailInfo);
        }

        public static void SendMailAsync(string fromAddress, string fromDisplay, string[] toAddressList,
            string[] ccAddressList, string[] bccAddressList, string subject, string body,
            string[] attachmentFilePathList, Encoding charset, bool isBodyHtml, MailPriority priority,
            string address, int port, string username, string password, bool enableSsl, Action<AsyncCompletedEventArgs> callback = null, object userToken = null)
        {
            var emailInfo = BuildMailMessage(fromAddress, fromDisplay, toAddressList, ccAddressList, bccAddressList, subject, body, attachmentFilePathList,
                charset, isBodyHtml, priority);
            SmtpClient smtpMail = new SmtpClient(address, port);
            smtpMail.Credentials = new NetworkCredential(username, password);
            smtpMail.EnableSsl = enableSsl;
            if (callback != null)
            {
                smtpMail.SendCompleted += (s, e) =>
                {
                    if (callback != null)
                    {
                        callback(e);
                    }
                };
            }
            smtpMail.SendAsync(emailInfo, userToken);
        }
    }
}
