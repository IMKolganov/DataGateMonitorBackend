using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses;

public class GetAllCertificatesResponse
{
    public List<MonitorServerCertificate> MonitorServerCertificates = [];
}