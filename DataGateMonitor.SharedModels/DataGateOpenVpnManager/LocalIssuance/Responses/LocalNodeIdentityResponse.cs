namespace DataGateMonitor.SharedModels.DataGateOpenVpnManager.LocalIssuance.Responses;

public sealed class LocalNodeIdentityResponse : NodeIdentityResponse
{
    public int PendingEvents { get; set; }
}
