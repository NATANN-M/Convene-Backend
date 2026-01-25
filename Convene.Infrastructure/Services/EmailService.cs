using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Convene.Application.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Convene.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var useGmailApi = !string.IsNullOrEmpty(_configuration["Gmail:RefreshToken"]);

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

                _logger.LogInformation("Attempting Gmail API send. ClientId: {ClientId}...", clientId.Substring(0, 10));

                var tokenResponse = new TokenResponse 
                { 
                    RefreshToken = refreshToken,
                    IssuedUtc = DateTime.UtcNow // Force update
                };

                var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
                    Scopes = new[] { GmailService.Scope.GmailSend }
                });

                var credentials = new UserCredential(flow, "user", tokenResponse);

                var service = new GmailService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credentials,
                    ApplicationName = "Convene"
                });

                // Create the MIME message correctly
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                var mimeMessage = MimeKit.MimeMessage.CreateFromMailMessage(mailMessage);
                
                string rawMessage;
                using (var stream = new System.IO.MemoryStream())
                {
                    mimeMessage.WriteTo(stream);
                    rawMessage = Convert.ToBase64String(stream.ToArray())
                        .Replace('+', '-').Replace('/', '_').Replace("=", "");
                }

                var message = new Google.Apis.Gmail.v1.Data.Message { Raw = rawMessage };

                await service.Users.Messages.Send(message, "me").ExecuteAsync();
                _logger.LogInformation("Email sent successfully via Gmail API to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email via Gmail API");
                throw new InvalidOperationException($"Gmail API Error: {ex.Message}. Check if your Refresh Token is still valid.", ex);
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
