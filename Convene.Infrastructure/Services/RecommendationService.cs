using Microsoft.EntityFrameworkCore;
using Convene.Application.DTOs.Event;
using Convene.Application.DTOs.EventBrowsing;
using Convene.Application.DTOs.Recommendation;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services.Recommendation
{
    public class RecommendationService : IRecommendationService
    {
        private readonly ConveneDbContext _context;
        private readonly IMLService _mlService;
        private readonly IRuleScoringService _ruleService;
        private readonly ITrackingService _trackingService;

        public RecommendationService(
            ConveneDbContext context,
            IMLService mlService,
            IRuleScoringService ruleService,
            ITrackingService trackingService)
        {
            _context = context;
            _mlService = mlService;
            _ruleService = ruleService;
            _trackingService = trackingService;
        }

        // Public: return summary DTOs and persist UserRecommendation
        public async Task<List<EventSummaryDto>> GetRecommendationsForUserAsync(Guid userId)
        {
         
            //  Load all events
          
            var events = await _context.Events
                .Where(e => e.Status == EventStatus.Published)
                .Include(e => e.Category)
                .Include(e => e.TicketTypes)
                .Include(e => e.Bookings)
                .AsNoTracking()
                .ToListAsync();

            //  Load boosts for all events in one query
            var boosts = await _context.EventBoosts
       .Include(b => b.BoostLevel)
       .Where(b => events.Select(e => e.Id).Contains(b.EventId)
                   && b.EndTime > DateTime.UtcNow) // only active boosts
       .ToListAsync();


         
            //  Get User Location + ML Model
           
            var userLocation = await _trackingService.GetLatestUserLocationAsync(userId) ?? "";
            int interactionCount = await _trackingService.GetUserInteractionCountAsync(userId);
            bool isColdStart = interactionCount < 3;
            bool noMLModel = (await _mlService.LoadModelAsync()) == null;

            var recommendations = new List<UserRecommendation>();
            var dtoList = new List<EventSummaryDto>();

            // Compute Scores for All Events
           
            foreach (var ev in events)
            {
                // ML score
                float mlScore = 0f;
                try
                {
                    mlScore = await _mlService.PredictScoreAsync(userId, ev.Id);
                }
                catch { mlScore = 0f; }

                // Rule score
                float ruleScore = await CalculateRuleScoreAsync(userId, userLocation, ev);

                // final combined score
                float finalScore = CombineScores(mlScore, ruleScore, isColdStart, noMLModel);

                // persist recommendation row
                recommendations.Add(new UserRecommendation
                {
                    UserId = userId,
                    EventId = ev.Id,
                    FinalScore = finalScore,
                    LastUpdated = DateTime.UtcNow
                });

                // build DTO
                var media = TryParseCoverImage(ev.CoverImageUrl);

                dtoList.Add(new EventSummaryDto
                {
                    EventId = ev.Id,
                    Title = ev.Title,
                    BannerImageUrl = media?.CoverImage,
                    Venue = ev.Venue,
                    TicketsaleStart = ev.TicketSalesStart,
                    TicketsaleEnd = ev.TicketSalesEnd,
                    StartDate = ev.StartDate,
                    EndDate = ev.EndDate,
                    CategoryName = ev.Category?.Name ?? "",
                    ActiveBoostLevelName=boosts.Where(b => b.EventId == ev.Id && b.EndTime > DateTime.UtcNow)
                      
                        .Select(b => b.BoostLevel.Name)
                        .FirstOrDefault() ?? "",
                    LowestTicketPrice = GetLowestTicketPrice(ev.TicketTypes),
                    IsSoldOut = ev.TicketTypes.All(t => t.Quantity <= 0)
                });
            }

         
            //  Save Recommendations
            
            foreach (var rec in recommendations)
            {
                var existing = await _context.UserRecommendations
                    .FirstOrDefaultAsync(r => r.UserId == rec.UserId && r.EventId == rec.EventId);

                if (existing != null)
                {
                    existing.FinalScore = rec.FinalScore;
                    existing.LastUpdated = rec.LastUpdated;
                }
                else
                {
                    _context.UserRecommendations.Add(rec);
                }
            }

            await _context.SaveChangesAsync();

         
            ////  Apply Boost Logic  /////
         
            // A = weight > 10
            var groupA = dtoList
                .Where(d =>
                {
                    var boost = boosts.FirstOrDefault(b => b.EventId == d.EventId);
                    return boost != null &&
                           boost.BoostLevel.Weight > 10 &&
                           (boost.BoostLevel.Name == "Gold" || boost.BoostLevel.Name == "Premium");
                })
                .OrderBy(x => Guid.NewGuid()) // rotation
                .ToList();

            // B = 5 < weight = 10
            var groupB = dtoList
                .Where(d =>
                {
                    var boost = boosts.FirstOrDefault(b => b.EventId == d.EventId);
                    return boost != null &&
                           boost.BoostLevel.Weight > 5 &&
                           boost.BoostLevel.Weight <= 10 &&
                           (boost.BoostLevel.Name == "Gold" || boost.BoostLevel.Name == "Premium");
                })
                .OrderBy(x => Guid.NewGuid()) // rotation
                .ToList();

            // C = normal recommended events
            var groupC = dtoList
                .Where(d =>
                {
                    var boost = boosts.FirstOrDefault(b => b.EventId == d.EventId);
                    bool isBoosted = boost != null && boost.BoostLevel.Weight > 5;
                    return !isBoosted;
                })
                .OrderByDescending(d =>
                {
                    var rec = recommendations.FirstOrDefault(r => r.EventId == d.EventId);
                    return rec?.FinalScore ?? 0f;
                })
                .ToList();

            
            // 6. Limit boosted events
            
            var boostedTop = groupA
                .Concat(groupB)
                .Take(5)   // maximum 5 boosted items
                .ToList();

           
            //  Final Result Ordering
        
            var finalResult = boostedTop.Concat(groupC).ToList();

            return finalResult;
        }


        // Generate for user (used by background job / admin)
        public async Task GenerateForUserAsync(Guid userId)
        {
            await GetRecommendationsForUserAsync(userId);
        }

        public async Task RetrainGlobalModelAsync()
        {
            var allInteractions = await _trackingService.GetAllInteractionsAsync();
            await _mlService.TrainGlobalModelAsync(allInteractions);
            await _mlService.EvaluateModelAsync(allInteractions);
        }

        public async Task RetrainForUserAsync(Guid userId)
        {
            var allInteractions = await _trackingService.GetAllInteractionsAsync();
            await _mlService.TrainForUserAsync(userId, allInteractions);
        }

        public async Task<AdminMetricsDto> GetMetricsAsync()
        {
            var interactions = await _trackingService.GetAllInteractionsAsync();
            var metricsEntity = await _mlService.EvaluateModelAsync(interactions);

            var topCategories = string.IsNullOrEmpty(metricsEntity.TopCategoriesJson)
                ? new Dictionary<string, int>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(metricsEntity.TopCategoriesJson)!;

            var topEvents = string.IsNullOrEmpty(metricsEntity.TopEventsJson)
                ? new Dictionary<string, int>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(metricsEntity.TopEventsJson)!;

            var proximityDistribution = string.IsNullOrEmpty(metricsEntity.ProximityDistributionJson)
                ? new Dictionary<string, int>()
                : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, int>>(metricsEntity.ProximityDistributionJson)!;

            return new AdminMetricsDto
            {
                TotalInteractions = metricsEntity.TotalInteractions,
                TotalUsers = metricsEntity.TotalUsers,
                TotalEvents = metricsEntity.TotalEvents,
                TopCategories = topCategories,
                TopEvents = topEvents,
                ProximityDistribution = proximityDistribution,
                MLAccuracy = metricsEntity.MLAccuracy,
                MLRmse = metricsEntity.MLRmse,
                ModelVersion = metricsEntity.ModelVersion,
                LastTrained = metricsEntity.LastTrained,
                ColdStartPercentage = metricsEntity.ColdStartPercentage
            };
        }

        #region Internal helpers

        private float CombineScores(float mlScore, float ruleScore, bool isColdStart, bool noMLModel)
        {
            if (isColdStart || noMLModel)
                return ruleScore * 0.9f + mlScore * 0.1f;

            return mlScore * 0.6f + ruleScore * 0.4f;
        }

        /// <summary>
        /// Calculate rule score. When userLocation is present, category affinity is boosted (C).
        /// </summary>
        private async Task<float> CalculateRuleScoreAsync(Guid userId, string userLocation, Domain.Entities.Event ev)
        {
            var distance = await _ruleService.DistanceScoreAsync(userLocation ?? "", ev.Location ?? "");

            var categoryName = ev.Category?.Name ?? "";
            var categoryAffinity = await _ruleService.CategoryAffinityScoreAsync(userId, categoryName);

            // BOOST C when user provided location (option A)
            if (!string.IsNullOrEmpty(userLocation))
            {
                // 25% boost to category affinity, capped at 1.0
                categoryAffinity = Math.Min(1f, categoryAffinity * 1.25f);
            }

            var popularity = await _ruleService.PopularityScoreAsync(ev.Id);
            var recency = await _ruleService.RecencyScoreAsync(ev.StartDate);
            var coldBoost = await _ruleService.ColdStartBoostAsync(userId);

            // Combine rule signals and normalize
            float combined = (distance + categoryAffinity + popularity + recency) / 4f;
            combined += coldBoost; // add boost for cold users
            return Math.Min(combined, 1f);
        }

        private decimal GetLowestTicketPrice(IEnumerable<TicketType> ticketTypes)
        {
            if (ticketTypes == null) return 0;
            var prices = ticketTypes.Where(t => t.Quantity > 0).Select(t => t.BasePrice);
            return prices.Any() ? prices.Min() : 0;
        }

        private EventMediaDto? TryParseCoverImage(string? coverImageUrl)
        {
            if (string.IsNullOrWhiteSpace(coverImageUrl)) return null;
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<EventMediaDto>(coverImageUrl);
            }
            catch
            {
                return null;
            }
        }

        #endregion
    }
}
