using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientFactory(
    ILoggerFactory loggerFactory,
    IHubContext<OpenVpnFrontendHub> frontendHub,
    IMicroserviceTokenService tokenService,
    IVpnDataService vpnDataService) : IOpenVpnMicroserviceClientFactory
{
    private readonly ConcurrentDictionary<int, OpenVpnMicroserviceClient> _clientCache = new();

    public OpenVpnMicroserviceClient Create(OpenVpnServer server)
    {
        return _clientCache.GetOrAdd(server.Id, _ =>
            new OpenVpnMicroserviceClient(
                server,
                loggerFactory.CreateLogger<OpenVpnMicroserviceClient>(),
                frontendHub,
                tokenService));
    }

    public async Task<OpenVpnMicroserviceClient?> TryCreateByServerIdAsync(int serverId, CancellationToken ct)
    {
        if (_clientCache.TryGetValue(serverId, out var cached))
            return cached;

        var server = await vpnDataService.GetOpenVpnServer(serverId, ct);
        if (server is null) throw new Exception($"OpenVPN server not found with id {serverId}");

        return Create(server);
    }
}