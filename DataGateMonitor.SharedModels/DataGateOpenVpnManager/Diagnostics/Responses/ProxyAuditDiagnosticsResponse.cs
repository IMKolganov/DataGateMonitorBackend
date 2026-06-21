namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

/// <summary>
/// Response payload for <c>GET api/diagnostics/proxy-audit</c>.
/// </summary>
public sealed class ProxyAuditDiagnosticsResponse
{
    public DateTime CheckedAtUtc { get; set; }

    public int Count { get; set; }

    public IReadOnlyList<ProxySessionAuditEntryDto> Entries { get; set; } = Array.Empty<ProxySessionAuditEntryDto>();
}
