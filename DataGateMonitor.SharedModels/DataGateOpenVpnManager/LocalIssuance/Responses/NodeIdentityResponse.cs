namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.LocalIssuance.Responses;

public class NodeIdentityResponse
{
    public string Fingerprint { get; set; } = string.Empty;
    public string PublicKeyPem { get; set; } = string.Empty;
}
