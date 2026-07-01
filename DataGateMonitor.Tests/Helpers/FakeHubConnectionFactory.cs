using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

namespace DataGateMonitor.Tests.Helpers;

internal sealed class FakeHubConnectionFactory : IHubConnectionFactory, IEventHubConnectionFactory
{
    public FakeHubConnectionProxy? LastCreated { get; private set; }
    public List<FakeHubConnectionProxy> Created { get; } = [];

    public IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider)
    {
        var proxy = new FakeHubConnectionProxy();
        LastCreated = proxy;
        Created.Add(proxy);
        return proxy;
    }
}
