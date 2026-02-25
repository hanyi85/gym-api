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
            var subject = "請驗證您的帳號";

            var body = $@"
                <h2>帳號驗證</h2>
                <p>請點擊下方連結完成驗證：</p>
                <a href='{verifyLink}'>點我驗證</a>
            ";

            await SendEmail(toEmail, subject, body);
        }

        public async Task SendResetPasswordEmail(string toEmail, string resetLink)
        {
            var subject = "重設您的密碼";

            var body = $@"
                <h2>密碼重設</h2>
                <p>請點擊下方連結重設密碼：</p>
                <a href='{resetLink}'>重設密碼</a>
                <p>此連結 15 分鐘內有效</p>
            ";

            await SendEmail(toEmail, subject, body);
        }

        private async Task SendEmail(string toEmail, string subject, string body)
        {
            var smtp = _config.GetSection("SmtpSettings");

            var smtpClient = new SmtpClient(smtp["Host"])
            {
                Port = int.Parse(smtp["Port"]),
                Credentials = new NetworkCredential(
                    smtp["Username"],
                    smtp["Password"]
                ),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtp["From"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(toEmail);

            Console.WriteLine("==== 測試寄信 ====");
            Console.WriteLine($"To: {toEmail}");
            Console.WriteLine($"Subject: {subject}");

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}