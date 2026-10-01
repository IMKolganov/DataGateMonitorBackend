namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

public class OverviewUsersSeriesSummaryDto
{
    /// <summary>Max concurrent sessions in any series bucket that had samples.</summary>
    public int PeakActiveSessions { get; set; }

    /// <summary>Bucket start (UTC) where <see cref="PeakActiveSessions"/> occurred.</summary>
    public DateTimeOffset? PeakActiveSessionsAt { get; set; }

    /// <summary>Min concurrent sessions among buckets that had samples (excludes zero-filled gaps).</summary>
    public int LowActiveSessions { get; set; }

    /// <summary>Bucket start (UTC) where <see cref="LowActiveSessions"/> occurred.</summary>
    public DateTimeOffset? LowActiveSessionsAt { get; set; }

    /// <summary>Max concurrent devices (distinct ExternalId) in any series bucket that had samples.</summary>
    public int PeakActiveUsers { get; set; }

    /// <summary>Bucket start (UTC) where <see cref="PeakActiveUsers"/> occurred.</summary>
    public DateTimeOffset? PeakActiveUsersAt { get; set; }

    /// <summary>Min concurrent devices among buckets that had samples (excludes zero-filled gaps).</summary>
    public int LowActiveUsers { get; set; }

    /// <summary>Bucket start (UTC) where <see cref="LowActiveUsers"/> occurred.</summary>
    public DateTimeOffset? LowActiveUsersAt { get; set; }
}
