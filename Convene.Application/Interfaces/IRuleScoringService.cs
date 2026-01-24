using Convene.Domain.Entities;
using System;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IRuleScoringService
    {
        Task<float> DistanceScoreAsync(string userLocation, string eventLocation);
        Task<float> CategoryAffinityScoreAsync(Guid userId, string category);
        Task<float> PopularityScoreAsync(Guid eventId);
       Task<float> RecencyScoreAsync(DateTime eventDate);
        Task<float> ColdStartBoostAsync(Guid userId);
    }
}
