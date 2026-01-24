using System;
using System.Linq;
using System.Threading.Tasks;
using Convene.Domain.Entities;
using Convene.Infrastructure.Persistence;

namespace Convene.Infrastructure.Seeders
{
    public static class BoostLevelSeeder
    {
        public static async Task SeedAsync(ConveneDbContext context)
        {
            if (!context.BoostLevels.Any())
            {
                context.BoostLevels.AddRange(
                    new BoostLevel
                    {
                        Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                        Name = "Standard Boost",
                        Description = "24-hour medium visibility boost",
                        CreditCost = 10,
                        DurationHours = 24,
                        Weight = 2,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new BoostLevel
                    {
                        Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                        Name = "Premium Boost",
                        Description = "48-hour high visibility boost",
                        CreditCost = 20,
                        DurationHours = 48,
                        Weight = 4,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    }
                );

                await context.SaveChangesAsync();
            }
        }
    }
}
