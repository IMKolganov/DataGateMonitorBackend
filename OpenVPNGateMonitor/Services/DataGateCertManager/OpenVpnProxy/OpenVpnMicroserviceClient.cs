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
    IVpnDataService vpnDataService)
{
    private readonly Dictionary<int, HubConnection> _connections = new();
    private readonly HashSet<int> _subscribed = new();

    public async Task SendCommandToMicroserviceAsync(int vpnServerId, string command)
    {
        try
        {
            var connection = await EnsureConnectionAsync(vpnServerId);

            if (connection.State != HubConnectionState.Connected)
            {
                await connection.StartAsync();
                logger.LogInformation("Started SignalR connection for server {ServerId}", vpnServerId);
            }

            await connection.InvokeAsync("SendCommand", command);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send command to microservice for server {ServerId}", vpnServerId);
            var errorMessage = $"[Error] Failed to send command to server {vpnServerId}: {ex.Message}";
            await frontendHub.Clients.Group(vpnServerId.ToString()).SendAsync("ReceiveCommandResult", errorMessage);
        }
    }

    private async Task<HubConnection> EnsureConnectionAsync(int vpnServerId)
    {
        if (_connections.TryGetValue(vpnServerId, out var existingConnection))
        {
            if (existingConnection.State == HubConnectionState.Disconnected)
            {
                logger.LogInformation("Reconnecting to microservice for server {ServerId}", vpnServerId);
                await existingConnection.StartAsync();
            }

            return existingConnection;
        }

        var server = await vpnDataService.GetOpenVpnServer(vpnServerId, CancellationToken.None);

        if (server is null || string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException($"OpenVPN server {vpnServerId} not found or has no microservice URL");

        var token = tokenService.GenerateToken("vpn-cert-issuer", "cert-create",
            "backend", "DataGateCertManager");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Generated token is null or empty");

        var fullUrl = $"{server.ApiUrl.TrimEnd('/')}/hubs/openvpn";

        var connection = new HubConnectionBuilder()
            .WithUrl(fullUrl, options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        if (!_subscribed.Contains(vpnServerId))
        {
            connection.On<string>("ReceiveMessage", async message =>
            {
                logger.LogDebug("Forwarding ReceiveMessage to frontend for server {ServerId}", vpnServerId);
                await frontendHub.Clients.Group(vpnServerId.ToString()).SendAsync("ReceiveMessage", message);
            });

            connection.On<string>("ReceiveCommandResult", async result =>
            {
                logger.LogDebug("Forwarding ReceiveCommandResult to frontend for server {ServerId}", vpnServerId);
                await frontendHub.Clients.Group(vpnServerId.ToString()).SendAsync("ReceiveCommandResult", result);
            });

            _subscribed.Add(vpnServerId);
        }

        _connections[vpnServerId] = connection;

        return connection;
    }
}
