using Convene.Application.DTOs.EventBrowsing;
using Convene.Application.DTOs.Recommendation;
using Convene.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IRecommendationService
    {
        Task<List<EventSummaryDto>> GetRecommendationsForUserAsync(Guid userId);
        Task GenerateForUserAsync(Guid userId);
        Task RetrainGlobalModelAsync();

        //for admin purpose
        Task RetrainForUserAsync(Guid userId);
        Task<AdminMetricsDto> GetMetricsAsync();
    }
}
