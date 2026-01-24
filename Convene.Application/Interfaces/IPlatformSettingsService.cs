using Convene.Domain.Entities;
using Convene.Application.DTOs.AdminPlatformSetting;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface IPlatformSettingsService
    {
        Task<PlatformSettings> GetSettingsAsync();
        Task UpdateSettingsAsync(UpdatePlatformSettingsDto dto);

        // Convenience methods
        Task<int> GetEventPublishCostAsync();
        Task<int> GetInitialOrganizerCreditsAsync();
        Task<decimal> GetCreditPriceETBAsync();
    }
}
