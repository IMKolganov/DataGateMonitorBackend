namespace OpenVPNGateMonitor.SharedModels.DataGateOpenVpnManager.Cert.Requests;

public class RevokeServerCertificateRequest
{
    public string CommonName { get; set; } = string.Empty;
}