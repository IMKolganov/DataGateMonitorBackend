using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

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
            var openVpnFileQueryService = scope.ServiceProvider.GetRequiredService<IIssuedOvpnFileQueryService>();
            
            return new OpenVpnEventClient(server, logger, eventHub, tokenService, openVpnFileQueryService, 
                serviceProvider);
        });
    }

    public async Task<OpenVpnEventClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken)
    {
        if (_clientCache.TryGetValue(serverId, out var cached))
            return cached;

        using var scope = serviceProvider.CreateScope();
        var openVpnOverviewQuery = scope.ServiceProvider.GetRequiredService<IOpenVpnServerQueryService>();
        var server = await openVpnOverviewQuery.GetByIdAsync(serverId, cancellationToken);
        if (server is null) throw new Exception($"OpenVPN server not found with id {serverId}");

        return Create(server);
    }
}