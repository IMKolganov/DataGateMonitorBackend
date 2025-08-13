using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

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

public class OpenVpnEventClient(
    OpenVpnServer server,
    ILogger<OpenVpnEventClient> logger,
    IHubContext<OpenVpnEventHub> eventHub,
    IMicroserviceTokenService tokenService,
    IServiceProvider serviceProvider)
{
    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _handlersRegistered;

    // ---- diag fields ----
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
        logger.LogInformation("OpenVpnEventClient started. Status={State}, ConnId={ConnId}, Url={Url}, Host={Host}, Port={Port}",
            s.State, s.ConnectionId, s.Url, s.Host, s.Port);
    }

    public OpenVpnEventConnectionStatus GetStatus()
        => new(
            ServerId: server.Id,
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
        _fullUrl = $"{server.ApiUrl.TrimEnd('/')}/hubs/openvpn-event";
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
                logger.LogInformation("Creating SignalR connection for server {ServerId} (Url={Url}, Host={Host}, Port={Port})",
                    server.Id, _fullUrl, _host, _port);

                _connection = new HubConnectionBuilder()
                    .WithUrl(_fullUrl, options =>
                    {
                        options.AccessTokenProvider = () =>
                            Task.FromResult<string?>(tokenService.GenerateToken(
                                "vpn-cert-issuer", "cert-create", "backend", "DataGateCertManager"));
                    })
                    .WithAutomaticReconnect(new[] { // понятный backoff
                        TimeSpan.Zero, TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15),
                        TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60)
                    })
                    .Build();

                if (!_handlersRegistered)
                {
                    // события от удалённого хоста
                    _connection.On<OpenVpnServerEventLog>("ClientConnected", async data => await HandleEvent("ClientConnected", data));
                    _connection.On<OpenVpnServerEventLog>("ClientDisconnected", async data => await HandleEvent("ClientDisconnected", data));
                    _connection.On<OpenVpnServerEventLog>("ClientAttempted", async data => await HandleEvent("ClientAttempted", data));
                    _connection.On<OpenVpnServerEventLog>("TlsVerified", async data => await HandleEvent("TlsVerified", data));

                    // жизненный цикл коннекта
                    _connection.Reconnecting += ex =>
                    {
                        Stamp(HubConnectionState.Reconnecting, ex);
                        logger.LogWarning(ex, "SignalR reconnecting (ServerId={ServerId}, Host={Host}, Port={Port})",
                            server.Id, _host, _port);
                        return Task.CompletedTask;
                    };
                    _connection.Reconnected += connId =>
                    {
                        Stamp(HubConnectionState.Connected);
                        logger.LogInformation("SignalR reconnected (ServerId={ServerId}, ConnId={ConnId}, Host={Host}, Port={Port})",
                            server.Id, connId, _host, _port);
                        return Task.CompletedTask;
                    };
                    _connection.Closed += ex =>
                    {
                        Stamp(HubConnectionState.Disconnected, ex);
                        logger.LogError(ex, "SignalR closed (ServerId={ServerId}, Host={Host}, Port={Port})",
                            server.Id, _host, _port);
                        return Task.CompletedTask;
                    };

                    _handlersRegistered = true;
                }
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                await _connection.StartAsync(cancellationToken);
                Stamp(HubConnectionState.Connected);
                logger.LogInformation("Started OpenVpnEventClient SignalR connection for server {ServerId}. ConnId={ConnId}, Host={Host}, Port={Port}",
                    server.Id, _connection.ConnectionId, _host, _port);
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
    
    private async Task HandleEvent(string eventType, OpenVpnServerEventLog data)
    {
        var swTotal = Stopwatch.StartNew();
        var group = server.Id.ToString();

        try
        {
            logger.LogInformation(
                "Handling event {EventType} for ServerId={ServerId}; CN={CommonName}; Real={Real}; Virt={Virt}; Since={Since}",
                eventType, server.Id, data.CommonName, data.RealAddress, data.VirtualAddress, data.ConnectedSince);

            using var scope = serviceProvider.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IVpnEventLogService>();

            var rawJson = JsonConvert.SerializeObject(data);
            var jsonLen = rawJson?.Length ?? 0;
            logger.LogDebug("Serialized event {EventType} for ServerId={ServerId}; JsonLength={JsonLen}",
                eventType, server.Id, jsonLen);

            var swSave = Stopwatch.StartNew();
            var log = new OpenVpnServerEventLog
            {
                CommonName = data.CommonName,
                RealAddress = data.RealAddress,
                VirtualAddress = data.VirtualAddress,
                ConnectedSince = data.ConnectedSince,
                Message = data.Message,
                RawJson = rawJson ?? string.Empty
            };

            await logService.SaveEventAsync(server.Id, eventType, log, rawJson, CancellationToken.None);
            swSave.Stop();
            logger.LogInformation(
                "Saved event {EventType} for ServerId={ServerId}; SaveMs={ElapsedMs}",
                eventType, server.Id, swSave.ElapsedMilliseconds);

            var swHub = Stopwatch.StartNew();
            await eventHub.Clients.Group(group).SendAsync(eventType, data);
            swHub.Stop();
            logger.LogInformation(
                "Broadcasted {EventType} to group {Group} (ServerId={ServerId}); HubMs={ElapsedMs}",
                eventType, group, server.Id, swHub.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to handle SignalR event {EventType} from ServerId={ServerId}; CN={CommonName}",
                eventType, server.Id, data.CommonName);
        }
        finally
        {
            swTotal.Stop();
            logger.LogDebug("HandleEvent finished for {EventType} (ServerId={ServerId}); TotalMs={ElapsedMs}",
                eventType, server.Id, swTotal.ElapsedMilliseconds);
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        var s = GetStatus();
        logger.LogInformation("Stopping OpenVpnEventClient (ServerId={ServerId}, State={State}, ConnId={ConnId}, Host={Host}, Port={Port})",
            s.ServerId, s.State, s.ConnectionId, s.Host, s.Port);

        if (_connection is not null)
        {
            try { await _connection.StopAsync(ct); } catch { /* ignore */ }
            try { await _connection.DisposeAsync(); } catch { /* ignore */ }
        }
    }
}
