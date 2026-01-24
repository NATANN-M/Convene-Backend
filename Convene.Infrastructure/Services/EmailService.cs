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
                // Simple console output for now. Replace with real email logic later.
                Console.WriteLine($"Sending Email to {toEmail}");
                Console.WriteLine($"Subject: {subject}");
                Console.WriteLine($"Body: {htmlBody}");
                await Task.CompletedTask;


                var smtpServer = _configuration["Email:SmtpServer"];
                var smtpPort = int.Parse(_configuration["Email:Port"]);
                var senderEmail = _configuration["Email:SenderEmail"];
                var senderName = _configuration["Email:SenderName"];
                var senderPassword = _configuration["Email:Password"];
                var enableSsl = bool.Parse(_configuration["Email:EnableSsl"]);

                using var client = new SmtpClient(smtpServer)
                {
                    Port = smtpPort,
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl
                };

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // You can log this or rethrow for middleware to catch
                throw new InvalidOperationException("Failed to send email", ex);
            }
        }

    }
}
