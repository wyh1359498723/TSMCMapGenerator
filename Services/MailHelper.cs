using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Serilog;
using System.Configuration;

namespace TSMCMapGenerator.Services;

public class MailHelper
{
     public static Tuple<bool,string> SendMail(string mailSubject, string mailContent, List<string> mailReceiver,List<string>? mailCc=null , string? fileNames = null)
    {
        bool result = true;
        string msg = string.Empty;

        try
        {
            SmtpClient smtpClient = new SmtpClient();
            MailMessage mailMessage = new MailMessage();

            smtpClient.Host = ConfigurationManager.AppSettings["MailHost"];
            var mailname = ConfigurationManager.AppSettings["MailUsername"];
            var mailpsd = ConfigurationManager.AppSettings["MailPassword"];
            
            smtpClient.Port = int.Parse(ConfigurationManager.AppSettings["MailPort"]);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new System.Net.NetworkCredential(mailname, mailpsd);
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            mailMessage.BodyEncoding = Encoding.UTF8;
            mailMessage.IsBodyHtml = true;
            mailMessage.Priority = MailPriority.High;

            mailMessage.From = new MailAddress(mailname);
            
            foreach (string mailtoads in mailReceiver)
                mailMessage.To.Add(mailtoads);
            if (mailCc?.Count > 0)
            {
                foreach (string mailccads in mailCc)
                {
                    if (!string.IsNullOrEmpty(mailccads))
                        mailMessage.CC.Add(mailccads);
                }
            }
            mailMessage.Subject = mailSubject;
            
            mailMessage.Attachments.Clear();

            if (fileNames != null)
                mailMessage.Attachments.Add(new Attachment(fileNames, MediaTypeNames.Application.Octet));

            mailMessage.Body = GetBody(mailContent);
            smtpClient.Send(mailMessage);
            
        }
        catch (Exception ex)
        {
            result = false;
            Log.Error("邮件发送失败：{Subject}\r\n{Content}", mailSubject, mailContent);
            msg = ex.Message + "----" + ex.StackTrace; 
            Log.Error("邮件发送异常：{Message}", msg);
        }
        
        return new Tuple<bool, string>(result, msg);
    }
    
    
    private static string GetBody(string content)
    {
        var htmlBody = new StringBuilder();
        htmlBody.Append("<body style=\"font-size:10pt\">");
        htmlBody.Append("<div style=\"font-size:10pt; font-weight:bold\">各位好：</div>");
        htmlBody.Append("<br/>");
        htmlBody.Append("<div style=\"margin-left:5px;\">" + content + "</div>");
        htmlBody.Append("<br/>");
        htmlBody.Append("<div>" + DateTime.Now.Year + "年" + DateTime.Now.Month + "月" + DateTime.Now.Day + "日</div>");
        htmlBody.Append("<div>此邮件为系统自动发送，请勿回复!</div></body>");
        return htmlBody.ToString();
    }
}
