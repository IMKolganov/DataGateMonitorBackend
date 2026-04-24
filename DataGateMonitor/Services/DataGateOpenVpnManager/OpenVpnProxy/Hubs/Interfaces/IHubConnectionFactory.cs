namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

public interface IHubConnectionFactory
{
    IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider);
}
