using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;

public class OpenVpnMicroserviceClient
{
    private readonly ILogger<OpenVpnMicroserviceClient> _logger;
    private readonly IHubContext<OpenVpnFrontendHub> _frontendHub;
    private readonly IMicroserviceTokenService _tokenService;
    private readonly IVpnDataService _vpnDataService;

    private readonly Dictionary<int, HubConnection> _connections = new();

    public OpenVpnMicroserviceClient(
        ILogger<OpenVpnMicroserviceClient> logger,
        IHubContext<OpenVpnFrontendHub> frontendHub,
        IMicroserviceTokenService tokenService,
        IVpnDataService vpnDataService)
    {
        _logger = logger;
        _frontendHub = frontendHub;
        _tokenService = tokenService;
        _vpnDataService = vpnDataService;
    }

    public async Task SendCommandToMicroserviceAsync(int vpnServerId, string command)
    {
        if (!_connections.TryGetValue(vpnServerId, out var connection))
        {
            connection = await CreateConnectionForServerAsync(vpnServerId);
            _connections[vpnServerId] = connection;
        }

        if (connection.State == HubConnectionState.Disconnected)
        {
            await connection.StartAsync();
            _logger.LogInformation("Connected to microservice for server {ServerId}", vpnServerId);
        }

        if (connection.State == HubConnectionState.Connected)
        {
            await connection.InvokeAsync("SendCommand", command);
        }
        else
        {
            _logger.LogWarning("Cannot send command to server {ServerId} — SignalR not connected", vpnServerId);
        }
    }

    private async Task<HubConnection> CreateConnectionForServerAsync(int vpnServerId)
    {
        var server = await _vpnDataService.GetOpenVpnServer(vpnServerId, CancellationToken.None);

        if (server is null || string.IsNullOrWhiteSpace(server.ApiUrl))
            throw new InvalidOperationException($"OpenVPN server {vpnServerId} not found or has no microservice URL");

        var audience = "OpenVpnMicroservice";
        var token = _tokenService.GenerateToken(
            subject: "OpenVpnBackend",
            purpose: "proxy",
            role: "backend",
            audience: audience
        );

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Generated token is null or empty");

        var connection = new HubConnectionBuilder()
            .WithUrl($"{server.ApiUrl}/hub/openvpn", options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<string>("ReceiveMessage", async message =>
        {
            await _frontendHub.Clients.Group(vpnServerId.ToString()).SendAsync("ReceiveMessage", message);
        });

        connection.On<string>("ReceiveCommandResult", async result =>
        {
            await _frontendHub.Clients.Group(vpnServerId.ToString()).SendAsync("ReceiveCommandResult", result);
        });

        return connection;
    }
}
