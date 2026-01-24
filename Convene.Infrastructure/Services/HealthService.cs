using CloudinaryDotNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Convene.Application.Interfaces;
using Convene.Application.Interfaces.Convene.Application.Interfaces;
using Convene.Infrastructure.Entities;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services
{
    public class HealthService
    {
        private readonly ConveneDbContext _context;
        private readonly ICloudinaryService _cloudinary;
        private readonly IPaymentService _paymentService;
        private readonly IBackgroundTaskQueue _queue;
        private readonly IConfiguration _config;
        private readonly ILogger<HealthService> _logger;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IEnumerable<TrackedBackgroundService> _backgroundServices;

        private static readonly List<long> _apiResponseTimes = new();

        public HealthService(
            ConveneDbContext context,
            ICloudinaryService cloudinary,
            IPaymentService paymentService,
            IBackgroundTaskQueue queue,
            IConfiguration config,
            ILogger<HealthService> logger,
            IHttpClientFactory httpFactory,
            IEnumerable<TrackedBackgroundService> backgroundServices)
        {
            _context = context;
            _cloudinary = cloudinary;
            _paymentService = paymentService;
            _queue = queue;
            _config = config;
            _logger = logger;
            _httpFactory = httpFactory;
            _backgroundServices = backgroundServices;
        }

        // -------------------------- STATIC HELPERS -------------------------
        public static void AddApiResponseTime(long ms)
        {
            _apiResponseTimes.Add(ms);
            if (_apiResponseTimes.Count > 100)
                _apiResponseTimes.RemoveAt(0);
        }

        // ---------------------------- MAIN HEALTH CHECK ----------------------------
        public async Task<Dictionary<string, object>> CheckHealthAsync(bool storeSnapshot = true)
        {
            var result = new Dictionary<string, object>();

            // --- Database ---
            try
            {
                var sw = Stopwatch.StartNew();
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                sw.Stop();
                result["Database"] = new { status = "Healthy", responseTimeMs = sw.ElapsedMilliseconds };
            }
            catch (Exception ex)
            {
                result["Database"] = new { status = "Unhealthy", error = ex.Message };
            }

            // --- Cloudinary ---
            try
            {
                await _cloudinary.TestConnectionAsync();
                result["Cloudinary-File-Upload"] = new { status = "Healthy", Message="File storage Working Fine" };
            }
            catch (Exception ex)
            {
                result["Cloudinary"] = new { status = "Unhealthy", error = ex.Message };
            }

            // --- Payment (Chapa) ---
            try
            {
                var key = _config["Chapa:SecretKey"];
                var client = _httpFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

                // Use the "Get Banks" endpoint for a true health/connectivity check
                var response = await client.GetAsync("https://api.chapa.co/v1/banks");

                result["PaymentService"] = new
                {
                    status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy",
                    statusCode = response.StatusCode
                };
            }
            catch (Exception ex)
            {
                result["PaymentService"] = new { status = "Unhealthy", error = ex.Message };
            }

            // --- API Timing ---
            result["ApiResponseTime"] = new
            {
                lastAverageMs = _apiResponseTimes.Count > 0 ? (long)Math.Round(_apiResponseTimes.Average()) : 0,
                lastRequestMs = _apiResponseTimes.Count > 0 ? _apiResponseTimes[^1] : 0
            };

            // --- System Info ---
            var start = Process.GetCurrentProcess().StartTime.ToUniversalTime();
            result["System"] = new
            {
                uptimeMinutes = (DateTime.UtcNow - start).TotalMinutes,
                memoryMB = Process.GetCurrentProcess().PrivateMemorySize64 / (1024 * 1024),
                cpuCores = Environment.ProcessorCount
            };

            
            try
            {
                var queueLength = (_queue as dynamic)?._workItems?.Count ?? 0;
                result["BackgroundQueue"] = new { pending = queueLength };
            }
            catch { }

            // --- Background Services ---
            try
            {
                result["BackgroundServices"] = _backgroundServices.Select(s => new
                {
                    service = s.GetType().Name,
                    lastRun = s.LastRunTime
                }).ToList();
            }
            catch { }

            // --- Save Snapshot (Optional) ---
            if (storeSnapshot)
            {
                try
                {
                    var snapshot = new SystemHealthSnapshot
                    {
                        JsonData = JsonSerializer.Serialize(result)
                    };
                    _context.Add(snapshot);
                    await _context.SaveChangesAsync();
                }
                catch { }
            }

            return result;
        }
    }
}
