using System.Diagnostics;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateCertManager.VpnEvent.Requests;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class OpenVpnEventClient(
    OpenVpnServer openVpnServer,
    ILogger<OpenVpnEventClient> logger,
    IHubContext<OpenVpnEventHub> eventHub,
    IMicroserviceTokenService tokenService,
    IIssuedOvpnFileQueryService openVpnFileQueryService,
    IServiceProvider serviceProvider)
{
    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _handlersRegistered;

    // ---- diagnostics ----
    private string _fullUrl = "";
    private string _host = "";
    private int _port;
    private HubConnectionState _lastState = HubConnectionState.Disconnected;
    private DateTimeOffset _lastStateChangedUtc = DateTimeOffset.MinValue;
    private DateTimeOffset? _lastReconnectedUtc;
    private DateTimeOffset? _lastClosedUtc;
    private string? _lastError;

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);
        var s = GetStatus();
        logger.LogInformation(
            "OpenVpnEventClient started. Status={State}, ConnId={ConnId}, Url={Url}, Host={Host}, Port={Port}",
            s.State, s.ConnectionId, s.Url, s.Host, s.Port);
    }

    private OpenVpnEventConnectionStatus GetStatus()
        => new(
            ServerId: openVpnServer.Id,
            Url: _fullUrl,
            Host: _host,
            Port: _port,
            State: _connection?.State ?? HubConnectionState.Disconnected,
            ConnectionId: _connection?.ConnectionId,
            LastStateChangedUtc: _lastStateChangedUtc,
            LastReconnectedUtc: _lastReconnectedUtc,
            LastClosedUtc: _lastClosedUtc,
            LastError: _lastError
        );

    private void InitTargetUrl()
    {
        if (!string.IsNullOrEmpty(_fullUrl)) return;
        _fullUrl = $"{openVpnServer.ApiUrl.TrimEnd('/')}/hubs/openvpn-event";
        var uri = new Uri(_fullUrl);
        _host = uri.Host;
        _port = uri.IsDefaultPort ? (uri.Scheme == Uri.UriSchemeHttps ? 443 : 80) : uri.Port;
    }

    private void Stamp(HubConnectionState state, Exception? error = null)
    {
        _lastState = state;
        _lastStateChangedUtc = DateTimeOffset.UtcNow;
        _lastError = error?.Message;
        if (state == HubConnectionState.Connected) _lastReconnectedUtc = _lastStateChangedUtc;
        if (error != null) _lastClosedUtc = _lastStateChangedUtc;
    }

    private async Task<HubConnection> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null && _connection.State == HubConnectionState.Connected)
                return _connection;

            if (_connection == null)
            {
                InitTargetUrl();
                logger.LogInformation(
                    "Creating SignalR connection for server {ServerId} (Url={Url}, Host={Host}, Port={Port})",
                    openVpnServer.Id, _fullUrl, _host, _port);

                _connection = new HubConnectionBuilder()
                    .WithUrl(_fullUrl, options =>
                    {
                        options.AccessTokenProvider = () =>
                            Task.FromResult<string?>(tokenService.GenerateToken(
                                "vpn-cert-issuer", "cert-create", "backend", "DataGateCertManager"));
                    })
                    .WithAutomaticReconnect([
                        TimeSpan.Zero, TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)
                    ])
                    .Build();

                _connection.ServerTimeout = TimeSpan.FromSeconds(30);
                _connection.KeepAliveInterval = TimeSpan.FromSeconds(15);

                if (!_handlersRegistered)
                {
                    // --- Register all handlers using the new payload type VpnEventRequest ---
                    _connection.On<VpnEventRequest>("ClientConnected",
                        async data => await HandleEvent("ClientConnected", data));
                    _connection.On<VpnEventRequest>("ClientDisconnected",
                        async data => await HandleEvent("ClientDisconnected", data));
                    _connection.On<VpnEventRequest>("ClientAttempted",
                        async data => await HandleEvent("ClientAttempted", data));
                    _connection.On<VpnEventRequest>("TlsVerified",
                        async data => await HandleEvent("TlsVerified", data));

                    // New unified/typed error channels
                    _connection.On<VpnEventRequest>("ErrorEvent",
                        async data => await HandleEvent("ErrorEvent", data));
                    _connection.On<VpnEventRequest>("AuthFailed",
                        async data => await HandleEvent("AuthFailed", data));
                    _connection.On<VpnEventRequest>("TlsError",
                        async data => await HandleEvent("TlsError", data));
                    _connection.On<VpnEventRequest>("VerifyError",
                        async data => await HandleEvent("VerifyError", data));
                    _connection.On<VpnEventRequest>("VpnError",
                        async data => await HandleEvent("VpnError", data));

                    // Optional: env dumps if you broadcast them
                    _connection.On<object>("EnvDumpReceived",
                        async _ => await Task.CompletedTask);

                    _connection.Reconnecting += ex =>
                    {
                        Stamp(HubConnectionState.Reconnecting, ex);
                        logger.LogWarning(ex,
                            "SignalR reconnecting (ServerId={ServerId}, Host={Host}, Port={Port})",
                            openVpnServer.Id, _host, _port);
                        return Task.CompletedTask;
                    };
                    _connection.Reconnected += connId =>
                    {
                        Stamp(HubConnectionState.Connected);
                        logger.LogInformation(
                            "SignalR reconnected (ServerId={ServerId}, ConnId={ConnId}, Host={Host}, Port={Port})",
                            openVpnServer.Id, connId, _host, _port);
                        return Task.CompletedTask;
                    };
                    _connection.Closed += ex =>
                    {
                        Stamp(HubConnectionState.Disconnected, ex);
                        logger.LogError(ex,
                            "SignalR closed (ServerId={ServerId}, Host={Host}, Port={Port})",
                            openVpnServer.Id, _host, _port);
                        return Task.CompletedTask;
                    };

                    _handlersRegistered = true;
                }
            }

            // First connect retry loop
            if (_connection.State != HubConnectionState.Connected)
            {
                var attempt = 0;
                while (_connection.State != HubConnectionState.Connected)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    attempt++;

                    try
                    {
                        await _connection.StartAsync(cancellationToken);
                        Stamp(HubConnectionState.Connected);
                        logger.LogInformation(
                            "Started OpenVpnEventClient SignalR connection for server {ServerId}. ConnId={ConnId}, Host={Host}, Port={Port}",
                            openVpnServer.Id, _connection.ConnectionId, _host, _port);
                        break;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex,
                            "SignalR start failed (attempt {Attempt}) for server {ServerId}. Retrying in 5s...",
                            attempt, openVpnServer.Id);

                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }
                }
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task HandleEvent(string eventType, VpnEventRequest data)
    {
        var swTotal = Stopwatch.StartNew();
        var group = openVpnServer.Id.ToString();

        try
        {
            logger.LogInformation(
                "Handling event {EventType} for ServerId={ServerId}; CN={CommonName}; Real={Real}; Virt={Virt}; Since={Since}",
                eventType, openVpnServer.Id, data.CommonName, data.RealAddress, data.VirtualAddress, data.ConnectedSince);

            using var scope = serviceProvider.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IVpnEventLogService>();
            // var logService = scope.ServiceProvider.GetRequiredService<IVpnEventLogService>();


            // Build request that mirrors the table and overrides ServerId/EventType
            var req = data.Adapt<VpnEventRequest>();
            
            var swSave = Stopwatch.StartNew();
            await logService.SaveEventAsync(openVpnServer.Id, eventType, req, CancellationToken.None);

            // if (string.IsNullOrEmpty(req.CommonName) && req.CommonName != null)
            // {
            //     var newClient = new OpenVpnServerClient
            //     {
            //         VpnServerId = openVpnServer.Id,
            //         ExternalId = await openVpnFileQueryService.GetExternalIdByCommonName(
            //             req.CommonName, openVpnServer.Id, false, CancellationToken.None) ?? string.Empty,
            //         SessionId = sessionId,
            //         CommonName = req.CommonName,
            //         RemoteIp = req.RealAddress ?? string.Empty,
            //         LocalIp = req.VirtualAddress ?? string.Empty,
            //         BytesReceived = req.BytesReceived ?? 0,
            //         BytesSent = req.BytesSent ?? 0,
            //         ConnectedSince = req.ConnectedSince ?? DateTimeOffset.UtcNow,
            //         DisconnectedAt = req.DisconnectedAt,
            //         Username = req.CommonName,
            //         Country = openVpnClient.Country,
            //         Region = openVpnClient.Region,
            //         City = openVpnClient.City,
            //         Latitude = openVpnClient.Latitude,
            //         Longitude = openVpnClient.Longitude,
            //         IsConnected = true,
            //         LastUpdate = DateTimeOffset.UtcNow,
            //         CreateDate = DateTimeOffset.UtcNow
            //     };
            // }

            
            swSave.Stop();
            logger.LogInformation(
                "Saved event {EventType} for ServerId={ServerId}; SaveMs={ElapsedMs}",
                eventType, openVpnServer.Id, swSave.ElapsedMilliseconds);

            // forward to local SignalR subscribers (same payload as received)
            var swHub = Stopwatch.StartNew();
            await eventHub.Clients.Group(group).SendAsync(eventType, data);
            swHub.Stop();
            logger.LogInformation(
                "Broadcasted {EventType} to group {Group} (ServerId={ServerId}); HubMs={ElapsedMs}",
                eventType, group, openVpnServer.Id, swHub.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to handle SignalR event {EventType} from ServerId={ServerId}; CN={CommonName}",
                eventType, openVpnServer.Id, data.CommonName);
        }
        finally
        {
            swTotal.Stop();
            logger.LogDebug("HandleEvent finished for {EventType} (ServerId={ServerId}); TotalMs={ElapsedMs}",
                eventType, openVpnServer.Id, swTotal.ElapsedMilliseconds);
        }
    }
}