using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Convene.Application.Interfaces;
using Resend;

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
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];
            var resendApiKey = _configuration["Resend:ApiKey"];

            if (environment == "Production" && !string.IsNullOrEmpty(resendApiKey))
            {
                await SendViaResendAsync(toEmail, subject, htmlBody, resendApiKey);
            }
            else
            {
                await SendViaSmtpAsync(toEmail, subject, htmlBody);
            }
        }

        private async Task SendViaResendAsync(string toEmail, string subject, string htmlBody, string apiKey)
        {
            try
            {
                IResend resend = ResendClient.Create(apiKey);
                var message = new Resend.EmailMessage
                {
                    From = "onboarding@resend.dev", // Note: In production you should use  verified domain
                    To = toEmail,
                    Subject = subject,
                    HtmlBody = htmlBody
                };

                await resend.EmailSendAsync(message);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Resend API Error: {ex.Message}", ex);
            }
        }

        private async Task SendViaSmtpAsync(string toEmail, string subject, string htmlBody)
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

                var senderPassword = senderPasswordRaw.Replace(" ", "").Replace("\"", "").Replace("'", "").Trim();
                var smtpPort = int.TryParse(portStr, out var p) ? p : 587;
                var enableSsl = bool.TryParse(sslStr, out var ssl) ? ssl : true;

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

                using var client = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 15000
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
            catch (SmtpException ex)
            {
                throw new InvalidOperationException($"SMTP Error (Port {(_configuration["Email:Port"] ?? "587")}): {ex.Message} Status: {ex.StatusCode}. {ex.InnerException?.Message}", ex);
            }
            catch (Exception ex)
            {
                var fullMessage = ex.Message;
                if (ex.InnerException != null) fullMessage += $" -> {ex.InnerException.Message}";
                throw new InvalidOperationException($"Failed to send email via SMTP: {fullMessage}", ex);
            }
        }
    }
}
