using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;

public class OpenVpnMicroserviceClient(
    ILogger<OpenVpnMicroserviceClient> logger,
    IHubContext<OpenVpnFrontendHub> frontendHub,
    IMicroserviceTokenService tokenService,
    IServiceScopeFactory scopeFactory)
{
    private readonly Dictionary<int, HubConnection> _connections = new();
    private readonly HashSet<int> _subscribed = new();
    
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingCommands = new();

    public async Task<string> SendCommandWithResponseAsync(int vpnServerId, string command, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingCommands[requestId] = tcs;

        try
        {
            var connection = await EnsureConnectionAsync(vpnServerId, cancellationToken);
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

    public async Task SendCommandToMicroserviceAsync(int vpnServerId, string command, CancellationToken cancellationToken)
    {
        try
        {
            var connection = await EnsureConnectionAsync(vpnServerId, cancellationToken);

            if (connection.State != HubConnectionState.Connected)
            {
                logger.LogWarning("SignalR connection is {State}, trying to reconnect...", connection.State);
                await ReconnectAsync(connection, vpnServerId);
            }

            await connection.InvokeAsync("SendCommand", command);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command to microservice for server {ServerId}", vpnServerId);
            var errorMessage = $"[Error] Failed to send command to server {vpnServerId}: {ex.Message}";
            await frontendHub.Clients.Group(vpnServerId.ToString())
                .SendAsync("ReceiveCommandResult", errorMessage, cancellationToken);
        }
    }

    private async Task<HubConnection> EnsureConnectionAsync(int vpnServerId, CancellationToken cancellationToken)
    {
        if (_connections.TryGetValue(vpnServerId, out var existingConnection))
        {
            return existingConnection;
        }

        logger.LogInformation("Creating SignalR connection for server {ServerId}", vpnServerId);

        using var scope = scopeFactory.CreateScope();
        var vpnDataService = scope.ServiceProvider.GetRequiredService<IVpnDataService>();
        var server = await vpnDataService.GetOpenVpnServer(vpnServerId, cancellationToken);
        if (server is null || string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException($"OpenVPN server {vpnServerId} not found or has no microservice URL");

        var token = tokenService.GenerateToken(
            "vpn-cert-issuer", "cert-create", "backend", "DataGateCertManager");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Generated token is null or empty");

        var fullUrl = $"{server.ApiUrl.TrimEnd('/')}/hubs/openvpn";

        var connection = new HubConnectionBuilder()
            .WithUrl(fullUrl, options => options.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

        // Ensure On(...) handlers are registered only once per serverId
        if (!_subscribed.Contains(vpnServerId))
        {
            connection.On<string>("ReceiveCommandResult", async result =>
            {
                logger.LogInformation("Forwarding ReceiveCommandResult to frontend for server {ServerId}", vpnServerId);
                await frontendHub.Clients.Group(vpnServerId.ToString()).
                    SendAsync("ReceiveCommandResult", result);
            });

            connection.On<string>("ReceiveMessage", async message =>
            {
                await frontendHub.Clients.Group(vpnServerId.ToString()).
                    SendAsync("ReceiveMessage", message);
            });
            
            connection.On<string, string>("ReceiveCommandResultWithRequestId", (requestId, result) =>
            {
                if (_pendingCommands.TryRemove(requestId, out var tcs))
                {
                    tcs.TrySetResult(result);
                }
            });

            _subscribed.Add(vpnServerId);
        }

        await connection.StartAsync(cancellationToken);
        logger.LogInformation("Started SignalR connection for server {ServerId}", vpnServerId);

        _connections[vpnServerId] = connection;

        return connection;
    }
    
    private async Task ReconnectAsync(HubConnection connection, int vpnServerId)
    {
        try
        {
            await connection.StopAsync();
            await connection.StartAsync();
            logger.LogInformation("Reconnected to SignalR for server {ServerId}", vpnServerId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reconnect to SignalR for server {ServerId}", vpnServerId);
            throw;
        }
    }
}