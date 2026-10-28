using System.ComponentModel.DataAnnotations;

namespace DataGateMonitor.Models;

/// <summary>
/// Queue row for rewriting <see cref="VpnServerClientTraffic.ExternalId"/> after Telegram→Google merge.
/// Processed strictly by <see cref="Id"/> ascending, one job at a time (see TrafficRewriteBackgroundService).
/// </summary>
public class PendingTrafficRewrite : BaseEntity<long>
{
    /// <summary>Telegram (or other) ExternalId that still appears on raw traffic / orphan dailies.</summary>
    [Required, MaxLength(255)]
    public string OldExternalId { get; set; } = string.Empty;

    /// <summary>Survivor Google ExternalId.</summary>
    [Required, MaxLength(255)]
    public string NewExternalId { get; set; } = string.Empty;

    public int SurvivorUserId { get; set; }

    public int MergedUserId { get; set; }

    public PendingTrafficRewriteStatus Status { get; set; } = PendingTrafficRewriteStatus.Pending;

    /// <summary>
    /// Exclusive upper bound of the next MeasuredAt batch (calendar-week cursor on stock PG).
    /// Null until the first pass initializes from max(MeasuredAt) for OldExternalId.
    /// On Timescale later this can store the last processed chunk identity instead.
    /// </summary>
    public DateTimeOffset? NextRangeEndExclusive { get; set; }

    /// <summary>Optional Timescale chunk name of the last fully processed chunk (unused on stock PG).</summary>
    [MaxLength(256)]
    public string? LastProcessedChunkName { get; set; }

    public long RowsUpdated { get; set; }

    [MaxLength(2048)]
    public string? LastError { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
