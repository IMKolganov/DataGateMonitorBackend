using Npgsql;

namespace DataGateMonitor.Middlewares;

/// <summary>
/// Classifies request cancellation so client navigation is not logged like a server fault.
/// </summary>
public static class RequestCancellationLogging
{
    public const string PostgresQueryCancelledSqlState = "57014";

    public static bool IsClientInitiatedCancellation(HttpContext context) =>
        context.RequestAborted.IsCancellationRequested;

    public static PostgresException? FindPostgresQueryCancelled(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException pg && pg.SqlState == PostgresQueryCancelledSqlState)
                return pg;
        }

        return null;
    }
}
