using DataGateMonitor.SharedModels.DataGateOpenVpnManager.PiHole.Dto;

namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.PiHole.Requests;

/// <summary>
/// Batch of DNS queries broadcast from the OpenVPN microservice (SignalR <c>DnsQueriesReceived</c>).
/// </summary>
public sealed class DnsQueryBatchRequest
{
    public DateTimeOffset CollectedAtUtc { get; set; }

    public IReadOnlyList<DnsQueryEventDto> Queries { get; set; } = Array.Empty<DnsQueryEventDto>();
}
