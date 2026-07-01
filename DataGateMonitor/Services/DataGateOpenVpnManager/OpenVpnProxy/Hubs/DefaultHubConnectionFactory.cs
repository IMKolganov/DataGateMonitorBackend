using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

internal sealed class DefaultHubConnectionFactory : IHubConnectionFactory
{
    public IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider)
    {
        var connection = OpenVpnHubConnectionBuilder.Build(fullUrl, accessTokenProvider, suppressSignalRInfoLogs: true);
        return new HubConnectionProxy(connection);
    }
}
