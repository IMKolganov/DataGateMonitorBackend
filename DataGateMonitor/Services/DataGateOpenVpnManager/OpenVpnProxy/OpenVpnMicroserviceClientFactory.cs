using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public class OpenVpnMicroserviceClientFactory(IServiceProvider serviceProvider) : IOpenVpnMicroserviceClientFactory
{
    private readonly ConcurrentDictionary<int, IOpenVpnMicroserviceClient> _clientCache = new();

    public IOpenVpnMicroserviceClient Create(VpnServer server)
    {
        if (server.ServerType != VpnServerType.OpenVpn)
        {
            throw new InvalidOperationException(
                $"OpenVPN microservice client is only for OpenVPN servers (server {server.Id} is {server.ServerType}).");
        }

        var normalizedUrl = VpnServerApiUrlNormalizer.Normalize(server.ApiUrl);

        return _clientCache.AddOrUpdate(
            server.Id,
            _ => CreateNew(server),
            (_, existing) =>
            {
                if (!VpnServerApiUrlNormalizer.Equals(existing.RegisteredApiUrl, normalizedUrl))
                {
                    DisposeClient(existing);
                    return CreateNew(server);
                }
                return existing;
            });
    }

    public async Task<IOpenVpnMicroserviceClient?> TryCreateByServerIdAsync(
        int serverId,
        CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var openVpnOverviewQuery = scope.ServiceProvider.GetRequiredService<IVpnServerQueryService>();
        var server = await openVpnOverviewQuery.GetById(serverId, cancellationToken);
        if (server is null)
            return null;

        if (server.ServerType != VpnServerType.OpenVpn)
            return null;

        return Create(server);
    }

    public void Invalidate(int serverId)
    {
        if (_clientCache.TryRemove(serverId, out var client))
            DisposeClient(client);
    }

    private IOpenVpnMicroserviceClient CreateNew(VpnServer server)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OpenVpnMicroserviceClient>>();
        var frontendHub = scope.ServiceProvider.GetRequiredService<IHubContext<OpenVpnFrontendHub>>();
        var tokenService = scope.ServiceProvider.GetRequiredService<IMicroserviceTokenService>();
        var scopeFactory = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();
        var hubConnectionFactory = scope.ServiceProvider.GetService<IHubConnectionFactory>();
        return new OpenVpnMicroserviceClient(server, logger, frontendHub, tokenService, scopeFactory, hubConnectionFactory);
    }

    private static void DisposeClient(IOpenVpnMicroserviceClient client)
    {
        // try async dispose first; fall back to sync
        (client as IAsyncDisposable)?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        (client as IDisposable)?.Dispose();
    }
}