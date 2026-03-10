using Arelia.Infrastructure.Services;

namespace Arelia.Web.Middleware;

/// <summary>
/// Returns 503 Service Unavailable for all requests while the app is in maintenance mode.
/// Static files and the Blazor SignalR hub are excluded so the maintenance page can render.
/// </summary>
public sealed class MaintenanceMiddleware(RequestDelegate next, MaintenanceState state)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (state.IsInMaintenance
            && !context.Request.Path.StartsWithSegments("/_blazor")
            && !context.Request.Path.StartsWithSegments("/_framework")
            && !context.Request.Path.StartsWithSegments("/_content"))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("The application is temporarily unavailable — a backup restore is in progress.");
            return;
        }

        await next(context);
    }
}
