using Microsoft.AspNetCore.SignalR.Client;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

public interface IHubConnectionProxy : IAsyncDisposable
{
    HubConnectionState State { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task InvokeAsync(string methodName, CancellationToken cancellationToken = default, params object?[] args);

    void On<T>(string methodName, Func<T, Task> handler);
    void On<T1, T2>(string methodName, Action<T1, T2> handler);
}