using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace YourNamespace.RateLimiting
{
    public static class RateLimitingConfig
    {
        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // -------------------------------------
                // 1) GLOBAL RATE LIMIT FOR ALL ENDPOINTS
                // -------------------------------------
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    
                    if (context.User?.Identity?.IsAuthenticated == true)
                    {
                        var userId = context.User.FindFirst("UserId")?.Value;

                        // Safety fallback (should not happen)
                        if (string.IsNullOrEmpty(userId))
                            userId = "unknown-user";

                        //  role-based limits
                        var role = context.User.FindFirst("role")?.Value;

                        var permitLimit = role switch
                        {
                            "Admin" => 1000,
                            "Organizer" => 500,
                            "GatePerson" => 300,
                            _ => 200
                        };

                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: $"user:{userId}",
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = permitLimit,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 6, // smoother traffic
                                QueueLimit = 0
                            });
                    }

                    // 2?? ANONYMOUS USERS ? per IP (strict)
                    var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

                    return RateLimitPartition.GetSlidingWindowLimiter(
                        partitionKey: $"ip:{ip}",
                        factory: _ => new SlidingWindowRateLimiterOptions
                        {
                            PermitLimit = 50,
                            Window = TimeSpan.FromMinutes(1),
                            SegmentsPerWindow = 6,
                            QueueLimit = 0
                        });
                });


                // -------------------------------------
                // 2) CUSTOM SCAN ENDPOINT RATE LIMITING
                // -------------------------------------
                options.AddPolicy("ScanRateLimit", context =>
                {
                    string gatePersonId = GetClientId(context);

                    return RateLimitPartition.GetTokenBucketLimiter(
                        partitionKey: gatePersonId,
                        factory: key => new TokenBucketRateLimiterOptions
                        {
                            TokenLimit = 10,                        // Maximum burst
                            TokensPerPeriod = 5,                    // Refill amount
                            ReplenishmentPeriod = TimeSpan.FromSeconds(1), // Refill window
                            QueueLimit = 0,
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                        });
                });

                // custom 429 response
                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;

                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        success = false,
                        message = "Too many requests. Please slow down.",
                        retryAfter = 1
                    });
                };
            });

            return services;
        }

        private static string GetClientId(HttpContext context)
        {
            var userId = context.User?.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userId))
                return userId;

            var deviceId = context.Request.Headers["X-Device-Id"].ToString();
            if (!string.IsNullOrEmpty(deviceId))
                return deviceId;

            return "anonymous";
        }
    }
}
