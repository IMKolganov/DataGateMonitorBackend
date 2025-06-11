using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class OpenVpnEventClientFactory(IServiceProvider serviceProvider) : IOpenVpnEventClientFactory
{
    private readonly ConcurrentDictionary<int, OpenVpnEventClient> _clientCache = new();

    public OpenVpnEventClient Create(OpenVpnServer server)
    {
        return _clientCache.GetOrAdd(server.Id, _ =>
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<OpenVpnEventClient>>();
            var eventHub = scope.ServiceProvider.GetRequiredService<IHubContext<OpenVpnEventHub>>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IMicroserviceTokenService>();
            return new OpenVpnEventClient(server, logger, eventHub, tokenService, serviceProvider);
        });
    }

    public async Task<OpenVpnEventClient?> TryCreateByServerIdAsync(int serverId, CancellationToken ct)
    {
        if (_clientCache.TryGetValue(serverId, out var cached))
            return cached;

        using var scope = serviceProvider.CreateScope();
        var vpnDataService = scope.ServiceProvider.GetRequiredService<IVpnDataService>();
        var server = await vpnDataService.GetOpenVpnServer(serverId, ct);
        if (server is null) throw new Exception($"OpenVPN server not found with id {serverId}");

        return Create(server);
    }
}