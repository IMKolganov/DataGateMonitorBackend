using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.DataBase.Services.Query.VpnServerTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnServerEvent.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.Events;

public class OpenVpnEventClientFactory(IServiceProvider rootProvider) : IOpenVpnEventClientFactory
{
    private readonly ConcurrentDictionary<int, OpenVpnEventClient> _clientCache = new();

    public OpenVpnEventClient Create(VpnServer server)
    {
        if (server.ServerType != VpnServerType.OpenVpn)
        {
            throw new InvalidOperationException(
                $"OpenVPN event client is only for OpenVPN servers (server {server.Id} is {server.ServerType}).");
        }

        var normalizedUrl = VpnServerApiUrlNormalizer.Normalize(server.ApiUrl);

        return _clientCache.AddOrUpdate(
            server.Id,
            _ => CreateNew(server),
            (_, existing) =>
            {
                if (!VpnServerApiUrlNormalizer.Equals(existing.RegisteredApiUrl, normalizedUrl))
                {
                    StopClient(existing, server.Id);
                    return CreateNew(server);
                }

                return existing;
            });
    }

    public async Task<OpenVpnEventClient?> TryCreateByServerIdAsync(int serverId, CancellationToken cancellationToken)
    {
        if (_clientCache.TryGetValue(serverId, out var cached))
            return cached;

        using var scope = rootProvider.CreateScope();
        var serverQuery = scope.ServiceProvider.GetRequiredService<IVpnServerQueryService>();
        var server = await serverQuery.GetById(serverId, cancellationToken);
        if (server is null)
            return null;

        if (server.ServerType != VpnServerType.OpenVpn)
            return null;

        return Create(server);
    }

    public bool Remove(int serverId)
    {
        if (!_clientCache.TryRemove(serverId, out var client))
            return false;

        StopClient(client, serverId);
        return true;
    }

    public IReadOnlyCollection<OpenVpnEventClient> GetAllClients()
        => _clientCache.Values.ToArray();

    public ConnectionStatusesResponse GetAllClientStatuses()
    {
        var items = _clientCache.Values
            .Select(c => c.GetStatus().ConnectionStatus)
            .ToList();

        return new ConnectionStatusesResponse { ConnectionStatuses = items };
    }

    public bool TryGetClientStatus(int serverId, out ConnectionStatusResponse? status)
    {
        if (_clientCache.TryGetValue(serverId, out var client))
        {
            status = client.GetStatus();
            return true;
        }

        status = null;
        return false;
    }

    private OpenVpnEventClient CreateNew(VpnServer server)
    {
        var logger = rootProvider.GetRequiredService<ILogger<OpenVpnEventClient>>();
        var eventHub = rootProvider.GetRequiredService<IHubContext<OpenVpnEventHub>>();
        var tokenService = rootProvider.GetRequiredService<IMicroserviceTokenService>();
        var scopeFactory = rootProvider.GetRequiredService<IServiceScopeFactory>();
        var eventHubConnectionFactory = rootProvider.GetService<IEventHubConnectionFactory>();
        return new OpenVpnEventClient(server, logger, eventHub, tokenService, scopeFactory, eventHubConnectionFactory);
    }

    private void StopClient(OpenVpnEventClient client, int serverId)
    {
        try
        {
            client.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            var logger = rootProvider.GetRequiredService<ILogger<OpenVpnEventClientFactory>>();
            logger.LogWarning(ex, "Error stopping event client for server {ServerId}", serverId);
        }
    }
}
