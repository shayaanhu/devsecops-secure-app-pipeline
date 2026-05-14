using MailKit.Net.Smtp;
using MimeKit;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CarpoolApp.Server.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string toEmail, string otp)
        {
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var senderPassword = _config["EmailSettings:Password"];

            // In development or if credentials are missing, just log the OTP instead
            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogWarning($"Email service is not configured. OTP for {toEmail}: {otp}");
                return;
            }

            try
            {
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(senderEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = "Your UniRide OTP Code";
                email.Body = new TextPart("plain") { Text = $"Your OTP is: {otp}\n\nThis code expires in 10 minutes." };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(senderEmail, senderPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
                
                _logger.LogInformation($"OTP email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                // Log the error and the OTP for testing purposes
                _logger.LogError($"Failed to send OTP email to {toEmail}: {ex.Message}. OTP: {otp}");
                _logger.LogWarning($"Email sending failed, but OTP is valid. OTP for {toEmail}: {otp}");
            }
        }
    }
}

