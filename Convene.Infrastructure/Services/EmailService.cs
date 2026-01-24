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

                // Gmail App Passwords often contain spaces for readability
                // Also strip literal quotes in case the user included them in the Render UI
                var senderPassword = senderPasswordRaw.Replace(" ", "").Replace("\"", "").Replace("'", "").Trim();
                var smtpPort = int.TryParse(portStr, out var p) ? p : 587;
                var enableSsl = bool.TryParse(sslStr, out var ssl) ? ssl : true;

                // Ensure TLS 1.2+ is used (required by Gmail)
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                using var client = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 15000 // 15 seconds
                };

                using var mailMessage = new MailAddress(senderEmail, senderName ?? "Convene") is var fromAddress
                    ? new MailMessage
                    {
                        From = fromAddress,
                        Subject = subject,
                        Body = htmlBody,
                        IsBodyHtml = true
                    } : throw new InvalidOperationException("Invalid sender email address format.");

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (SmtpException ex)
            {
                 // Specific SMTP error with more detail
                 throw new InvalidOperationException($"SMTP Error (Port {(_configuration["Email:Port"] ?? "587")}): {ex.Message} Status: {ex.StatusCode}. {ex.InnerException?.Message}", ex);
            }
            catch (Exception ex)
            {
                // Rethrow with full hierarchy of messages
                var fullMessage = ex.Message;
                if (ex.InnerException != null) fullMessage += $" -> {ex.InnerException.Message}";
                throw new InvalidOperationException($"Failed to send email: {fullMessage}", ex);
            }
        }

    }
}
