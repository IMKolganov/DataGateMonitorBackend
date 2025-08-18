using Microsoft.AspNetCore.SignalR.Client;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public record OpenVpnEventConnectionStatus(
    int ServerId,
    string Url,
    string Host,
    int Port,
    HubConnectionState State,
    string? ConnectionId,
    DateTimeOffset LastStateChangedUtc,
    DateTimeOffset? LastReconnectedUtc,
    DateTimeOffset? LastClosedUtc,
    string? LastError
);