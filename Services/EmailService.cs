using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Mail;

namespace PharmaDistributionApp.Services
{
    public class EmailService
    {
        private string senderEmail = "khanghuynh3467@gmail.com";
        private string senderPassword = "nmhp rxre bxgd qyhq";
        
        public string GenerateOTP()
        {
            Random rand = new Random();
            int num = rand.Next(0, 1000000);
            return num.ToString("D6");
        }
        public void SendVerificationCode(string receiverEmail, string otpCode)
        {
            try
            {
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(senderEmail);
                mail.To.Add(receiverEmail);
                mail.Subject = "Mã xác minh lấy lại mật khẩu";
                mail.Body = $"Mã xác minh của bạn là: <h2>{otpCode}</h2> <p>Mã này có hiệu lực trong 5 phút.</p>";
                mail.IsBodyHtml = true; 

                SmtpClient smtp = new SmtpClient("smtp.gmail.com");
                smtp.Port = 587; 
                smtp.EnableSsl = true;
                smtp.Credentials = new NetworkCredential(senderEmail, senderPassword);

                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                throw ex; 
            }
        }
    }
}
