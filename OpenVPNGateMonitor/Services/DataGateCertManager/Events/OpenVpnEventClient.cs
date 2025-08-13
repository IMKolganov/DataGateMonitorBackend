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

    // Reconnect loop guard
    private Task? _reconnectLoopTask;
    private CancellationTokenSource? _reconnectCts;

    private bool _handlersRegistered;

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);

        // Heartbeat to remote hub for liveness (lightweight)
        _heartbeat ??= new PeriodicTimer(TimeSpan.FromSeconds(30));
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
                    logger.LogDebug(ex, "Heartbeat failed (ServerId={ServerId})", server.Id);
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
                                var token = tokenService.GenerateToken(
                                    "vpn-cert-issuer", "cert-create", "backend", "DataGateCertManager");
                                if (string.IsNullOrWhiteSpace(token))
                                    logger.LogError("Generated empty token (ServerId={ServerId})", server.Id);
                                return Task.FromResult<string?>(token);
                            }
                            catch (Exception ex)
                            {
                                logger.LogError(ex, "Token generation failed (ServerId={ServerId})", server.Id);
                                return Task.FromResult<string?>(null);
                            }
                        };
                    })
                    // NOTE: no WithAutomaticReconnect to avoid races; we handle reconnect ourselves.
                    .Build();

                // Only log lifecycle; do not call Start/Stop inside these
                _connection.Closed += error =>
                {
                    logger.LogWarning(error, "SignalR closed (ServerId={ServerId}); starting reconnect loop", server.Id);
                    StartReconnectLoop();
                    return Task.CompletedTask;
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

            if (_connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await _connection.StartAsync(cancellationToken);
                    logger.LogInformation("SignalR connected (ServerId={ServerId}, ConnId={ConnId})",
                        server.Id, _connection.ConnectionId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Initial StartAsync failed (ServerId={ServerId})", server.Id);
                    StartReconnectLoop(); // fire background reconnect loop
                }
            }

            return _connection!;
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

            // Fan-out to local UI hub group for this server
            await eventHub.Clients.Group(server.Id.ToString())
                .SendAsync(eventType, data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle event {EventType} (ServerId={ServerId})", eventType, server.Id);
        }
    }

    // Starts a single reconnect loop if not already running
    private void StartReconnectLoop()
    {
        if (_reconnectLoopTask is { IsCompleted: false }) return;

        _reconnectCts?.Cancel();
        _reconnectCts?.Dispose();
        _reconnectCts = new CancellationTokenSource();

        _reconnectLoopTask = Task.Run(async () =>
        {
            var ct = _reconnectCts.Token;

            // Exponential backoff with jitter, capped at 60s
            var delay = TimeSpan.FromSeconds(2);
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // If connection object not built yet, EnsureConnectionAsync will build it
                    await _connectionLock.WaitAsync(ct);
                    try
                    {
                        if (_connection is null)
                        {
                            // Build connection lazily if somehow null
                            await EnsureConnectionAsync(ct);
                        }
                        else if (_connection.State == HubConnectionState.Disconnected)
                        {
                            await _connection.StartAsync(ct);
                            logger.LogInformation("SignalR reconnected (ServerId={ServerId}, ConnId={ConnId})",
                                server.Id, _connection.ConnectionId);
                            return; // exit loop on success
                        }
                        else if (_connection.State == HubConnectionState.Connected)
                        {
                            return; // already connected
                        }
                    }
                    finally
                    {
                        _connectionLock.Release();
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Throttle to warning; this can be noisy on large fleets
                    logger.LogWarning(ex, "Reconnect attempt failed (ServerId={ServerId})", server.Id);
                }

                // Backoff + jitter (0.8–1.2x)
                var jitter = 0.8 + (Random.Shared.NextDouble() * 0.4);
                var next = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2 * jitter, 60));
                try { await Task.Delay(delay, ct); } catch (OperationCanceledException) { return; }
                delay = next;
            }
        }, _reconnectCts.Token);
    }

    public async Task StopAsync(CancellationToken ct = default)
    {
        _heartbeat?.Dispose();
        _reconnectCts?.Cancel();

        if (_reconnectLoopTask is not null)
        {
            try { await _reconnectLoopTask; } catch { /* ignore */ }
        }

        if (_connection is not null)
        {
            try { await _connection.StopAsync(ct); } catch { /* ignore */ }
            try { await _connection.DisposeAsync(); } catch { /* ignore */ }
        }

        _reconnectCts?.Dispose();
        _reconnectCts = null;
    }
}
