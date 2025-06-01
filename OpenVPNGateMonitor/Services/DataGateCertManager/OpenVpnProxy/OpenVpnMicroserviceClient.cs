using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;

public class OpenVpnMicroserviceClient(
    OpenVpnServer server,
    ILogger<OpenVpnMicroserviceClient> logger,
    IHubContext<OpenVpnFrontendHub> frontendHub,
    IMicroserviceTokenService tokenService)
{
    private readonly OpenVpnServer _server = server;
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingCommands = new();
    private HubConnection? _connection;

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
            var connection = await EnsureConnectionAsync(cancellationToken);

            if (connection.State != HubConnectionState.Connected)
            {
                logger.LogWarning("SignalR connection is {State}, trying to reconnect...", connection.State);
                await ReconnectAsync(connection);
            }

            await connection.InvokeAsync("SendCommand", command, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command to microservice for server {ServerId}", _server.Id);
            var errorMessage = $"[Error] Failed to send command to server {_server.Id}: {ex.Message}";
            await frontendHub.Clients.Group(_server.Id.ToString())
                .SendAsync("ReceiveCommandResult", errorMessage, cancellationToken);
        }
    }
    
    public async Task SendCommandToMicroserviceAsync(string command, CancellationToken cancellationToken)
    {
        try
        {
            var connection = await EnsureConnectionAsync(cancellationToken);

            if (connection.State != HubConnectionState.Connected)
            {
                logger.LogWarning("SignalR connection is {State}, trying to reconnect...", connection.State);
                await ReconnectAsync(connection);
            }

            await connection.InvokeAsync("SendCommand", command, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command to microservice for server {ServerId}", _server.Id);
            var errorMessage = $"[Error] Failed to send command to server {_server.Id}: {ex.Message}";
            await frontendHub.Clients.Group(_server.Id.ToString())
                .SendAsync("ReceiveCommandResult", errorMessage, cancellationToken);
        }
    }

    private async Task<HubConnection> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
            return _connection;

        logger.LogInformation("Creating SignalR connection for server {ServerId}", _server.Id);

        if (string.IsNullOrWhiteSpace(_server.ApiUrl))
            throw new InvalidOperationException($"OpenVPN server {_server.Id} has no microservice URL");

        var token = tokenService.GenerateToken(
            "vpn-cert-issuer", "cert-create", "backend", "DataGateCertManager");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Generated token is null or empty");

        var fullUrl = $"{_server.ApiUrl.TrimEnd('/')}/hubs/openvpn";

        _connection = new HubConnectionBuilder()
            .WithUrl(fullUrl, options => options.AccessTokenProvider = () => Task.FromResult<string?>(token))
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string>("ReceiveCommandResult", async result =>
        {
            await frontendHub.Clients.Group(_server.Id.ToString())
                .SendAsync("ReceiveCommandResult", result);
        });

        _connection.On<string>("ReceiveMessage", async message =>
        {
            await frontendHub.Clients.Group(_server.Id.ToString())
                .SendAsync("ReceiveMessage", message);
        });

        _connection.On<string, string>("ReceiveCommandResultWithRequestId", (requestId, result) =>
        {
            if (_pendingCommands.TryRemove(requestId, out var tcs))
            {
                tcs.TrySetResult(result);
            }
        });

        await _connection.StartAsync(cancellationToken);
        logger.LogInformation("Started SignalR connection for server {ServerId}", _server.Id);

        return _connection;
    }

    private async Task ReconnectAsync(HubConnection connection)
    {
        try
        {
            await connection.StopAsync();
            await connection.StartAsync();
            logger.LogInformation("Reconnected to SignalR for server {ServerId}", _server.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reconnect to SignalR for server {ServerId}", _server.Id);
            throw;
        }
    }
}
