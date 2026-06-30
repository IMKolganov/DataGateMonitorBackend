namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

public sealed class PiHoleDiagnosticsResponse
{
    public DateTime CheckedAtUtc { get; set; }

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public bool HasAppPassword { get; set; }

    public int PollIntervalSeconds { get; set; }

    public int BatchSize { get; set; }

    public int LookbackSeconds { get; set; }

    public string ClientSubnetPrefix { get; set; } = string.Empty;

    public bool Authenticated { get; set; }

    public string? Error { get; set; }

    public int SampleQueryCount { get; set; }

    /// <summary>Background collector loop is active on the microservice.</summary>
    public bool CollectorRunning { get; set; }

    public DateTime? RuntimeConfigAppliedAtUtc { get; set; }

    public DateTime? LastPollAtUtc { get; set; }

    public DateTime? LastSuccessfulPollAtUtc { get; set; }

    public string? LastPollError { get; set; }

    public int LastPollQueriesFetched { get; set; }

    public int LastPollQueriesAfterFilter { get; set; }

    public int LastPollQueriesEnriched { get; set; }

    public int LastPollQueriesForwarded { get; set; }

    public DateTime? LastCursorUntilUtc { get; set; }

    /// <summary>Total DNS rows stored in the dashboard DB for this server.</summary>
    public int StoredQueryCount { get; set; }

    public DateTime? LastStoredQueryAtUtc { get; set; }

    /// <summary>Ok, Warning, Error, or Disabled.</summary>
    public string Health { get; set; } = "Unknown";

    public string? HealthMessage { get; set; }
}
