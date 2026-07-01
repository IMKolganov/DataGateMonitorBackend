using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json.Linq;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Serialization;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public sealed class OpenVpnProxyTrafficFlowClient(
    VpnServer server,
    ILogger<OpenVpnProxyTrafficFlowClient> logger,
    IHubContext<OpenVpnProxyTrafficFlowHub> trafficFlowHub,
    IMicroserviceTokenService tokenService,
    IHubConnectionFactory? hubConnectionFactory = null) : IOpenVpnProxyTrafficFlowClient
{
    private IHubConnectionProxy? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _handlersRegistered;
    private readonly IHubConnectionFactory _hubFactory = hubConnectionFactory ?? new DefaultHubConnectionFactory();

    public string RegisteredApiUrl { get; } = server.ApiUrl;

    public async Task StartListeningAsync(CancellationToken cancellationToken)
    {
        await EnsureConnectionAsync(cancellationToken);
    }

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
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private async Task<IHubConnectionProxy> EnsureConnectionAsync(CancellationToken cancellationToken)
    {
        await _connectionLock.WaitAsync(cancellationToken);
        try
        {
            if (_connection is not null
                && !VpnServerApiUrlNormalizer.Equals(server.ApiUrl, RegisteredApiUrl))
            {
                logger.LogWarning(
                    "Detected API URL change for server {ServerId}. Recreating proxy traffic flow connection...",
                    server.Id);
                logger.LogDebug(
                    "Traffic flow hub URL change for server {ServerId}: RegisteredApiUrl={RegisteredApiUrl}, CurrentApiUrl={CurrentApiUrl}",
                    server.Id, RegisteredApiUrl, server.ApiUrl);
                try { await _connection.StopAsync(CancellationToken.None); } catch { /* ignore */ }
                try { await _connection.DisposeAsync(); } catch { /* ignore */ }
                _connection = null;
                _handlersRegistered = false;
            }

            if (_connection is not null && _connection.State == HubConnectionState.Connected)
            {
                logger.LogDebug(
                    "Traffic flow hub already Connected for server {ServerId}, ConnId={ConnId}",
                    server.Id, _connection.ConnectionId);
                return _connection;
            }

            if (_connection is null)
            {
                var fullUrl = $"{server.ApiUrl.TrimEnd('/')}/hubs/proxy-traffic-flow";
                logger.LogInformation("Creating proxy traffic flow connection for server {ServerId} at {Url}", server.Id, fullUrl);
                _connection = _hubFactory.Create(
                    fullUrl,
                    () => Task.FromResult<string?>(tokenService.GenerateToken(
                        "vpn-cert-issuer", "cert-create", "backend", "DataGateOpenVpnManager")));

                if (!_handlersRegistered)
                {
                    _connection.On<JToken>("TrafficFlowUpdated", async payload =>
                    {
                        var relay = ProjectJson.Deserialize<object>(payload.ToString());
                        if (relay is null)
                            return;

                        await trafficFlowHub.Clients.Group(server.Id.ToString())
                            .SendAsync("TrafficFlowUpdated", relay);
                    });

                    _handlersRegistered = true;
                }
            }

            if (_connection.State != HubConnectionState.Connected)
            {
                logger.LogDebug(
                    "Traffic flow hub StartWhenReady: ServerId={ServerId}, CurrentState={State}",
                    server.Id, _connection.State);
                await HubConnectionStartup.StartWhenReadyAsync(
                    () => _connection.State,
                    ct => _connection.StartAsync(ct),
                    cancellationToken,
                    logger: logger);
                logger.LogInformation("Started proxy traffic flow listener for server {ServerId}", server.Id);
            }

            return _connection;
        }
        finally
        {
            _connectionLock.Release();
        }
    }
}
