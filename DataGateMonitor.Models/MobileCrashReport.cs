using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

public class MobileCrashReport : BaseEntity<long>
{
    [Required, MaxLength(256)]
    public string AppProcess { get; set; } = null!;

    [Required, MaxLength(512)]
    public string FileName { get; set; } = null!;

    [Required]
    public string PayloadRaw { get; set; } = null!;

    [Required, MaxLength(32)]
    public string ParseStatus { get; set; } = null!;

    public DateTimeOffset? TimestampUtc { get; set; }

    [MaxLength(256)]
    public string? Process { get; set; }

    [MaxLength(256)]
    public string? Thread { get; set; }

    [MaxLength(128)]
    public string? Sdk { get; set; }

    [MaxLength(256)]
    public string? Device { get; set; }

    [MaxLength(32)]
    public string? Kind { get; set; }

    [MaxLength(512)]
    public string? Exception { get; set; }

    [MaxLength(4000)]
    public string? Message { get; set; }

    [MaxLength(256)]
    public string? Tag { get; set; }

    public string? Stacktrace { get; set; }
}
