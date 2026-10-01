namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerClients.Dto;

public class OverviewSummaryDto
{
    public long TotalTrafficInBytes { get; set; }
    public long TotalTrafficOutBytes { get; set; }

    /// <summary>Max concurrent sessions (distinct SessionId) in any series bucket that had samples.</summary>
    public int PeakActiveClients { get; set; }

    /// <summary>Bucket start (UTC) where <see cref="PeakActiveClients"/> occurred.</summary>
    public DateTimeOffset? PeakActiveClientsAt { get; set; }

    /// <summary>Min concurrent sessions among buckets that had samples (excludes zero-filled gaps).</summary>
    public int LowActiveClients { get; set; }

    /// <summary>Bucket start (UTC) where <see cref="LowActiveClients"/> occurred.</summary>
    public DateTimeOffset? LowActiveClientsAt { get; set; }
}
