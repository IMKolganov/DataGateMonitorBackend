using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using OpenVPNGateMonitor.Services.Others.Notifications.OpenVpnMicroserviceClient;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnMicroserviceClient(
    OpenVpnServer server,
    ILogger<OpenVpnMicroserviceClient> logger,
    IHubContext<OpenVpnFrontendHub> frontendHub,
    IMicroserviceTokenService tokenService,
    IOpenVpnMicroserviceNotificationService microserviceNotificationService,
    IHubConnectionFactory? hubConnectionFactory = null) : IOpenVpnMicroserviceClient
{
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingCommands = new();
    private IHubConnectionProxy? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _handlersRegistered = false;
    private string? _lastApiUrl;
    private bool _disposed;
    public string CurrentApiUrl => server.ApiUrl;
    private readonly IHubConnectionFactory _hubFactory = hubConnectionFactory ?? new DefaultHubConnectionFactory();

    public async Task<string> SendCommandWithResponseAsync(string command, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCommands[requestId] = tcs;

        try
        {
            var connection = await EnsureConnectionAsync(cancellationToken);
            await connection.InvokeAsync("SendCommandWithRequestId", requestId, command, cancellationToken);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            await using (linkedCts.Token.Register(() => tcs.TrySetCanceled()))
            {
                return await tcs.Task;
            }
        }
        finally
        {
            _pendingCommands.TryRemove(requestId, out _);
        }
    }

    public async Task SendCommandAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString("N");
            var connection = await EnsureConnectionAsync(cancellationToken);

            if (connection.State != HubConnectionState.Connected)
            {
                logger.LogWarning("SignalR connection is {State}, trying to reconnect...", connection.State);
                await ReconnectAsync(connection);
            }

            await connection.InvokeAsync("SendCommand", requestId, command, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command to microservice for server {ServerId}", server.Id);
            var errorMessage = $"[Error] Failed to send command to server {server.Id}: {ex.Message}";
            await frontendHub.Clients.Group(server.Id.ToString())
                .SendAsync("ReceiveCommandResult", errorMessage, cancellationToken);
            await microserviceNotificationService.NotifySendCommandFailed(server.Id, server.ServerName, ex.Message, CancellationToken.None);
        }
    }

    public async Task SendCommandToMicroserviceAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var requestId = Guid.NewGuid().ToString("N");
            var connection = await EnsureConnectionAsync(cancellationToken);

            if (connection.State != HubConnectionState.Connected)
            {
                logger.LogWarning("SignalR connection is {State}, trying to reconnect...", connection.State);
                await ReconnectAsync(connection);
            }

            await connection.InvokeAsync("SendCommand", requestId, command, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command to microservice for server {ServerId}", server.Id);
            var errorMessage = $"[Error] Failed to send command to server {server.Id}: {ex.Message}";
            await frontendHub.Clients.Group(server.Id.ToString())
                .SendAsync("ReceiveCommandResult", errorMessage, cancellationToken);
            await microserviceNotificationService.NotifySendCommandFailed(server.Id, server.ServerName, ex.Message, CancellationToken.None);
        }
    }

    private async Task<IHubConnectionProxy> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null && server.ApiUrl != _lastApiUrl)
            {
                logger.LogWarning("Detected API URL change for server {ServerId}. Recreating SignalR connection...", server.Id);
                await _connection.DisposeAsync();
                _connection = null;
                _handlersRegistered = false;
            }

            if (_connection is null)
            {
                logger.LogInformation("Creating SignalR connection for server {ServerId}", server.Id);

                var fullUrl = $"{server.ApiUrl.TrimEnd('/')}/hubs/openvpn";
                _lastApiUrl = server.ApiUrl;

                _connection = _hubFactory.Create(fullUrl, () =>
                    Task.FromResult<string?>(tokenService.GenerateToken("vpn-cert-issuer", "cert-create", "backend", "DataGateOpenVpnManager")));

                if (!_handlersRegistered)
                {
                    _connection.On<string>("ReceiveCommandResult", async result =>
                    {
                        await frontendHub.Clients.Group(server.Id.ToString())
                            .SendAsync("ReceiveCommandResult", result);
                    });

                    _connection.On<string>("ReceiveMessage", async message =>
                    {
                        await frontendHub.Clients.Group(server.Id.ToString())
                            .SendAsync("ReceiveMessage", message);
                    });

                    _connection.On<string, string>("ReceiveCommandResultWithRequestId", (requestId, result) =>
                    {
                        if (_pendingCommands.TryRemove(requestId, out var tcs))
                            tcs.TrySetResult(result);
                    });

                    _handlersRegistered = true;
                }
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                await _connection.StartAsync(cancellationToken);
                logger.LogInformation("Started SignalR connection for server {ServerId}", server.Id);
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task ReconnectAsync(IHubConnectionProxy connection)
    {
        try
        {
            await connection.StopAsync();
            await connection.StartAsync();
            logger.LogInformation("Reconnected to SignalR for server {ServerId}", server.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reconnect to SignalR for server {ServerId}", server.Id);
            await microserviceNotificationService.NotifyReconnectFailed(server.Id, server.ServerName, ex.Message, CancellationToken.None);
            throw;
        }
    }

    public void Dispose()
    {
        DisposeAsyncCore().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        var vt = DisposeAsyncCore();
        GC.SuppressFinalize(this);
        return vt;
    }

    private async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;

        // Cancel any pending command waiters
        foreach (var kv in _pendingCommands)
        {
            try { kv.Value.TrySetCanceled(); } catch { /* ignore */ }
        }
        _pendingCommands.Clear();

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                try { await _connection.StopAsync(); } catch { /* ignore */ }
                try { await _connection.DisposeAsync(); } catch { /* ignore */ }
                _connection = null;
            }
            _handlersRegistered = false;
            _disposed = true;
        }
        finally
        {
            _connectionLock.Release();
            _connectionLock.Dispose();
        }
    }
}