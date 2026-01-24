using Convene.Domain.Entities;
using Convene.Application.DTOs.AdminPlatformSetting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Convene.Application.DTOs.Boosts.AdminBoostLevel;

namespace Convene.Application.Interfaces
{
    public interface IBoostService
    {
        Task<IEnumerable<BoostLevel>> GetActiveBoostLevelsAsync();
        Task<EventBoost> ApplyBoostAsync(Guid organizerProfileId, Guid eventId, Guid boostLevelId);
        Task<bool> CanApplyBoostAsync(Guid organizerProfileId, int boostCost);

        Task<IEnumerable<EventBoost>> GetOrganizerBoostsAsync(Guid organizerProfileId);


        // Admin analytics
        Task<IEnumerable<BoostAnalyticsDto>> GetBoostAnalyticsAsync();

        // --------- ADMIN CRUD ----------
        Task<BoostLevelDto> CreateBoostLevelAsync(CreateBoostLevelDto dto);
        Task<BoostLevelDto> UpdateBoostLevelAsync(UpdateBoostLevelDto dto);
        Task<bool> DeleteBoostLevelAsync(Guid boostLevelId); // soft delete
        Task<IEnumerable<BoostLevelDto>> GetAllBoostLevelsAsync(); // include inactive
        Task<BoostLevelDto> GetBoostLevelByIdAsync(Guid boostLevelId);

        Task<bool> SetBoostLevelStatusAsync(Guid boostLevelId, bool isActive);
        


    }
}
