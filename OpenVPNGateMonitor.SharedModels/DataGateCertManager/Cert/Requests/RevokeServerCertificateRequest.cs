namespace OpenVPNGateMonitor.SharedModels.DataGateCertManager.Cert.Requests;

public class RevokeServerCertificateRequest
{
    public string CommonName { get; set; } = string.Empty;
}