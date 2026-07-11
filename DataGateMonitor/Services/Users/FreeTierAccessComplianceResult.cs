namespace DataGateMonitor.Services.Users;

public sealed class FreeTierAccessComplianceResult
{
    public bool IsApplicable { get; init; }

    public bool IsCompliant { get; init; }

    public bool IsMergedAccount { get; init; }

    public bool IsChannelSubscribed { get; init; }

    public string? ActivePlanName { get; init; }

    public int? UserId { get; init; }

    public long? TelegramId { get; set; }

    public bool AdminsNotified { get; init; }

    /// <summary>True when access is allowed via the configurable grace window (no channel subscription / merge).</summary>
    public bool IsGracePeriod { get; init; }

    /// <summary>When <see cref="IsGracePeriod"/> is true, when the grace window expires (UTC).</summary>
    public DateTimeOffset? GraceExpiresAtUtc { get; init; }
}
