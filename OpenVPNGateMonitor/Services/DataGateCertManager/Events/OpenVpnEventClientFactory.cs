using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.Events;

public class OpenVpnEventClientFactory(IServiceProvider rootProvider) : IOpenVpnEventClientFactory
{
    private readonly ConcurrentDictionary<int, OpenVpnEventClient> _clientCache = new();

    public OpenVpnEventClient Create(OpenVpnServer server)
    {
        return _clientCache.GetOrAdd(server.Id, _ =>
        {
            // Resolve only non-scoped services from the root container
            var logger       = rootProvider.GetRequiredService<ILogger<OpenVpnEventClient>>();
            var eventHub     = rootProvider.GetRequiredService<IHubContext<OpenVpnEventHub>>();
            // should be Singleton/Transient
            var tokenService = rootProvider.GetRequiredService<IMicroserviceTokenService>();
            // for resolving scoped services inside the client
            var scopeFactory = rootProvider.GetRequiredService<IServiceScopeFactory>();

            return new OpenVpnEventClient(server, logger, eventHub, tokenService, scopeFactory);
        });
    }

    public async Task<OpenVpnEventClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken)
    {
        if (_clientCache.TryGetValue(serverId, out var cached))
            return cached;

        // Scoped query is OK here because we don't keep the scope
        using var scope = rootProvider.CreateScope();
        var serverQuery = scope.ServiceProvider.GetRequiredService<IOpenVpnServerQueryService>();
        var server = await serverQuery.GetByIdAsync(serverId, cancellationToken);
        if (server is null) return null;

        return Create(server);
    }

    public bool Remove(int serverId) => _clientCache.TryRemove(serverId, out _);
}