namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto;

public sealed class VpnDnsQueryLogDto
{
    public int Id { get; set; }

    public int VpnServerId { get; set; }

    public long PiHoleQueryId { get; set; }

    public int? UserId { get; set; }

    public string? ExternalId { get; set; }

    public string? CommonName { get; set; }

    public string ClientIp { get; set; } = string.Empty;

    public string Domain { get; set; } = string.Empty;

    public string? QueryType { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTimeOffset QueriedAtUtc { get; set; }
}
