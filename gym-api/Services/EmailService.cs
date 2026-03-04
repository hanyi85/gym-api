using NuGet.Common;
using System.Net;
using System.Net.Mail;
using static gym_api.Services.EmailService;

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
            var subject = "【練吧 Fitness Bar】帳號驗證通知";

            var body = $@"
    <div style='font-family: Arial, sans-serif; line-height:1.6; color:#333;'>

        <h2 style='color:#f38d00;'>歡迎加入 練吧 Fitness Bar</h2>

        <p>親愛的會員您好：</p>

        <p>
        感謝您註冊 <strong>練吧 Fitness Bar</strong>。
        為保障您的帳號安全，請點擊下方按鈕完成信箱驗證：
        </p>

        <p style='margin:30px 0;'>
            <a href='{verifyLink}'
               style='background-color:#f38d00;
                      color:#ffffff;
                      padding:12px 24px;
                      text-decoration:none;
                      border-radius:6px;
                      display:inline-block;'>
                立即驗證帳號
            </a>
        </p>

        <p>
        若您無法點擊按鈕，請複製以下連結至瀏覽器開啟：
        </p>

        <p style='word-break:break-all; font-size:12px; color:#666;'>
            {verifyLink}
        </p>

        <hr style='margin:30px 0;' />

        <p style='font-size:12px; color:#999;'>
        此為系統自動發送信件，請勿直接回覆。<br/>
        若您並未申請帳號，請忽略此封信件。
        </p>

        <p style='font-size:12px; color:#999;'>
        © {DateTime.Now.Year} 練吧 Fitness Bar. All Rights Reserved.
        </p>

    </div>
    ";

            await SendEmail(toEmail, subject, body);
        }
        public async Task SendResetPasswordEmail(string toEmail, string resetLink)
        {
            var subject = "【練吧 Fitness Bar】密碼重設通知";

            var body = $@"
    <div style='font-family: Arial, sans-serif; line-height:1.6; color:#333;'>

        <h2 style='color:#f38d00;'>密碼重設申請</h2>

        <p>親愛的會員您好：</p>

        <p>
        我們收到一筆重設密碼的申請。
        若此操作為您本人所提出，請於 15 分鐘內點擊下方按鈕完成密碼重設：
        </p>

        <p style='margin:30px 0;'>
            <a href='{resetLink}'
               style='background-color:#f38d00;
                      color:#ffffff;
                      padding:12px 24px;
                      text-decoration:none;
                      border-radius:6px;
                      display:inline-block;'>
                重設密碼
            </a>
        </p>

        <p>
        若您無法點擊按鈕，請複製以下連結至瀏覽器開啟：
        </p>

        <p style='word-break:break-all; font-size:12px; color:#666;'>
            {resetLink}
        </p>

        <p style='margin-top:20px;'>
        此連結將於 <strong>15 分鐘後失效</strong>。<br/>
        若您未提出此申請，請忽略此信件，您的帳號仍然安全。
        </p>

        <hr style='margin:30px 0;' />

        <p style='font-size:12px; color:#999;'>
        此為系統自動發送信件，請勿直接回覆。
        </p>

        <p style='font-size:12px; color:#999;'>
        © {DateTime.Now.Year} 練吧 Fitness Bar. All Rights Reserved.
        </p>

    </div>
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


        public async Task SendOrderConfirmationEmail(string toEmail, string orderId, decimal totalAmount, string paymentMethod)
        {
            var subject = $"【練吧 Fitness Bar】訂單確認通知 - #{orderId}";

            // 對接 Vue 路由: /shop/orders/:id
            string detailLink = $"http://localhost:5173/shop/orders/{orderId}";

            var body = $@"
<div style='font-family: Arial, sans-serif; line-height:1.6; color:#333;'>

    <h2 style='color:#f38d00;'>感謝您的訂購！</h2>

    <p>親愛的會員您好：</p>

    <p>
    我們已收到您的訂單 <strong>#{orderId}</strong>。
    團隊正在為您準備後續作業，您可以點擊下方按鈕查看詳細訂單內容：
    </p>

    <div style='background-color:#f9f9f9; padding:20px; border-radius:6px; margin:25px 0;'>
        <p style='margin:5px 0;'><strong>付款方式：</strong> {paymentMethod}</p>
        <p style='margin:5px 0;'><strong>應付總額：</strong> <span style='color:#e74c3c; font-size:18px; font-weight:bold;'>NT$ {totalAmount:N0}</span></p>
    </div>

    <p style='margin:30px 0;'>
        <a href='{detailLink}'
           style='background-color:#f38d00;
                  color:#ffffff;
                  padding:12px 24px;
                  text-decoration:none;
                  border-radius:6px;
                  display:inline-block;
                  font-weight:bold;'>
            查看訂單詳情
        </a>
    </p>

    

    

    <hr style='margin:30px 0;' />

    <p style='font-size:12px; color:#999;'>
    此為系統自動發送信件，請勿直接回覆。<br/>
    若您對訂單有任何疑問，歡迎聯繫官方客服。
    </p>

    <p style='font-size:12px; color:#999;'>
    © {DateTime.Now.Year} 練吧 Fitness Bar. All Rights Reserved.
    </p>

</div>
";

            await SendEmail(toEmail, subject, body);
        }
    }
}