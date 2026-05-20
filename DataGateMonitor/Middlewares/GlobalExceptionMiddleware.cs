using System.Net;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Npgsql;
using DataGateMonitor.Services.Others.Notifications;

namespace DataGateMonitor.Middlewares;

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
        catch (OperationCanceledException ex)
        {
            await HandleOperationCanceledAsync(context, ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred.");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleOperationCanceledAsync(HttpContext context, OperationCanceledException exception)
    {
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.ToString();
        var traceId = context.TraceIdentifier;
        var wasAbortedByClient = context.RequestAborted.IsCancellationRequested;

        if (wasAbortedByClient)
        {
            logger.LogInformation(
                exception,
                "Request was cancelled by client. " +
                "Method: {Method}. Path: {Path}. QueryString: {QueryString}. TraceId: {TraceId}",
                method,
                path,
                queryString,
                traceId);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Operation was cancelled. " +
                "Method: {Method}. Path: {Path}. QueryString: {QueryString}. TraceId: {TraceId}",
                method,
                path,
                queryString,
                traceId);
        }

        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 499;

        var payload = new
        {
            statusCode = 499,
            message = "Request was cancelled.",
            detail = exception.Message,
            traceId
        };

        var json = JsonConvert.SerializeObject(payload);
        await context.Response.WriteAsync(json);
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
            InvalidOperationException ioe when IsDuplicateOrConflictMessage(ioe.Message)
                => ((int)HttpStatusCode.Conflict, ioe.Message),
            InvalidOperationException ioe when IsClientFacingInvalidOperation(ioe.Message)
                => ((int)HttpStatusCode.BadRequest, ioe.Message),
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
            detail = GetExceptionDetails(exception),
            traceId = context.TraceIdentifier
        };

        var json = JsonConvert.SerializeObject(payload);
        await context.Response.WriteAsync(json);
    }

    private static bool IsDuplicateOrConflictMessage(string message) =>
        message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
        || message.Contains("already in use", StringComparison.OrdinalIgnoreCase)
        || message.Contains("already has", StringComparison.OrdinalIgnoreCase);

    private static bool IsClientFacingInvalidOperation(string message) =>
        message.Contains("only supported for", StringComparison.OrdinalIgnoreCase)
        || message.Contains("is missing", StringComparison.OrdinalIgnoreCase)
        || message.Contains("not found", StringComparison.OrdinalIgnoreCase);

    private static string GetExceptionDetails(Exception ex)
    {
        if (ex is DbUpdateException dbEx)
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

        var current = ex;
        while (current.InnerException != null)
        {
            current = current.InnerException;
        }

        return current.Message;
    }
}