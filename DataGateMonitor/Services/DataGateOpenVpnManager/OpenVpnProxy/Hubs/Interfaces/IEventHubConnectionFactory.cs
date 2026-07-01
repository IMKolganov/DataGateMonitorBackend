namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

public interface IEventHubConnectionFactory
{
    IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider);
}
