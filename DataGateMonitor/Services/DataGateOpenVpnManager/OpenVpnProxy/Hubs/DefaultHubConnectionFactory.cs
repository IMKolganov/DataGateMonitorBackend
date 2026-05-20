using Microsoft.AspNetCore.SignalR.Client;
using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

internal sealed class DefaultHubConnectionFactory : IHubConnectionFactory
{
    public IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(fullUrl, options => { options.AccessTokenProvider = accessTokenProvider; })
            .WithAutomaticReconnect()
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .Build();
        return new HubConnectionProxy(connection);
    }
}