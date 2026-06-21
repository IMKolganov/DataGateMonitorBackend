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
    bool IsConnected);
