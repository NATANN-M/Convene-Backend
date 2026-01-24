using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Convene.Application.Interfaces;

namespace Convene.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpServer = _configuration["Email:SmtpServer"];
                var portStr = _configuration["Email:Port"];
                var senderEmail = _configuration["Email:SenderEmail"];
                var senderName = _configuration["Email:SenderName"];
                var senderPasswordRaw = _configuration["Email:Password"];
                var sslStr = _configuration["Email:EnableSsl"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPasswordRaw))
                {
                     throw new InvalidOperationException("Email settings are missing in configuration (SmtpServer, SenderEmail, or Password).");
                }

                // Gmail App Passwords often contain spaces for readability, but should be used without them
                var senderPassword = senderPasswordRaw.Replace(" ", "").Trim();
                var smtpPort = int.TryParse(portStr, out var p) ? p : 587;
                var enableSsl = bool.TryParse(sslStr, out var ssl) ? ssl : true;

                using var client = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName ?? "Convene"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Rethrow with details (inner exception will contain the SMTP error)
                throw new InvalidOperationException($"Failed to send email to {toEmail}: {ex.Message}", ex);
            }
        }

    }
}
