using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses.Dto;

namespace OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerCerts.Responses;

public class RevokeCertificateResponse
{
    public MonitorServerCertificate MonitorServerCertificate { get; set; } =  new ();
}