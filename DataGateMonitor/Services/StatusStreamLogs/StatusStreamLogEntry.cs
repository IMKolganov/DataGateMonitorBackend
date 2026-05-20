namespace DataGateMonitor.Services.StatusStreamLogs;

public sealed class StatusStreamLogEntry
{
    public DateTimeOffset TimestampUtc { get; init; }

    public string PayloadJson { get; init; } = string.Empty;

    public string Source { get; init; } = "memory";
}
