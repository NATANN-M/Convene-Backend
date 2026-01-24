using System;
using System.Linq;
using System.Threading.Tasks;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Seeders
{
    public static class PlatformSettingsSeeder
    {
        public static async Task SeedAsync(ConveneDbContext context)
        {
            if (!context.PlatformSettings.Any())
            {
                context.PlatformSettings.Add(new PlatformSettings
                {
                    Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    InitialOrganizerCredits = 20,
                    EventPublishCost = 5,
                    CreditPriceETB = 10,
                    UpdatedAt = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }
        }
    }
}
