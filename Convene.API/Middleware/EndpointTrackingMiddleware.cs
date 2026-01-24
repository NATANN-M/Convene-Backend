using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

public class EndpointTrackingMiddleware
{
    private readonly RequestDelegate _next;

    public EndpointTrackingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            SimpleNotificationService.TrackEndpoint(context);
        }
        catch (Exception)
        {
        }

        await _next(context);
    }
}
