using Convene.Application.DTOs.AdminPlatformSetting;
using Convene.Application.DTOs.Credits;
using Convene.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Convene.Application.Interfaces
{
    public interface ICreditService
    {
        Task<int> GetBalanceAsync(Guid organizerProfileId);

       
        Task DeductCreditsAsync(Guid organizerProfileId, int credits, string type, string? description = null);

       
        Task<IEnumerable<CreditTransaction>> GetTransactionHistoryAsync(Guid organizerProfileId);

        Task<PlatformSettings> GetPlatformSettingsAsync();

        
        Task<PlatformRevenueSummaryDto> GetPlatformRevenueSummaryAsync();

        
        Task<CreditTransaction> CreatePendingTransactionAsync(Guid organizerProfileId, int creditsToBuy);

      
        Task<bool> MarkTransactionCompletedAsync(string paymentReference);


        Task<BuyCreditInfoDto> GetPurchaseInfoAsync();

        // admin to add manual credit
        Task AdminAddCreditsAsync(AdminAddCreditDto request);


       
            Task<List<CreditTransactionViewDto>> GetCreditTransactionsAsync(
                CreditTransactionFilterDto filter);
        

    }
}
