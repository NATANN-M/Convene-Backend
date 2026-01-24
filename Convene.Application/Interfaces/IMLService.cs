using Convene.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IMLService
    {
        Task TrainGlobalModelAsync(List<UserEventInteraction> interactions);
        Task TrainForUserAsync(Guid userId, List<UserEventInteraction> interactions);
        Task<float> PredictScoreAsync(Guid userId, Guid eventId);
        Task SaveModelAsync(MlModelStorage model);
        Task<MlModelStorage?> LoadModelAsync();
        Task<RecommendationMetrics> EvaluateModelAsync(List<UserEventInteraction> interactions);
        //for the MlTrainingBackgroundService
         Task<bool> TrainModelAsync();
    }
}
