using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Convene.Application.Interfaces;

public class SimpleNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private static readonly ConcurrentDictionary<string, int> _endpointCounts = new();
    private static DateTime _lastReportTime = DateTime.UtcNow;

    public SimpleNotificationService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        await SafeSendEmail("?? Backend Started", $"Backend started at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await SendReportAndClear();
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }

    private async Task SendReportAndClear()
    {
        if (_endpointCounts.IsEmpty) return;

        var report = GenerateEndpointReport();
        await SafeSendEmail("?? Backend Activity Report", report);
        _endpointCounts.Clear();
        _lastReportTime = DateTime.UtcNow;
    }

    private string GenerateEndpointReport()
    {
        try
        {
            var totalRequests = _endpointCounts.Values.Sum();
            var topEndpoints = _endpointCounts.OrderByDescending(x => x.Value).Take(10).ToList();

            var report = $"Backend Activity Report\nPeriod: {_lastReportTime:yyyy-MM-dd HH:mm:ss} to {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\nTotal Requests: {totalRequests}\nUnique Endpoints: {_endpointCounts.Count}\n\nTop Endpoints:\n";

            foreach (var endpoint in topEndpoints)
            {
                report += $"  {endpoint.Key}: {endpoint.Value} requests\n";
            }

            report += $"\nReport Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
            return report;
        }
        catch (Exception)
        {
            return $"Activity Report\nReport Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";
        }
    }

    public static void TrackEndpoint(HttpContext context)
    {
        try
        {
            var endpoint = $"{context.Request.Method} {context.Request.Path}";
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

            string source = "Unknown";

            if (userAgent.Contains("Postman"))
            {
                source = $"Postman - IP: {ip}";
            }
            else if (userAgent.Contains("curl"))
            {
                source = $"curl - IP: {ip}";
            }
            else if (userAgent.Contains("Swagger"))
            {
                source = $"Swagger - IP: {ip}";
            }
            else if (userAgent.Contains("Windows"))
            {
                source = $"Windows - IP: {ip}";
            }
            else if (userAgent.Contains("Mac"))
            {
                source = $"Mac - IP: {ip}";
            }
            else if (userAgent.Contains("Linux"))
            {
                source = $"Linux - IP: {ip}";
            }
            else
            {
                source = $"Browser - IP: {ip}";
            }

            var endpointWithSource = $"{endpoint} ({source})";
            _endpointCounts.AddOrUpdate(endpointWithSource, 1, (key, oldValue) => oldValue + 1);
        }
        catch (Exception)
        {
        }
    }

    private async Task SafeSendEmail(string subject, string body)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var senderEmail = _configuration["Email:SenderEmail"];

            if (string.IsNullOrEmpty(senderEmail)) return;

            var htmlBody = $"<pre style='font-family: Arial, sans-serif; font-size: 14px;'>{body}</pre>";
            await emailService.SendEmailAsync(senderEmail, subject, htmlBody);
        }
        catch (Exception)
        {
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Force send report with whatever was captured before closing
            if (!_endpointCounts.IsEmpty)
            {
                var timeSinceLastReport = DateTime.UtcNow - _lastReportTime;
                var finalReport = GenerateEndpointReport();
                await SafeSendEmail("?? Backend Closed - Final Report",
                    $"Backend closed at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\n" +
                    $"Captured activity from last {timeSinceLastReport.TotalMinutes:F0} minutes\n\n{finalReport}");
            }
            else
            {
                await SafeSendEmail("?? Backend Stopped",
                    $"Backend stopped at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC\nNo endpoints accessed since last report.");
            }
        }
        catch
        {
        }

        await base.StopAsync(cancellationToken);
    }
}
