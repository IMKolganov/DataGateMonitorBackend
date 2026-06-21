namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.Diagnostics.Responses;

/// <summary>
/// One proxy-session audit record returned by <c>GET api/diagnostics/proxy-audit</c>.
/// </summary>
public sealed class ProxySessionAuditEntryDto
{
    public DateTime AtUtc { get; set; }

    public string Event { get; set; } = string.Empty;

    public string? ConnectionId { get; set; }

    public string? Decision { get; set; }

    public string? Reason { get; set; }

    public IReadOnlyDictionary<string, string>? Details { get; set; }
}
