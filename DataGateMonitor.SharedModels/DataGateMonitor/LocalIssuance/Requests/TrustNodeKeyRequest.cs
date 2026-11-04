namespace DataGateMonitor.SharedModels.DataGateMonitor.LocalIssuance.Requests;

public sealed class TrustNodeKeyRequest
{
    /// <summary>Must equal the pending fingerprint (compare with <c>datagate-profile node-id</c> on the node).</summary>
    public string Fingerprint { get; set; } = string.Empty;
}
