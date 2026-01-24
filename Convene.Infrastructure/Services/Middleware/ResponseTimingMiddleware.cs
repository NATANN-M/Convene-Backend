using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Middleware
{
    public class ResponseTimingMiddleware
    {
        private readonly RequestDelegate _next;

        public ResponseTimingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var sw = Stopwatch.StartNew();

            await _next(context);

            sw.Stop();

            context.Items["ResponseTimeMs"] = sw.ElapsedMilliseconds;

            Convene.Infrastructure.Services.HealthService.AddApiResponseTime(sw.ElapsedMilliseconds);
        }
    }

    public static class ResponseTimingMiddlewareExtensions
    {
        // Only classic IApplicationBuilder extension
        public static IApplicationBuilder UseResponseTiming(this IApplicationBuilder app)
        {
            app.UseMiddleware<ResponseTimingMiddleware>();
            return app;
        }
    }
}
