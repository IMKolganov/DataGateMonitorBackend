namespace DataGateMonitor.SharedModels.DataGateMonitor.XrayNode.Responses;

/// <summary>
/// Response for <c>POST api/vpn-servers/{vpnServerId}/xray/kick-user</c> and <c>disable-user</c>.
/// </summary>
public sealed class XrayNodeUserActionResponse
{
    public bool Ok { get; set; } = true;
}
