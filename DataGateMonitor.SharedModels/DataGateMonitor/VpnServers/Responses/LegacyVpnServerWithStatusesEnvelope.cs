using DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Dto;

namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

/// <summary>
/// Legacy JSON envelope for <c>GET api/open-vpn-servers/get-all-with-status</c>.
/// Serialized with camelCase (<c>openVpn*</c> keys) for older mobile clients.
/// </summary>
public sealed class LegacyVpnServerWithStatusesEnvelope
{
    public bool Success { get; set; } = true;

    public string Message { get; set; } = "Success";

    public LegacyVpnServerWithStatusesData Data { get; set; } = new();
}

public sealed class LegacyVpnServerWithStatusesData
{
    public List<LegacyVpnServerWithStatusItem> OpenVpnServerWithStatuses { get; set; } = new();
}

public sealed class LegacyVpnServerWithStatusItem
{
    public LegacyVpnServerResponses OpenVpnServerResponses { get; set; } = new();

    public VpnServerStatusLogResponse? OpenVpnServerStatusLogResponse { get; set; }

    public int CountConnectedClients { get; set; }

    public int CountSessions { get; set; }

    public long TotalBytesIn { get; set; }

    public long TotalBytesOut { get; set; }
}

public sealed class LegacyVpnServerResponses
{
    public VpnServerDto OpenVpnServer { get; set; } = new();
}
