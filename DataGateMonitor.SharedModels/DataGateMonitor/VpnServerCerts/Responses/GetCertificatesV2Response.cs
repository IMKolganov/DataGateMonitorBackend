using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses.Dto;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses;

/// <summary>v2 paged certificates.</summary>
public class GetCertificatesV2Response
{
    public PagedResponse<MonitorServerCertificate> Certificates { get; set; } = new();
}
