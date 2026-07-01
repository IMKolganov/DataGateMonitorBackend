using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public sealed class OpenVpnProxyTrafficFlowClientFactory(IServiceProvider rootProvider) : IOpenVpnProxyTrafficFlowClientFactory
{
    private readonly ConcurrentDictionary<int, IOpenVpnProxyTrafficFlowClient> _clientCache = new();

    private ILogger<OpenVpnProxyTrafficFlowClientFactory> Logger =>
        rootProvider.GetRequiredService<ILogger<OpenVpnProxyTrafficFlowClientFactory>>();

    public IOpenVpnProxyTrafficFlowClient Create(VpnServer server)
    {
        if (server.ServerType != VpnServerType.OpenVpn)
        {
            throw new InvalidOperationException(
                $"Proxy traffic flow client is only for OpenVPN servers (server {server.Id} is {server.ServerType}).");
        }

        var normalizedUrl = VpnServerApiUrlNormalizer.Normalize(server.ApiUrl);

        return _clientCache.AddOrUpdate(
            server.Id,
            _ =>
            {
                Logger.LogDebug("Creating new traffic flow client for server {ServerId}, ApiUrl={ApiUrl}", server.Id, server.ApiUrl);
                return CreateNew(server);
            },
            (_, existing) =>
            {
                if (!VpnServerApiUrlNormalizer.Equals(existing.RegisteredApiUrl, normalizedUrl))
                {
                    Logger.LogDebug(
                        "Recreating traffic flow client for server {ServerId}: RegisteredApiUrl={RegisteredApiUrl} -> ApiUrl={ApiUrl}",
                        server.Id, existing.RegisteredApiUrl, server.ApiUrl);
                    StopClient(existing, server.Id);
                    return CreateNew(server);
                }

                Logger.LogDebug(
                    "Reusing cached traffic flow client for server {ServerId}, RegisteredApiUrl={RegisteredApiUrl}",
                    server.Id, existing.RegisteredApiUrl);
                return existing;
            });
    }

    public bool Remove(int serverId)
    {
        if (!_clientCache.TryRemove(serverId, out var client))
            return false;

        StopClient(client, serverId);
        return true;
    }

    public IReadOnlyCollection<IOpenVpnProxyTrafficFlowClient> GetAllClients() => _clientCache.Values.ToArray();

    private IOpenVpnProxyTrafficFlowClient CreateNew(VpnServer server)
    {
        var logger = rootProvider.GetRequiredService<ILogger<OpenVpnProxyTrafficFlowClient>>();
        var trafficHub = rootProvider.GetRequiredService<IHubContext<OpenVpnProxyTrafficFlowHub>>();
        var tokenService = rootProvider.GetRequiredService<IMicroserviceTokenService>();
        var hubConnectionFactory = rootProvider.GetService<IHubConnectionFactory>();
        return new OpenVpnProxyTrafficFlowClient(server, logger, trafficHub, tokenService, hubConnectionFactory);
    }

    private void StopClient(IOpenVpnProxyTrafficFlowClient client, int serverId)
    {
        try
        {
            client.StopAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            var logger = rootProvider.GetRequiredService<ILogger<OpenVpnProxyTrafficFlowClientFactory>>();
            logger.LogWarning(ex, "Error stopping proxy traffic flow client for server {ServerId}", serverId);
        }
    }
}
