using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
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
            await appNotifications.SystemException(exception, CancellationToken.None);
        }
        catch (Exception sendEx)
        {
            logger.LogError(sendEx, "Failed to send system exception notification.");
        }

        var (statusCodeInt, responseMessage) = exception switch
        {
            UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, exception.Message),
            ArgumentException => ((int)HttpStatusCode.BadRequest, exception.Message),
            InvalidOperationException when exception.Message.Contains("already has", StringComparison.OrdinalIgnoreCase)
                => ((int)HttpStatusCode.Conflict, exception.Message),
            DbUpdateException when exception.InnerException is PostgresException pg && pg.SqlState == "23505"
                => ((int)HttpStatusCode.Conflict, "A resource already exists with the same key."),
            _ => ((int)HttpStatusCode.InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCodeInt;

        var payload = new
        {
            statusCode = statusCodeInt,
            message = responseMessage,
            detail = GetExceptionDetails(exception)
        };

        var json = JsonConvert.SerializeObject(payload);
        await context.Response.WriteAsync(json);
    }
    
    private static string GetExceptionDetails(Exception exception)
    {
        if (exception is DbUpdateException dbEx)
        {
            if (dbEx.InnerException is PostgresException pg)
            {
                return $"{pg.MessageText} (SQLSTATE {pg.SqlState})";
            }

            if (dbEx.InnerException != null)
            {
                return dbEx.InnerException.Message;
            }
        }

        return exception.InnerException?.Message ?? exception.Message;
    }
}
