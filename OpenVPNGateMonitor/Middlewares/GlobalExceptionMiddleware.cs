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

        var statusCode = exception switch
        {
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,   // 401
            ArgumentException => (int)HttpStatusCode.BadRequest,               // 400
            _ => (int)HttpStatusCode.InternalServerError                      // 500
        };

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var message = statusCode switch
        {
            (int)HttpStatusCode.Unauthorized => exception.Message, // "Invalid login or password." / "User account is blocked."
            (int)HttpStatusCode.BadRequest => exception.Message,
            _ => "An unexpected error occurred. Please try again later."
        };

        var payload = new
        {
            statusCode,
            message,
            detail = exception.Message
        };

        var json = JsonConvert.SerializeObject(payload);
        await context.Response.WriteAsync(json);
    }
}
