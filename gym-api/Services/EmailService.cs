using System.Net;
using System.Net.Mail;

namespace gym_api.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendVerifyEmail(string toEmail, string verifyLink)
        {
            var smtp = _config.GetSection("SmtpSettings");

            var client = new SmtpClient(smtp["Host"])
            {
                Port = int.Parse(smtp["Port"]),
                Credentials = new NetworkCredential(
                    smtp["Username"],
                    smtp["Password"]
                ),
                EnableSsl = true
            };

            var mail = new MailMessage
            {
                From = new MailAddress(smtp["From"]),
                Subject = "請驗證您的帳號",
                Body = $"請點擊以下連結完成驗證：\n{verifyLink}",
                IsBodyHtml = false
            };

            mail.To.Add(toEmail);

            await client.SendMailAsync(mail);
        }
    }
}
