using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Convene.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Convene.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var brevoApiKey = _configuration["Brevo:ApiKey"];
            var environment = _configuration["ASPNETCORE_ENVIRONMENT"];

            // Use Brevo API in Production or if API Key is explicitly provided
            if (environment == "Production" || !string.IsNullOrEmpty(brevoApiKey))
            {
                await SendViaBrevoApiAsync(toEmail, subject, htmlBody);
            }
            else
            {
                await SendViaSmtpAsync(toEmail, subject, htmlBody);
            }
        }

        private async Task SendViaBrevoApiAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var apiKey = (_configuration["Brevo:ApiKey"] ?? "").Replace("\"", "").Replace("'", "").Trim();
                var fromEmail = (_configuration["Brevo:FromEmail"] ?? "natisew123@gmail.com").Replace("\"", "").Trim();
                var fromName = (_configuration["Email:SenderName"] ?? "Convene").Replace("\"", "").Trim();

                if (string.IsNullOrEmpty(apiKey))
                {
                    throw new InvalidOperationException("Brevo API Key is missing from configuration.");
                }

                _logger.LogInformation("Attempting Brevo API send. Key length: {Length}", apiKey.Length);

                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("api-key", apiKey);

                var payload = new
                {
                    sender = new { name = fromName, email = fromEmail },
                    to = new[] { new { email = toEmail } },
                    subject = subject,
                    htmlContent = htmlBody
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Brevo API Error ({response.StatusCode}): {error}");
                }

                _logger.LogInformation("Email successfully sent via Brevo API to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Brevo Service Error");
                throw new InvalidOperationException($"Brevo Delivery Failed: {ex.Message}", ex);
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
                    Timeout = 20000
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
