using Microsoft.AspNetCore.SignalR.Client;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

internal sealed class HubConnectionProxy(HubConnection inner) : IHubConnectionProxy
{
    public HubConnectionState State => inner.State;

    public Task StartAsync(CancellationToken cancellationToken = default)
        => inner.StartAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken = default)
        => inner.StopAsync(cancellationToken);

    public async ValueTask DisposeAsync() => await inner.DisposeAsync();

    public Task InvokeAsync(
        string methodName,
        object? arg1,
        object? arg2,
        CancellationToken cancellationToken = default)
        => inner.InvokeAsync(methodName, arg1, arg2, cancellationToken);

    public void On<T>(string methodName, Func<T, Task> handler)
        => inner.On(methodName, handler);

    public void On<T1, T2>(string methodName, Action<T1, T2> handler)
    {
        inner.On(methodName, new[] { typeof(T1), typeof(T2) }, args =>
        {
            handler((T1)args[0]!, (T2)args[1]!);
            return Task.CompletedTask;
        });
    }
}