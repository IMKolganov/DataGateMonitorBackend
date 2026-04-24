using Microsoft.AspNetCore.SignalR.Client;

namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

public interface IHubConnectionProxy : IAsyncDisposable
{
    HubConnectionState State { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);

    Task InvokeAsync(
        string methodName,
        object? arg1,
        CancellationToken cancellationToken = default);

    Task InvokeAsync(
        string methodName,
        object? arg1,
        object? arg2,
        CancellationToken cancellationToken = default);

    void On<T>(string methodName, Func<T, Task> handler);
    void On<T1, T2>(string methodName, Action<T1, T2> handler);
}