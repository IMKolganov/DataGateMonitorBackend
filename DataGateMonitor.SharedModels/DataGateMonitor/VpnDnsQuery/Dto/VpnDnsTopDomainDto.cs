namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto;

public sealed class VpnDnsTopDomainDto
{
    public string Domain { get; set; } = string.Empty;

    public int UniqueUsersCount { get; set; }

    public int QueryCount { get; set; }
}
