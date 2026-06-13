using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Mapster;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using DataGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;
using DataGateMonitor.SharedModels.DataGateOpenVpnManager.VpnEvent.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Events;

public class OpenVpnEventClient(
    VpnServer openVpnServer,
    ILogger<OpenVpnEventClient> logger,
    IHubContext<OpenVpnEventHub> eventHub,
    IMicroserviceTokenService tokenService,
    IServiceScopeFactory scopeFactory)
{
    private readonly VpnServer _openVpnServer = openVpnServer;

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
    private bool _notifiedEventHubConnectionFailed;

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);
        // var s = GetStatus();
        // logger.LogInformation(
        //     "OpenVpnEventClient started. Status={State}, ConnId={ConnId}, Url={Url}, Host={Host}, Port={Port}",
        //     s.State, s.ConnectionId, s.Url, s.Host, s.Port);
    }

    /// <summary>Stops and disposes the SignalR connection (e.g. when server is updated and client is removed from cache).</summary>
    public async Task StopAsync()
    {
        await _connectionLock.WaitAsync(CancellationToken.None);
        try
        {
            if (_connection is not null)
            {
                try { await _connection.StopAsync(CancellationToken.None); } catch { /* ignore */ }
                try { await _connection.DisposeAsync(); } catch { /* ignore */ }
                _connection = null;
                _handlersRegistered = false;
                logger.LogInformation("Stopped OpenVpnEventClient for server {ServerId}", _openVpnServer.Id);
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public ConnectionStatusResponse GetStatus()
    {
        return new ConnectionStatusResponse()
        {
            ConnectionStatus = new ConnectionStatusDto()
            {
                ServerId = _openVpnServer.Id,
                Url = _fullUrl,
                Host = _host,
                Port = _port,
                State = _lastState.ToString(),
                ConnectionId = _connection?.ConnectionId,
                LastStateChangedUtc = _lastStateChangedUtc,
                LastReconnectedUtc = _lastReconnectedUtc,
                LastClosedUtc = _lastClosedUtc,
                LastError = _lastError
            }
        };
    }

    private void InitTargetUrl()
    {
        if (!string.IsNullOrEmpty(_fullUrl)) return;
        _fullUrl = $"{_openVpnServer.ApiUrl.TrimEnd('/')}/hubs/openvpn-event";
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
                    _openVpnServer.Id, _fullUrl, _host, _port);

                _connection = new HubConnectionBuilder()
                    .WithUrl(_fullUrl, options =>
                    {
                        options.AccessTokenProvider = () =>
                            Task.FromResult<string?>(tokenService.GenerateToken(
                                "vpn-cert-issuer", "cert-create", "backend", "DataGateOpenVpnManager"));
                    })
                    .AddNewtonsoftJsonProtocol(options => options.PayloadSerializerSettings = ProjectJson.WebSettings)
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
                            _openVpnServer.Id, _host, _port);
                        return Task.CompletedTask;
                    };
                    _connection.Reconnected += connId =>
                    {
                        Stamp(HubConnectionState.Connected);
                        logger.LogInformation(
                            "SignalR reconnected (ServerId={ServerId}, ConnId={ConnId}, Host={Host}, Port={Port})",
                            _openVpnServer.Id, connId, _host, _port);
                        return Task.CompletedTask;
                    };
                    _connection.Closed += ex =>
                    {
                        Stamp(HubConnectionState.Disconnected, ex);
                        logger.LogError(ex,
                            "SignalR closed (ServerId={ServerId}, Host={Host}, Port={Port})",
                            _openVpnServer.Id, _host, _port);
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
                        logger.LogInformation(
                            "Attempting SignalR connect: ServerId={ServerId}, Attempt={Attempt}, Url={Url}, Host={Host}, Port={Port}",
                            _openVpnServer.Id, attempt, _fullUrl, _host, _port);
                        await _connection.StartAsync(cancellationToken);
                        _notifiedEventHubConnectionFailed = false;
                        Stamp(HubConnectionState.Connected);
                        logger.LogInformation(
                            "Started OpenVpnEventClient SignalR connection for server {ServerId}. ConnId={ConnId}, Host={Host}, Port={Port}",
                            _openVpnServer.Id, _connection.ConnectionId, _host, _port);
                        break;
                    }
                    catch (Exception ex)
                    {
                        var innerMsg = ex.InnerException?.Message ?? ex.Message;
                        logger.LogWarning(ex,
                            "SignalR start failed (attempt {Attempt}) for server {ServerId}, Url={Url}. Inner={Inner}. Retrying in 5s...",
                            attempt, _openVpnServer.Id, _fullUrl, innerMsg);

                        if (!_notifiedEventHubConnectionFailed)
                        {
                            _notifiedEventHubConnectionFailed = true;
                            try
                            {
                                using var notifyScope = scopeFactory.CreateScope();
                                var notifySvc = notifyScope.ServiceProvider.GetRequiredService<IOpenVpnMicroserviceNotificationService>();
                                await notifySvc.NotifyEventHubConnectionFailed(_openVpnServer.Id, _openVpnServer.ServerName, innerMsg, CancellationToken.None);
                            }
                            catch (Exception notifyEx)
                            {
                                logger.LogWarning(notifyEx, "Failed to send event-hub connection-failed notification for server {ServerId}", _openVpnServer.Id);
                            }
                        }

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
        var group = _openVpnServer.Id.ToString();

        try
        {
            logger.LogInformation(
                "Handling event {EventType} for ServerId={ServerId}; CN={CommonName}; Real={Real}; Virt={Virt}; Since={Since}",
                eventType, _openVpnServer.Id, data.CommonName, data.RealAddress, data.VirtualAddress,
                data.ConnectedSince);

            using var scope = scopeFactory.CreateScope();

            var logService = scope.ServiceProvider.GetRequiredService<IVpnEventLogService>();
            var fileQuery = scope.ServiceProvider.GetRequiredService<IIssuedOvpnFileQueryService>();
            var proxyLookup = scope.ServiceProvider.GetRequiredService<IProxyClientLookupService>();
            var clientCmd = scope.ServiceProvider.GetRequiredService<ICommandService<VpnServerClient, int>>();

            var req = data.Adapt<VpnEventRequest>();

            var swSave = Stopwatch.StartNew();
            await logService.SaveEventAsync(_openVpnServer.Id, eventType, req, CancellationToken.None);

            if (eventType.Equals("ClientConnected", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(req.CommonName))
            {
                var openVpnServerClient = new VpnServerClient
                {
                    VpnServerId = _openVpnServer.Id,
                    ExternalId = await fileQuery.GetExternalIdByCommonName(
                        req.CommonName, _openVpnServer.Id, CancellationToken.None) ?? string.Empty,
                    CommonName = req.CommonName!,
                    RemoteIp = req.RealAddress ?? string.Empty,
                    LocalIp = req.VirtualAddress ?? string.Empty,
                    BytesReceived = req.BytesReceived ?? 0,
                    BytesSent = req.BytesSent ?? 0,
                    ConnectedSince = req.ConnectedSince ?? DateTimeOffset.UtcNow,
                    DisconnectedAt = req.DisconnectedAt,
                    Username = req.CommonName,
                    IsConnected = true,
                    LastUpdate = DateTimeOffset.UtcNow,
                    CreateDate = DateTimeOffset.UtcNow
                };

                await proxyLookup.EnrichFromManagementRealAddressAsync(_openVpnServer, openVpnServerClient,
                    CancellationToken.None);

                openVpnServerClient.SessionId = GenerateSessionId(
                    openVpnServerClient.CommonName, openVpnServerClient.RemoteIp, openVpnServerClient.ConnectedSince);

                await clientCmd.Add(openVpnServerClient, saveChanges: false, CancellationToken.None);
                logger.LogInformation("VpnServerId: {Id}. Added new client session {SessionId}.",
                    _openVpnServer.Id, openVpnServerClient.SessionId);
            }
            else if (eventType.Equals("ClientDisconnected", StringComparison.OrdinalIgnoreCase)
                     && !string.IsNullOrWhiteSpace(req.CommonName))
            {
                var nowUtc = DateTimeOffset.UtcNow;

                await clientCmd.UpdateWhere(
                    x => x.VpnServerId == _openVpnServer.Id
                         && x.IsConnected
                         && x.CommonName == req.CommonName
                         && x.ConnectedSince == req.ConnectedSince
                         && x.RemoteIp == req.RealAddress,
                    s => s
                        .SetProperty(c => c.IsConnected, false)
                        .SetProperty(c => c.DisconnectedAt, nowUtc)
                        .SetProperty(c => c.LastUpdate, nowUtc),
                    CancellationToken.None);
            }

            // save while scope (DbContext) is still alive
            await clientCmd.SaveChanges(CancellationToken.None);

            swSave.Stop();
            logger.LogInformation("Saved event {EventType} for ServerId={ServerId}; SaveMs={ElapsedMs}",
                eventType, _openVpnServer.Id, swSave.ElapsedMilliseconds);

            var swHub = Stopwatch.StartNew();
            await eventHub.Clients.Group(group).SendAsync(eventType, data);
            swHub.Stop();
            logger.LogInformation("Broadcasted {EventType} to group {Group} (ServerId={ServerId}); HubMs={ElapsedMs}",
                eventType, group, _openVpnServer.Id, swHub.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to handle SignalR event {EventType} from ServerId={ServerId}; CN={CommonName}",
                eventType, _openVpnServer.Id, data.CommonName);
        }
        finally
        {
            swTotal.Stop();
            logger.LogDebug("HandleEvent finished for {EventType} (ServerId={ServerId}); TotalMs={ElapsedMs}",
                eventType, _openVpnServer.Id, swTotal.ElapsedMilliseconds);
        }
    }

    private Guid GenerateSessionId(string commonName, string realAddress, DateTimeOffset connectedSince)
    {
        var sessionString = $"{commonName}-{realAddress}-{connectedSince:o}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sessionString));
        return new Guid(hashBytes.Take(16).ToArray());
    }
}