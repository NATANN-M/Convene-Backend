using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using Microsoft.ML.Trainers;
using Microsoft.ML.Data;
using Convene.Application.Interfaces;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Services.Recommendation
{
    public class MLService : IMLService
    {
        private readonly ConveneDbContext _context;
        private readonly ITrackingService _trackingService;
        private readonly MLContext _mlContext;

        public MLService(ConveneDbContext context, ITrackingService trackingService)
        {
            _context = context;
            _trackingService = trackingService;
            _mlContext = new MLContext(seed: 1);
        }

        private class TrainingData
        {
            public string UserId { get; set; } = null!;
            public string EventId { get; set; } = null!;
            public float Label { get; set; }
        }

        private class Prediction
        {
            [ColumnName("Score")]
            public float Score { get; set; }
        }

        public async Task TrainGlobalModelAsync(List<UserEventInteraction> interactions)
        {
            var data = BuildTrainingData(interactions);
            if (!data.Any()) return;

            var dataView = _mlContext.Data.LoadFromEnumerable(data);

            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("UserIdEncoded", nameof(TrainingData.UserId))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("EventIdEncoded", nameof(TrainingData.EventId)))
                .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(new MatrixFactorizationTrainer.Options
                {
                    MatrixColumnIndexColumnName = "UserIdEncoded",
                    MatrixRowIndexColumnName = "EventIdEncoded",
                    LabelColumnName = nameof(TrainingData.Label),
                    NumberOfIterations = 20,
                    ApproximationRank = 32
                }));

            var model = pipeline.Fit(dataView);

            using var ms = new MemoryStream();
            _mlContext.Model.Save(model, dataView.Schema, ms);
            var modelBinary = ms.ToArray();

            var entity = await _context.MlModelStorages.FirstOrDefaultAsync(m => m.Id == 1)
                         ?? new MlModelStorage { Id = 1 };

            entity.ModelBinary = modelBinary;
            entity.Version = $"v{DateTime.UtcNow:yyyyMMddHHmmss}";
            entity.LastTrained = DateTime.UtcNow;

            if (_context.Entry(entity).State == EntityState.Detached)
                _context.MlModelStorages.Add(entity);

            await _context.SaveChangesAsync();
        }

        public async Task TrainForUserAsync(Guid userId, List<UserEventInteraction> interactions)
        {
            await TrainGlobalModelAsync(interactions);
        }

        public async Task<float> PredictScoreAsync(Guid userId, Guid eventId)
        {
            var modelEntity = await LoadModelAsync();
            if (modelEntity == null || modelEntity.ModelBinary == null) return 0f;

            using var ms = new MemoryStream(modelEntity.ModelBinary);
            var model = _mlContext.Model.Load(ms, out var schema);
            var predEngine = _mlContext.Model.CreatePredictionEngine<TrainingData, Prediction>(model);

            var input = new TrainingData
            {
                UserId = userId.ToString(),
                EventId = eventId.ToString(),
                Label = 0f
            };

            float mlScore;
            try
            {
                mlScore = Math.Clamp(predEngine.Predict(input).Score, 0f, 1f);
            }
            catch
            {
                mlScore = 0f;
            }

            // Proximity adjustment (no change in logic)
            var userLocationStr = await _trackingService.GetLatestUserLocationAsync(userId);

            if (!string.IsNullOrEmpty(userLocationStr))
            {
                (double userLat, double userLng) = _trackingService.ParseLocation(userLocationStr);

                var evt = await _context.Events.AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (evt != null && !string.IsNullOrEmpty(evt.Location))
                {
                    (double eventLat, double eventLng) = _trackingService.ParseLocation(
                        evt.Location.Replace("|", ",")
                    );

                    var distanceKm = _trackingService.CalculateDistance(userLat, userLng, eventLat, eventLng);

                    var proximityMultiplier = distanceKm switch
                    {
                        <= 5 => 1.2f,
                        <= 10 => 1.1f,
                        <= 20 => 1.05f,
                        _ => 1f
                    };

                    mlScore *= proximityMultiplier;
                }
            }

            return Math.Clamp(mlScore, 0f, 1f);
        }

        public async Task SaveModelAsync(MlModelStorage model)
        {
            var existing = await _context.MlModelStorages.FirstOrDefaultAsync(m => m.Id == 1);
            if (existing == null)
                await _context.MlModelStorages.AddAsync(model);
            else
            {
                existing.ModelBinary = model.ModelBinary;
                existing.Version = model.Version;
                existing.LastTrained = model.LastTrained;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<MlModelStorage?> LoadModelAsync()
        {
            return await _context.MlModelStorages.AsNoTracking().FirstOrDefaultAsync(m => m.Id == 1);
        }

        public async Task<bool> TrainModelAsync()
        {
            var interactions = await _trackingService.GetAllInteractionsAsync();

            // Avoid training small datasets
            if (interactions.Count < 20)
                return false;

            try
            {
                await TrainGlobalModelAsync(interactions);
                await EvaluateModelAsync(interactions); // update admin metrics
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<RecommendationMetrics> EvaluateModelAsync(List<UserEventInteraction> interactions)
        {
            var data = BuildTrainingData(interactions);

            // Safe float to avoid NaN/Infinity
            float SafeFloat(double value) => double.IsNaN(value) || double.IsInfinity(value) ? 0f : (float)value;

            var metricsEntity = await _context.RecommendationMetrics.FirstOrDefaultAsync(m => m.Id == 1) ?? new RecommendationMetrics { Id = 1 };

            if (!data.Any())
            {
                metricsEntity.TotalInteractions = 0;
                metricsEntity.TotalUsers = 0;
                metricsEntity.TotalEvents = 0;
                metricsEntity.TopCategoriesJson = "{}";
                metricsEntity.TopEventsJson = "{}";
                metricsEntity.ProximityDistributionJson = "{}";
                metricsEntity.MLRmse = 0f;
                metricsEntity.MLAccuracy = 0f;
                metricsEntity.ModelVersion = "n/a";
                metricsEntity.LastUpdated = DateTime.UtcNow;
                metricsEntity.ColdStartPercentage = 0f;

                if (_context.Entry(metricsEntity).State == EntityState.Detached)
                    _context.RecommendationMetrics.Add(metricsEntity);

                await _context.SaveChangesAsync();
                return metricsEntity;
            }

            // ML evaluation (if model exists)
            var modelEnt = await LoadModelAsync();
            if (modelEnt != null && modelEnt.ModelBinary != null)
            {
                using var ms = new MemoryStream(modelEnt.ModelBinary);
                var model = _mlContext.Model.Load(ms, out var schema);
                var dataView = _mlContext.Data.LoadFromEnumerable(data);
                var predictions = model.Transform(dataView);
                var mlMetrics = _mlContext.Regression.Evaluate(predictions, labelColumnName: nameof(TrainingData.Label), scoreColumnName: "Score");

                metricsEntity.MLRmse = SafeFloat(mlMetrics.RootMeanSquaredError);
                metricsEntity.MLAccuracy = SafeFloat(mlMetrics.RSquared);
                metricsEntity.ModelVersion = modelEnt.Version;
                metricsEntity.LastTrained = modelEnt.LastTrained;
            }
            else
            {
                metricsEntity.MLRmse = 0f;
                metricsEntity.MLAccuracy = 0f;
                metricsEntity.ModelVersion = "n/a";
            }

            // Build additional admin stats
            metricsEntity.TotalInteractions = data.Count;
            metricsEntity.TotalUsers = interactions.Select(i => i.UserId).Distinct().Count();
            metricsEntity.TotalEvents = interactions.Select(i => i.EventId).Distinct().Count();

            // compute cold-start percentage: users with <3 interactions / total users
            var userInteractionCounts = interactions
                .GroupBy(i => i.UserId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToList();

            if (userInteractionCounts.Count > 0)
            {
                var coldUsers = userInteractionCounts.Count(u => u.Count < 3);
                metricsEntity.ColdStartPercentage = (float)coldUsers / userInteractionCounts.Count * 100f;
            }
            else
            {
                metricsEntity.ColdStartPercentage = 0f;
            }

            // Top categories
            var topCategories = interactions
        .Where(i => !string.IsNullOrWhiteSpace(i.Category))
        .GroupBy(i => i.Category!.Trim())
        .OrderByDescending(g => g.Count())
        .Take(10)
        .ToDictionary(g => g.Key, g => g.Count());

            if (!topCategories.Any())
                topCategories["Unknown"] = interactions.Count;


            // Get top event IDs first
            var topEventIds = interactions
    .Where(i => i.EventId != Guid.Empty)
    .GroupBy(i => i.EventId)
    .OrderByDescending(g => g.Count())
    .Take(10)
    .Select(g => g.Key)
    .ToList();


            // Load event names for these IDs
            var eventNames = await _context.Events
                .Where(e => topEventIds.Contains(e.Id))
                .Select(e => new { e.Id, e.Title })
                .ToDictionaryAsync(e => e.Id, e => e.Title ?? $"Event {e.Id}");

            // Create dictionary with event names
            var topEvents = interactions
    .Where(i => i.EventId != Guid.Empty && topEventIds.Contains(i.EventId))

                .GroupBy(i => i.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToDictionary(
                    x => eventNames.ContainsKey(x.EventId) ? eventNames[x.EventId] : $"Event {x.EventId}",
                    x => x.Count
                );

            metricsEntity.TopCategoriesJson = System.Text.Json.JsonSerializer.Serialize(topCategories);
            metricsEntity.TopEventsJson = System.Text.Json.JsonSerializer.Serialize(topEvents);

            // Proximity distribution
            var distanceBuckets = new Dictionary<string, int>
            {
                ["0-5km"] = 0,
                ["5-10km"] = 0,
                ["10-20km"] = 0,
                [">20km"] = 0
            };

            // Get all event IDs from interactions for better querying
            var eventIdsForProximity = interactions
                .Where(x => !string.IsNullOrEmpty(x.UserLocation))
                .Select(i => i.EventId)
                .Distinct()
                .ToList();

            // Load all needed events in one query
            var eventsForProximity = await _context.Events
                .Where(e => eventIdsForProximity.Contains(e.Id) && !string.IsNullOrEmpty(e.Location))
                .ToDictionaryAsync(e => e.Id, e => e.Location);

            foreach (var i in interactions.Where(x => !string.IsNullOrEmpty(x.UserLocation)))
            {
                if (!eventsForProximity.TryGetValue(i.EventId, out var eventLocation))
                    continue;

                var userLoc = _trackingService.ParseLocation(i.UserLocation!);
                var eventLoc = _trackingService.ParseLocation(eventLocation.Replace("|", ","));
                var distanceKm = _trackingService.CalculateDistance(userLoc.lat, userLoc.lng, eventLoc.lat, eventLoc.lng);

                var bucket = distanceKm switch
                {
                    <= 5 => "0-5km",
                    <= 10 => "5-10km",
                    <= 20 => "10-20km",
                    _ => ">20km"
                };
                distanceBuckets[bucket]++;
            }

            metricsEntity.ProximityDistributionJson = System.Text.Json.JsonSerializer.Serialize(distanceBuckets);
            metricsEntity.LastUpdated = DateTime.UtcNow;

            if (_context.Entry(metricsEntity).State == EntityState.Detached)
                _context.RecommendationMetrics.Add(metricsEntity);

            await _context.SaveChangesAsync();
            return metricsEntity;
        }

        #region Helpers

        private List<TrainingData> BuildTrainingData(List<UserEventInteraction> interactions)
        {
            return interactions.Select(i => new TrainingData
            {
                UserId = i.UserId.ToString(),
                EventId = i.EventId.ToString(),
                Label = i.InteractionType switch
                {
                    "View" => 0.2f,
                    "Search" => 0.3f,
                    "Favorite" => 0.6f,
                    "Share" => 0.6f,
                    "Book" => 1f,
                    _ => 0.2f
                }
            }).ToList();
        }

        #endregion
    }
}
