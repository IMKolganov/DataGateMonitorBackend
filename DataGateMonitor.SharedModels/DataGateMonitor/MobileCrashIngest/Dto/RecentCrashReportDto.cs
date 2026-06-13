namespace DataGateMonitor.SharedModels.DataGateMonitor.MobileCrashIngest.Dto;

public sealed class RecentCrashReportDto
{
    public long Id { get; set; }
    public DateTime ReceivedAt { get; set; }
    public string AppProcess { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string ParseStatus { get; set; } = null!;
    public DateTime? TimestampUtc { get; set; }
    public string? Process { get; set; }
    public string? Thread { get; set; }
    public string? Sdk { get; set; }
    public string? Device { get; set; }
    public string? Kind { get; set; }
    public string? Exception { get; set; }
    public string? Message { get; set; }
    public string? Tag { get; set; }
    public string? AppVersion { get; set; }
    public string? Stacktrace { get; set; }
    public string? PayloadRaw { get; set; }
}
