namespace DataGateMonitor.Services.Api.MobileCrashIngest;

public sealed class CrashReportParseResult
{
    public bool IsParsed { get; init; }

    public DateTimeOffset? TimestampUtc { get; init; }

    public string? Process { get; init; }

    public string? Thread { get; init; }

    public string? Sdk { get; init; }

    public string? Device { get; init; }

    public string? Kind { get; init; }

    public string? Exception { get; init; }

    public string? Message { get; init; }

    public string? Tag { get; init; }

    public string? AppVersion { get; init; }

    public string? Stacktrace { get; init; }
}
