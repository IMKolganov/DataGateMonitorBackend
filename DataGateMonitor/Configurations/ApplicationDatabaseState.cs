namespace DataGateMonitor.Configurations;

public interface IApplicationDatabaseState
{
    /// <summary>Human-readable database startup line for the root URL.</summary>
    string GetDatabaseStatusLine();
}

/// <summary>
/// Tracks EF startup (wait + migrations) so the root page can report DB status without querying the database.
/// </summary>
public sealed class ApplicationDatabaseState : IApplicationDatabaseState
{
    private readonly object _sync = new();
    private Phase _phase = Phase.Initializing;
    private string? _failureDetail;

    private enum Phase
    {
        Initializing,
        WaitingOrMigrating,
        Ready,
        Failed
    }

    public void SetWaitingOrMigrating()
    {
        lock (_sync)
        {
            if (_phase is Phase.Ready)
                return;
            _phase = Phase.WaitingOrMigrating;
            _failureDetail = null;
        }
    }

    public void SetReady()
    {
        lock (_sync)
        {
            _phase = Phase.Ready;
            _failureDetail = null;
        }
    }

    public void SetFailed(string detail)
    {
        lock (_sync)
        {
            _phase = Phase.Failed;
            _failureDetail = detail;
        }
    }

    public string GetDatabaseStatusLine()
    {
        lock (_sync)
        {
            return _phase switch
            {
                Phase.Initializing =>
                    "Database: starting; PostgreSQL wait / EF migrations have not begun yet or are still running in the background.",
                Phase.WaitingOrMigrating =>
                    "Database: waiting for PostgreSQL and/or applying EF Core migrations (see logs).",
                Phase.Ready => "Database: connected and migrations are up to date.",
                Phase.Failed =>
                    $"Database: connection or migration failed — {_failureDetail ?? "see logs"}",
                _ => "Database: unknown state."
            };
        }
    }
}
