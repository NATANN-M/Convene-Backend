using Microsoft.EntityFrameworkCore;
using Convene.Domain.Entities;
using Convene.Domain.Enums;
using Convene.Infrastructure.Helpers;
using Convene.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Seeders
{
    public static class DbSeeder
    {
        public static async Task SeedSuperAdminAsync(ConveneDbContext context, PasswordHasher hasher, IConfiguration configuration)
        {
            try
            {
                var seedConfig = configuration.GetSection("Seed:SuperAdmin");
                var email = seedConfig["Email"] ?? "admin@Convene.com";
                var fullName = seedConfig["FullName"] ?? "System Super Admin";
                var phoneNumber = seedConfig["PhoneNumber"] ?? "0911000000";
                var defaultPassword = seedConfig["DefaultPassword"] ?? "Admin@123";

                // Check if SuperAdmin already exists
                var existingSuperAdmin = await context.Users
                    .FirstOrDefaultAsync(u => u.Role == UserRole.SuperAdmin);

                if (existingSuperAdmin == null)
                {
                    var superAdmin = new User
                    {
                        Id = Guid.NewGuid(),
                        FullName = fullName,
                        Email = email,
                        PhoneNumber = phoneNumber,
                        PasswordHash = hasher.HashPassword(defaultPassword),
                        Role = UserRole.SuperAdmin,
                        Status = UserStatus.Active,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await context.Users.AddAsync(superAdmin);
                    await context.SaveChangesAsync();

                    Console.WriteLine("SuperAdmin user created successfully.");
                }
                else
                {
                    existingSuperAdmin.UpdatedAt = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                    Console.WriteLine("SuperAdmin user already exists. Updated timestamp.");
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't break the application
                Console.WriteLine($"Warning: Could not seed SuperAdmin user. Error: {ex.Message}");
                // You might want to use proper logging here instead of Console.WriteLine
            }
        }
    }
}
