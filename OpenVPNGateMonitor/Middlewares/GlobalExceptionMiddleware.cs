using System.Net;
using Newtonsoft.Json;
using OpenVPNGateMonitor.Services.Others.Notifications;

namespace OpenVPNGateMonitor.Middlewares;

public class GlobalExceptionMiddleware(
    RequestDelegate next,
    IServiceProvider serviceProvider,
    ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
            return;

        // Notify admins (best-effort)
        try
        {
            using var scope = serviceProvider.CreateScope();
            var appNotifications = scope.ServiceProvider.GetRequiredService<IAppNotificationFacade>();
            await appNotifications.SystemExceptionAsync(exception, CancellationToken.None);
        }
        catch (Exception sendEx)
        {
            logger.LogError(sendEx, "Failed to send system exception notification.");
        }

        // Prepare error response
        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var payload = new
        {
            context.Response.StatusCode,
            Message = "An unexpected error occurred. Please try again later.",
            Detail = exception.Message
        };

        var json = JsonConvert.SerializeObject(payload);
        await context.Response.WriteAsync(json);
    }
}
