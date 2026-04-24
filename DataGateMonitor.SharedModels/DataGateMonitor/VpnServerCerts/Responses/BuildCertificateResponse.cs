using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses;

public class BuildCertificateResponse
{
    public MonitorServerCertificate MonitorServerCertificate { get; set; } =  new ();
}