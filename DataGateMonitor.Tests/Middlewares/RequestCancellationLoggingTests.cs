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
        var ex = new OperationCanceledException("The operation was canceled.");

        Assert.True(RequestCancellationLogging.IsClientInitiatedCancellation(context, ex));
    }

    [Fact]
    public void IsClientInitiatedCancellation_ReturnsTrue_ForGenericCancelBeforeRequestAbortedFlips()
    {
        var context = new DefaultHttpContext();
        var ex = new OperationCanceledException("The operation was canceled.");

        Assert.True(RequestCancellationLogging.IsClientInitiatedCancellation(context, ex));
    }

    [Fact]
    public void IsClientInitiatedCancellation_ReturnsFalse_ForPostgres57014WithoutClientAbort()
    {
        var context = new DefaultHttpContext();
        var pg = new PostgresException("canceling statement due to statement timeout", severity: "ERROR", invariantSeverity: "ERROR",
            sqlState: RequestCancellationLogging.PostgresQueryCancelledSqlState);
        var ex = new OperationCanceledException("Query was cancelled", pg);

        Assert.False(RequestCancellationLogging.IsClientInitiatedCancellation(context, ex));
    }

    [Fact]
    public void IsClientInitiatedCancellation_ReturnsFalse_ForHttpClientTimeoutMessage()
    {
        var context = new DefaultHttpContext();
        var ex = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout of 15 seconds elapsing.");

        Assert.False(RequestCancellationLogging.IsClientInitiatedCancellation(context, ex));
    }

    [Fact]
    public void IsBenignCancellationLogEvent_MatchesStackTraceFragments()
    {
        Assert.True(RequestCancellationLogging.IsBenignCancellationLogEvent(
            "   at System.Threading.CancellationToken.ThrowOperationCanceledException()"));
        Assert.True(RequestCancellationLogging.IsBenignCancellationLogEvent(
            "System.OperationCanceledException: The operation was canceled."));
    }
}
