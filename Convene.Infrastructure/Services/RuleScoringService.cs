using Microsoft.EntityFrameworkCore;
using Convene.Application.Interfaces;
using Convene.Infrastructure.Persistence;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services.Recommendation
{
    public class RuleScoringService : IRuleScoringService
    {
        private readonly ConveneDbContext _context;

        public RuleScoringService(ConveneDbContext context)
        {
            _context = context;
        }

        // userLocation: "lat,lng"
        public async Task<float> DistanceScoreAsync(string userLocation, string eventLocation)
        {
            if (string.IsNullOrWhiteSpace(userLocation) || string.IsNullOrWhiteSpace(eventLocation))
                return 0f;

            var u = ParseLocation(userLocation);
            var e = ParseLocation(eventLocation);

            var distanceKm = CalculateDistance(u.lat, u.lng, e.lat, e.lng);

            // Normalize: 0 km -> 1.0, >= 50km -> 0.0
            var normalized = Math.Max(0, (50 - distanceKm) / 50.0);
            return (float)normalized;
        }

        public async Task<float> CategoryAffinityScoreAsync(Guid userId, string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return 0f;

            var total = await _context.UserEventInteractions
                .CountAsync(i => i.UserId == userId);

            if (total == 0) return 0f;

            var same = await _context.UserEventInteractions
                .CountAsync(i => i.UserId == userId && i.Category == category);

            return (float)same / total;
        }

        public async Task<float> PopularityScoreAsync(Guid eventId)
        {
            var total = await _context.UserEventInteractions.CountAsync();
            if (total == 0) return 0f;

            var eventCount = await _context.UserEventInteractions.CountAsync(i => i.EventId == eventId);
            return (float)eventCount / total;
        }

        public Task<float> RecencyScoreAsync(DateTime eventDate)
        {
            // Fixed: Return Task.FromResult for synchronous calculations
            var days = (eventDate - DateTime.UtcNow).TotalDays;
            if (days < 0) return Task.FromResult(0f);

            // Events within 7 days are high; decays to 0 at 30 days
            var normalized = Math.Max(0, (30 - days) / 30.0);
            return Task.FromResult((float)normalized);
        }

        // if user has <3 interactions, return boost 1f else 0f
        public async Task<float> ColdStartBoostAsync(Guid userId)
        {
            var count = await _context.UserEventInteractions.CountAsync(i => i.UserId == userId);
            return count < 3 ? 1f : 0f;
        }

        #region Helpers
        public (double lat, double lng) ParseLocation(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return (0, 0);
            var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return (0, 0);

            if (!double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat)) lat = 0;
            if (!double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lng)) lng = 0;
            return (lat, lng);
        }

        public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371;
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lng2 - lng1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double DegreesToRadians(double deg) => deg * Math.PI / 180;
        #endregion
    }
}
