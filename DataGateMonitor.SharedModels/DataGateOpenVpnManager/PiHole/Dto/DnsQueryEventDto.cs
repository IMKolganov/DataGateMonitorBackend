namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.PiHole.Dto;

/// <summary>
/// Single DNS query collected from Pi-hole and enriched with OpenVPN identity when available.
/// </summary>
public sealed class DnsQueryEventDto
{
    public long PiHoleQueryId { get; set; }

    public string ClientIp { get; set; } = string.Empty;

    public string? CommonName { get; set; }

    public string Domain { get; set; } = string.Empty;

    public string? QueryType { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset QueriedAtUtc { get; set; }
}
