using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerTable;
using OpenVPNGateMonitor.Hubs;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Api.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateCertManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientFactory(IServiceProvider serviceProvider) : IOpenVpnMicroserviceClientFactory
{
    private readonly ConcurrentDictionary<int, OpenVpnMicroserviceClient> _clientCache = new();

    public OpenVpnMicroserviceClient Create(OpenVpnServer server)
    {
        return _clientCache.GetOrAdd(server.Id, _ =>
        {
            using var scope = serviceProvider.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<OpenVpnMicroserviceClient>>();
            var frontendHub = scope.ServiceProvider.GetRequiredService<IHubContext<OpenVpnFrontendHub>>();
            var tokenService = scope.ServiceProvider.GetRequiredService<IMicroserviceTokenService>();
            return new OpenVpnMicroserviceClient(server, logger, frontendHub, tokenService);
        });
    }

    public async Task<OpenVpnMicroserviceClient?> TryCreateByServerIdAsync(int serverId,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var openVpnOverviewQuery = scope.ServiceProvider.GetRequiredService<IOpenVpnServerQueryService>();
        var server = await openVpnOverviewQuery.GetByIdAsync(serverId, cancellationToken);
        if (server is null) throw new Exception($"OpenVPN server not found with id {serverId}");

        if (_clientCache.TryGetValue(serverId, out var cached))
        {
            if (!string.Equals(cached.CurrentApiUrl, server.ApiUrl, StringComparison.OrdinalIgnoreCase))
            {
                _clientCache.TryRemove(serverId, out _);
            }
            else
            {
                return cached;
            }
        }

        return Create(server);
    }
}