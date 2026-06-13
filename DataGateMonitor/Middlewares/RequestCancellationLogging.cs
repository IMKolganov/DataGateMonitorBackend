using Npgsql;

namespace DataGateMonitor.Middlewares;

/// <summary>
/// Classifies request cancellation so client navigation is not logged like a server fault.
/// </summary>
public static class RequestCancellationLogging
{
    public const string PostgresQueryCancelledSqlState = "57014";

    public static bool IsClientInitiatedCancellation(HttpContext context, OperationCanceledException exception)
    {
        if (context.RequestAborted.IsCancellationRequested)
            return true;

        if (exception.CancellationToken.IsCancellationRequested
            && exception.CancellationToken == context.RequestAborted)
            return true;

        // Statement timeout / server-side cancel — keep visible even when the message looks generic.
        if (FindPostgresQueryCancelled(exception) is not null)
            return false;

        return IsBenignHttpRequestCancellation(exception);
    }

    /// <summary>
    /// Generic cancel text from ASP.NET/EF when the browser aborts a fetch (often before RequestAborted flips).
    /// Distinct from HttpClient.Timeout ("configured HttpClient.Timeout").
    /// </summary>
    public static bool IsBenignHttpRequestCancellation(OperationCanceledException exception) =>
        exception.Message is "The operation was canceled." or "A task was canceled.";

    public static PostgresException? FindPostgresQueryCancelled(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg && pg.SqlState == PostgresQueryCancelledSqlState)
                return pg;
        }

        return null;
    }

    public static bool IsBenignCancellationLogEvent(string renderedMessage) =>
        renderedMessage.Contains("ThrowOperationCanceledException", StringComparison.Ordinal)
        || (renderedMessage.Contains("GlobalExceptionMiddleware.InvokeAsync", StringComparison.Ordinal)
            && renderedMessage.Contains("OperationCanceledException", StringComparison.Ordinal))
        || renderedMessage.Contains("System.OperationCanceledException: The operation was canceled.", StringComparison.Ordinal)
        || renderedMessage.Contains("System.Threading.Tasks.TaskCanceledException: A task was canceled.", StringComparison.Ordinal);
}
