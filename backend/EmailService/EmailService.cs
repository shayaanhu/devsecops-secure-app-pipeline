using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CarpoolApp.Server.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var password = _config["EmailSettings:Password"];

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine($"[OTP] {toEmail}: {otp}");
                return;
            }

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(senderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = "Your OTP Code";
            email.Body = new TextPart("plain") { Text = $"Your OTP is: {otp}" };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(senderEmail, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

    }
}

