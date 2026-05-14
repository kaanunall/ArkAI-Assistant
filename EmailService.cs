using System.Net;
using System.Net.Mail;

namespace ArkAI
{
    public class EmailService
    {
        public static void SendVerificationCode(string userEmail, string code)
        {
            // GITHUB İÇİN GİZLENDİ: Kendi mail adresinizi buraya girin
            var fromAddress = new MailAddress("YOUR_EMAIL_ADDRESS_HERE", "Ark AI Destek");
            var toAddress = new MailAddress(userEmail);

            // GITHUB İÇİN GİZLENDİ: Google 16 haneli uygulama şifrenizi buraya girin
            string fromPassword = "YOUR_APP_PASSWORD_HERE";

            string subject = "🛡️ Ark AI | Güvenlik Protokolü Doğrulama Kodu";

            // Arabacı Temalı HTML Body
            string body = $@"
            <html>
            <body style='background-color: #0a0c10; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; color: #ffffff; margin: 0; padding: 0;'>
                <table width='100%' border='0' cellspacing='0' cellpadding='0' style='padding: 40px;'>
                    <tr>
                        <td align='center'>
                            <div style='max-width: 600px; background-color: #121419; border: 1px solid #00ffff; border-radius: 15px; padding: 30px; box-shadow: 0px 0px 20px rgba(0, 255, 255, 0.2);'>
                                
                                <h1 style='color: #00ffff; margin-bottom: 10px; font-size: 28px; letter-spacing: 2px;'>ARK AI SYSTEM</h1>
                                <div style='height: 2px; background: linear-gradient(to right, #00ffff, transparent); margin-bottom: 25px;'></div>

                                <p style='font-size: 16px; line-height: 1.6; color: #d1d1d1;'>
                                    Pilot, sisteme erişim veya şifre değişikliği için bir güvenlik protokolü tetiklendi.
                                    Aşağıdaki doğrulama kodunu terminale girerek işlemi tamamla:
                                </p>

                                <div style='background-color: #1c1f26; border-radius: 10px; padding: 20px; margin: 30px 0; border-left: 5px solid #00ffff;'>
                                    <span style='display: block; font-size: 12px; color: #00ffff; letter-spacing: 1px; margin-bottom: 5px;'>GÜVENLİK KODU //</span>
                                    <span style='font-family: ""Consolas"", monospace; font-size: 42px; font-weight: bold; color: #ffffff; letter-spacing: 8px;'>{code}</span>
                                </div>

                                <p style='font-size: 13px; color: #888888; margin-top: 25px;'>
                                    Eğer bu işlemi sen başlatmadıysan, aracının güvenlik ayarlarını kontrol etmeni öneririz. 
                                    Bu kod 10 dakika boyunca geçerlidir.
                                </p>

                                <div style='margin-top: 40px; border-top: 1px solid #2a2d35; padding-top: 20px;'>
                                    <small style='color: #444444;'>ARK AI | Advanced Road Keeper - 2026</small>
                                </div>
                            </div>
                        </td>
                    </tr>
                </table>
            </body>
            </html>";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
            };

            using (var message = new MailMessage(fromAddress, toAddress)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true // HTML olarak gönderilmesi için bu şart
            })
            {
                smtp.Send(message);
            }
        }
    }
}