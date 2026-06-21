using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Command.VpnServerClientTable;

public sealed record VpnServerClientUpsertPayload(
    int VpnServerId,
    int? UserId,
    string ExternalId,
    Guid SessionId,
    string CommonName,
    string RemoteIp,
    string? ProxyRealIp,
    string LocalIp,
    long BytesReceived,
    long BytesSent,
    DateTimeOffset ConnectedSince,
    DateTimeOffset? DisconnectedAt,
    string Username,
    string? Country,
    string? Region,
    string? City,
    double? Latitude,
    double? Longitude,
    bool IsConnected)
{
    /// <summary>
    /// Builds an upsert snapshot from a <see cref="VpnServerClient"/> row or in-memory client state.
    /// </summary>
    public static VpnServerClientUpsertPayload FromClient(
        VpnServerClient client,
        bool persistProxyEnrichment = true,
        int? vpnServerId = null,
        int? userId = null,
        string? externalId = null,
        Guid? sessionId = null,
        bool? isConnected = null)
    {
        var connected = isConnected ?? client.IsConnected;

        return new VpnServerClientUpsertPayload(
            VpnServerId: vpnServerId ?? client.VpnServerId,
            UserId: userId ?? client.UserId,
            ExternalId: externalId ?? client.ExternalId,
            SessionId: sessionId ?? client.SessionId,
            CommonName: client.CommonName,
            RemoteIp: client.RemoteIp,
            ProxyRealIp: persistProxyEnrichment ? client.ProxyRealIp : null,
            LocalIp: client.LocalIp,
            BytesReceived: client.BytesReceived,
            BytesSent: client.BytesSent,
            ConnectedSince: client.ConnectedSince,
            DisconnectedAt: connected ? null : client.DisconnectedAt,
            Username: client.Username,
            Country: persistProxyEnrichment ? client.Country : null,
            Region: persistProxyEnrichment ? client.Region : null,
            City: persistProxyEnrichment ? client.City : null,
            Latitude: persistProxyEnrichment ? client.Latitude : null,
            Longitude: persistProxyEnrichment ? client.Longitude : null,
            IsConnected: connected);
    }
}
