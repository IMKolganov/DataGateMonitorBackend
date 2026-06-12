using DataGateMonitor.Middlewares;
using Microsoft.AspNetCore.Http;
using Npgsql;

namespace DataGateMonitor.Tests.Middlewares;

public sealed class RequestCancellationLoggingTests
{
    [Fact]
    public void FindPostgresQueryCancelled_Returns57014_FromInnerException()
    {
        var pg = new PostgresException("canceling statement due to user request", severity: "ERROR", invariantSeverity: "ERROR",
            sqlState: RequestCancellationLogging.PostgresQueryCancelledSqlState);
        var ex = new OperationCanceledException("Query was cancelled", pg);

        var found = RequestCancellationLogging.FindPostgresQueryCancelled(ex);

        Assert.NotNull(found);
        Assert.Equal("57014", found!.SqlState);
    }

    [Fact]
    public void FindPostgresQueryCancelled_ReturnsNull_WhenNoPostgresInner()
    {
        var ex = new OperationCanceledException("The operation was canceled.");

        Assert.Null(RequestCancellationLogging.FindPostgresQueryCancelled(ex));
    }

    [Fact]
    public void IsClientInitiatedCancellation_ReturnsTrue_WhenRequestAborted()
    {
        var context = new DefaultHttpContext();
        context.RequestAborted = new CancellationToken(canceled: true);

        Assert.True(RequestCancellationLogging.IsClientInitiatedCancellation(context));
    }
}
