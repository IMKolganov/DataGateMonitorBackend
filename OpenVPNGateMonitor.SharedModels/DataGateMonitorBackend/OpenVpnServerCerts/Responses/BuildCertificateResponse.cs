using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses;

public class BuildCertificateResponse
{
    public MonitorServerCertificate MonitorServerCertificate { get; set; } =  new ();
}