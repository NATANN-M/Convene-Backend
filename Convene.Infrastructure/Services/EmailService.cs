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
            var refreshToken = _configuration["Gmail:RefreshToken"];
            var useGmailApi = !string.IsNullOrEmpty(refreshToken);

            if (useGmailApi)
            {
                await SendViaGmailApiAsync(toEmail, subject, htmlBody);
            }
            else
            {
                await SendViaSmtpAsync(toEmail, subject, htmlBody);
            }
        }

        private async Task SendViaGmailApiAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var clientId = _configuration["Gmail:ClientId"]?.Replace("\"", "").Replace("'", "").Trim();
                var clientSecret = _configuration["Gmail:ClientSecret"]?.Replace("\"", "").Replace("'", "").Trim();
                var refreshToken = _configuration["Gmail:RefreshToken"]?.Replace("\"", "").Replace("'", "").Trim();
                var senderEmail = (_configuration["Email:SenderEmail"] ?? "natisew123@gmail.com").Replace("\"", "").Trim();

                if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refreshToken))
                {
                    throw new InvalidOperationException("Gmail API credentials (ClientId, ClientSecret, or RefreshToken) are missing.");
                }

                // Step 1: Get Access Token using Refresh Token
                using var client = _httpClientFactory.CreateClient();
                var refreshPayload = new Dictionary<string, string>
                {
                    { "client_id", clientId },
                    { "client_secret", clientSecret },
                    { "refresh_token", refreshToken },
                    { "grant_type", "refresh_token" }
                };

                var refreshResponse = await client.PostAsync("https://oauth2.googleapis.com/token", new FormUrlEncodedContent(refreshPayload));
                var refreshResult = await refreshResponse.Content.ReadAsStringAsync();

                if (!refreshResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Gmail Token Refresh Failed: {Error}", refreshResult);
                    throw new InvalidOperationException($"Gmail API Token Error: {refreshResult}. Your Refresh Token may have expired or was revoked.");
                }

                using var doc = JsonDocument.Parse(refreshResult);
                var accessToken = doc.RootElement.GetProperty("access_token").GetString();

                // Step 2: Construct MIME Message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                var mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(mailMessage);
                
                string base64Raw;
                using (var stream = new System.IO.MemoryStream())
                {
                    mimeMessage.WriteTo(stream);
                    base64Raw = Convert.ToBase64String(stream.ToArray())
                        .Replace('+', '-').Replace('/', '_').Replace("=", "");
                }

                // Step 3: Send Email via API
                var sendPayload = new { raw = base64Raw };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                
                var sendContent = new StringContent(JsonSerializer.Serialize(sendPayload), Encoding.UTF8, "application/json");
                var sendResponse = await client.PostAsync("https://gmail.googleapis.com/gmail/v1/users/me/messages/send", sendContent);

                if (!sendResponse.IsSuccessStatusCode)
                {
                    var sendError = await sendResponse.Content.ReadAsStringAsync();
                    throw new InvalidOperationException($"Gmail API Send Error: {sendError}");
                }

                _logger.LogInformation("Email successfully sent via Gmail API to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gmail Service Error");
                throw;
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
