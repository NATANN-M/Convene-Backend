using Convene.Application.DTOs.Recommendation;
using Convene.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface ITrackingService
    {
        // Existing
        Task SaveInteractionAsync(TrackInteractionDto dto,Guid userId);
        Task UpdateUserLocationAsync(Guid userId, Guid eventId, string userLocation);
        Task<int> GetUserInteractionCountAsync(Guid userId);
        Task<List<UserEventInteraction>> GetAllInteractionsAsync();
        Task<Dictionary<Guid, int>> GetEventPopularityAsync();
        Task<string> ExtractCategoryAsync(Guid eventId);
        Task<string?> GetLatestUserLocationAsync(Guid userId);

        // location tracking only
        Task UpdateUserLocationOnlyAsync(Guid userId, string userLocation);

        // NEW: General-purpose interaction tracking
        Task TrackInteractionAsync(
            Guid userId,
            Guid? eventId = null,
            string interactionType = "Unknown",
            string? userLocation = null);

        // ---------------- Proximity helpers ----------------
        (double lat, double lng) ParseLocation(string? input);
        double CalculateDistance(double lat1, double lng1, double lat2, double lng2);
    }
}
