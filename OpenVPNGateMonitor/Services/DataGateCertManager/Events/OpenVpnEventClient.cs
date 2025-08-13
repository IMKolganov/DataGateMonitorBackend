using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class OpenVpnEventClient(
    OpenVpnServer server,
    ILogger<OpenVpnEventClient> logger,
    IHubContext<OpenVpnEventHub> eventHub,
    IMicroserviceTokenService tokenService,
    IServiceProvider serviceProvider)
{
    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private PeriodicTimer? _heartbeat;
    private bool _handlersRegistered;

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);

        // Heartbeat to remote hub for liveness
        _heartbeat ??= new PeriodicTimer(TimeSpan.FromSeconds(20));
        _ = Task.Run(async () =>
        {
            while (await _heartbeat.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    if (_connection?.State == HubConnectionState.Connected)
                        await _connection.SendAsync("Ping", $"ovpn-srv-{server.Id}", cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Heartbeat failed for server {ServerId}", server.Id);
                }
            }
        }, cancellationToken);
    }

    private async Task<HubConnection> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is { State: HubConnectionState.Connected })
                return _connection;

            if (_connection is null)
            {
                var fullUrl = $"{server.ApiUrl.TrimEnd('/')}/hubs/openvpn-event";
                logger.LogInformation("Creating SignalR connection: {Url} (ServerId={ServerId})", fullUrl, server.Id);

                _connection = new HubConnectionBuilder()
                    .WithUrl(fullUrl, options =>
                    {
                        options.AccessTokenProvider = () =>
                        {
                            try
                            {
                                // Generate short-lived token on each connect/reconnect
                                var token = tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", "DataGateCertManager");
                                return Task.FromResult<string?>(token);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Failed to generate token for ServerId={ServerId}", server.Id);
                                return Task.FromResult<string?>(null);
                            }
                        };
                    })
                    .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                    .Build();

                // Lifecycle logging and recovery
                _connection.Reconnecting += error =>
                {
                    logger.LogWarning(error, "SignalR reconnecting... (ServerId={ServerId})", server.Id);
                    return Task.CompletedTask;
                };

                _connection.Reconnected += connectionId =>
                {
                    logger.LogInformation("SignalR reconnected. ConnId={ConnId}, ServerId={ServerId}", connectionId, server.Id);
                    return Task.CompletedTask;
                };

                _connection.Closed += async error =>
                {
                    logger.LogWarning(error, "SignalR connection closed. Starting reconnect loop (ServerId={ServerId})", server.Id);
                    await TryReconnectLoopAsync(_connection, server.Id, CancellationToken.None);
                };

                if (!_handlersRegistered)
                {
                    _connection.On<OpenVpnServerEventLog>("ClientConnected", data => HandleEvent("ClientConnected", data));
                    _connection.On<OpenVpnServerEventLog>("ClientDisconnected", data => HandleEvent("ClientDisconnected", data));
                    _connection.On<OpenVpnServerEventLog>("ClientAttempted", data => HandleEvent("ClientAttempted", data));
                    _connection.On<OpenVpnServerEventLog>("TlsVerified", data => HandleEvent("TlsVerified", data));
                    _handlersRegistered = true;
                }
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                await _connection.StartAsync(cancellationToken);
                logger.LogInformation("Started OpenVpnEventClient connection. State={State}, ServerId={ServerId}",
                    _connection.State, server.Id);
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
        try
        {
            using var scope = serviceProvider.CreateScope();
            var logService = scope.ServiceProvider.GetRequiredService<IVpnEventLogService>();

            var rawJson = JsonConvert.SerializeObject(data);

            var log = new OpenVpnServerEventLog
            {
                CommonName = data.CommonName,
                RealAddress = data.RealAddress,
                VirtualAddress = data.VirtualAddress,
                ConnectedSince = data.ConnectedSince,
                Message = data.Message,
                RawJson = rawJson
            };

            await logService.SaveEventAsync(server.Id, eventType, log, rawJson, CancellationToken.None);

            // Broadcast to local UI hub group for this server
            await eventHub.Clients.Group(server.Id.ToString())
                .SendAsync(eventType, data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle event {EventType} from server {ServerId}", eventType, server.Id);
        }
    }

    private static async Task TryReconnectLoopAsync(HubConnection conn, long serverId, CancellationToken ct)
    {
        // Simple exponential backoff with max 30s
        var delay = TimeSpan.FromSeconds(2);
        while (!ct.IsCancellationRequested && conn.State != HubConnectionState.Connected)
        {
            try
            {
                await conn.StartAsync(ct);
                return;
            }
            catch
            {
                // swallow and backoff
            }

            try
            {
                await Task.Delay(delay, ct);
            }
            catch (TaskCanceledException) { }

            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
        }
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _heartbeat?.Dispose();
        if (_connection is not null)
        {
            try { await _connection.StopAsync(ct); } catch { /* ignore */ }
            try { await _connection.DisposeAsync(); } catch { /* ignore */ }
        }
    }
}
