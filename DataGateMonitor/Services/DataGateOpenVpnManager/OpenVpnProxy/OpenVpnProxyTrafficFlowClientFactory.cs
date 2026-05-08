using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public sealed class OpenVpnProxyTrafficFlowClientFactory(IServiceProvider rootProvider) : IOpenVpnProxyTrafficFlowClientFactory
{
    private readonly ConcurrentDictionary<int, IOpenVpnProxyTrafficFlowClient> _clientCache = new();

    public IOpenVpnProxyTrafficFlowClient Create(VpnServer server)
    {
        if (server.ServerType != VpnServerType.OpenVpn)
        {
            throw new InvalidOperationException(
                $"Proxy traffic flow client is only for OpenVPN servers (server {server.Id} is {server.ServerType}).");
        }

        return _clientCache.GetOrAdd(server.Id, _ =>
        {
            var logger = rootProvider.GetRequiredService<ILogger<OpenVpnProxyTrafficFlowClient>>();
            var trafficHub = rootProvider.GetRequiredService<IHubContext<OpenVpnProxyTrafficFlowHub>>();
            var tokenService = rootProvider.GetRequiredService<IMicroserviceTokenService>();
            return new OpenVpnProxyTrafficFlowClient(server, logger, trafficHub, tokenService);
        });
    }

    public bool Remove(int serverId)
    {
        if (!_clientCache.TryRemove(serverId, out var client))
            return false;
        try
        {
            client.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            var logger = rootProvider.GetRequiredService<ILogger<OpenVpnProxyTrafficFlowClientFactory>>();
            logger.LogWarning(ex, "Error stopping proxy traffic flow client for server {ServerId}", serverId);
        }

        return true;
    }

    public IReadOnlyCollection<IOpenVpnProxyTrafficFlowClient> GetAllClients() => _clientCache.Values.ToArray();
}
