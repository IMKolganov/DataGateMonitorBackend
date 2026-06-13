using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServerCerts.Responses;

public class RevokeCertificateResponse
{
    public MonitorServerCertificate MonitorServerCertificate { get; set; } =  new ();
}