using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Recommendation;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services.Recommendation
{
    public class TrackingService : ITrackingService
    {
        private readonly ConveneDbContext _context;

        public TrackingService(ConveneDbContext context)
        {
            _context = context;
        }

       

        public async Task SaveInteractionAsync(TrackInteractionDto dto, Guid userId)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var category = await ExtractCategoryAsync(dto.EventId);

            var interaction = new UserEventInteraction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventId = dto.EventId,
                InteractionType = dto.InteractionType,
                UserLocation = dto.UserLocation,
                Category = category,
                Timestamp = DateTime.UtcNow
            };

            await _context.UserEventInteractions.AddAsync(interaction);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateUserLocationAsync(Guid userId, Guid eventId, string userLocation)
        {
            var interaction = await _context.UserEventInteractions
                .Where(i => i.UserId == userId && i.EventId == eventId)
                .OrderByDescending(i => i.Timestamp)
                .FirstOrDefaultAsync();

            if (interaction != null)
            {
                interaction.UserLocation = userLocation;
                await _context.SaveChangesAsync();
                return;
            }

            var category = await ExtractCategoryAsync(eventId);
            var newInteraction = new UserEventInteraction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventId = eventId,
                InteractionType = "View",
                UserLocation = userLocation,
                Category = category,
                Timestamp = DateTime.UtcNow
            };

            await _context.UserEventInteractions.AddAsync(newInteraction);
            await _context.SaveChangesAsync();
        }


        public async Task<int> GetUserInteractionCountAsync(Guid userId)
        {
            return await _context.UserEventInteractions.CountAsync(i => i.UserId == userId);
        }


        public async Task<List<UserEventInteraction>> GetAllInteractionsAsync()
        {
            return await _context.UserEventInteractions.AsNoTracking().ToListAsync();
        }

        public async Task<Dictionary<Guid, int>> GetEventPopularityAsync()
        {
            return await _context.UserEventInteractions
                .GroupBy(i => i.EventId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);
        }

        public async Task<string> ExtractCategoryAsync(Guid eventId)
        {
            var evt = await _context.Events
                .Include(e => e.Category)
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == eventId);

            return evt?.Category?.Name ?? "Unknown";
        }

        public async Task<string?> GetLatestUserLocationAsync(Guid userId)
        {
            return await _context.UserEventInteractions
                .Where(i => i.UserId == userId && !string.IsNullOrEmpty(i.UserLocation))
                .OrderByDescending(i => i.Timestamp)
                .Select(i => i.UserLocation)
                .FirstOrDefaultAsync();
        }

        

        #region Helpers (Internal Utilities)

        // Accepts "lat,lng" or "lat|lng" or "lat lng" (lenient)
        public (double lat, double lng) ParseLocation(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return (0, 0);

            // Normalize separators
            var normalized = input.Trim().Replace("|", ",").Replace(" ", ",");
            var parts = normalized.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return (0, 0);

            if (!double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lat))
                lat = 0;
            if (!double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var lng))
                lng = 0;

            return (lat, lng);
        }

        public double CalculateDistance(double lat1, double lng1, double lat2, double lng2)
        {
            const double R = 6371; // km
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

        #region NEW: General-purpose Tracking

        /// <summary>
        /// Track any user interaction with optional EventId and optional location.
        /// Resolves category if EventId is provided.
        /// </summary>
        public async Task TrackInteractionAsync(
            Guid userId,
            Guid? eventId = null,
            string interactionType = "Unknown",
            string? userLocation = null)
        {
            string category = "Unknown";

            if (eventId.HasValue)
            {
                category = await ExtractCategoryAsync(eventId.Value);
            }

            var interaction = new UserEventInteraction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventId = eventId ?? Guid.Empty,
                InteractionType = interactionType,
                Category = category,
                UserLocation = userLocation,
                Timestamp = DateTime.UtcNow
            };

            await _context.UserEventInteractions.AddAsync(interaction);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Track a location-only update (no event attached).
        /// Useful when user logs in or updates device location.
        /// </summary>
        public async Task UpdateUserLocationOnlyAsync(Guid userId, string userLocation)
        {
            var interaction = new UserEventInteraction
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                EventId = Guid.Empty,       // No event
                InteractionType = "LocationUpdate",
                Category = "Unknown",
                UserLocation = userLocation,
                Timestamp = DateTime.UtcNow
            };

            await _context.UserEventInteractions.AddAsync(interaction);
            await _context.SaveChangesAsync();
        }

        #endregion
    }
}
