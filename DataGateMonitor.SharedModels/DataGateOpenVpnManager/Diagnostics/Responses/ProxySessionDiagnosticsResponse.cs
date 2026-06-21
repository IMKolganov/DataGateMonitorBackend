namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

/// <summary>
/// Response payload for <c>GET api/diagnostics/proxy-sessions</c>.
/// </summary>
public sealed class ProxySessionDiagnosticsResponse
{
    public DateTime CheckedAtUtc { get; set; }

    public bool ManagementStatusAvailable { get; set; }

    public double? ManagementStatusAgeSeconds { get; set; }

    public int ManagementClientCount { get; set; }

    public bool PeerEvaluationAvailable { get; set; }

    public string? PeerEvaluationSkipReason { get; set; }

    public int ActiveProxySessionCount { get; set; }

    public int ZombieSessionCount { get; set; }

    public string DnsProbeTarget { get; set; } = string.Empty;

    public string DnsProbeScope { get; set; } = string.Empty;

    public string DnsProbeNote { get; set; } = string.Empty;

    public DnsProbeResult DnsProbe { get; set; } = new();

    public IReadOnlyList<ProxySessionDiagnosticItem> Sessions { get; set; } = Array.Empty<ProxySessionDiagnosticItem>();
}
