using DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs.Interfaces;
using Microsoft.AspNetCore.SignalR.Client;

namespace DataGateMonitor.Tests.Helpers;

internal class FakeHubConnectionProxy : IHubConnectionProxy
{
    private readonly Dictionary<string, Delegate> _handlers = new(StringComparer.Ordinal);
    private HubConnectionState _state = HubConnectionState.Disconnected;
    private bool _simulateDisconnectOnNextStateRead;

    public int StartCallCount { get; private set; }
    public int StopCallCount { get; private set; }
    public bool Disposed { get; private set; }

    public string? ConnectionId { get; set; } = "fake-conn";

    public Func<CancellationToken, Task>? StartAsyncOverride { get; set; }

    public Func<string, object?, CancellationToken, Task>? InvokeOneArgHandler { get; set; }

    public Func<string, object?, object?, CancellationToken, Task>? InvokeTwoArgHandler { get; set; }

    public virtual HubConnectionState State
    {
        get
        {
            if (_simulateDisconnectOnNextStateRead)
            {
                _simulateDisconnectOnNextStateRead = false;
                return HubConnectionState.Disconnected;
            }

            return _state;
        }
        set => _state = value;
    }

    public void SimulateDisconnectOnNextStateRead() => _simulateDisconnectOnNextStateRead = true;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        StartCallCount++;
        if (StartAsyncOverride is not null)
            return StartAsyncOverride(cancellationToken);

        _state = HubConnectionState.Connected;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        StopCallCount++;
        _state = HubConnectionState.Disconnected;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        _state = HubConnectionState.Disconnected;
        return ValueTask.CompletedTask;
    }

    public virtual Task InvokeAsync(string methodName, object? arg1, CancellationToken cancellationToken = default)
    {
        if (InvokeOneArgHandler is not null)
            return InvokeOneArgHandler(methodName, arg1, cancellationToken);

        return Task.CompletedTask;
    }

    public virtual Task InvokeAsync(string methodName, object? arg1, object? arg2, CancellationToken cancellationToken = default)
    {
        if (InvokeTwoArgHandler is not null)
            return InvokeTwoArgHandler(methodName, arg1, arg2, cancellationToken);

        return Task.CompletedTask;
    }

    public void On<T>(string methodName, Func<T, Task> handler) => _handlers[methodName] = handler;

    public void On<T1, T2>(string methodName, Action<T1, T2> handler) => _handlers[methodName] = handler;

    public void Raise<T1, T2>(string methodName, T1 arg1, T2 arg2)
    {
        if (_handlers[methodName] is Action<T1, T2> handler)
            handler(arg1, arg2);
    }

    public Task RaiseAsync<T>(string methodName, T arg)
    {
        if (_handlers[methodName] is Func<T, Task> handler)
            return handler(arg);

        return Task.CompletedTask;
    }

    public void OnReconnecting(Func<Exception?, Task> handler) => _handlers["__Reconnecting"] = handler;

    public void OnReconnected(Func<string?, Task> handler) => _handlers["__Reconnected"] = handler;

    public void OnClosed(Func<Exception?, Task> handler) => _handlers["__Closed"] = handler;

    public Task RaiseReconnectingAsync(Exception? ex = null)
    {
        if (_handlers["__Reconnecting"] is Func<Exception?, Task> handler)
            return handler(ex);
        return Task.CompletedTask;
    }

    public Task RaiseClosedAsync(Exception? ex = null)
    {
        _state = HubConnectionState.Disconnected;
        if (_handlers.TryGetValue("__Closed", out var handler) && handler is Func<Exception?, Task> closedHandler)
            return closedHandler(ex);
        return Task.CompletedTask;
    }

    public async Task SimulateReconnectingThenConnectedAsync(TimeSpan reconnectDuration)
    {
        _state = HubConnectionState.Reconnecting;
        await Task.Delay(reconnectDuration);
        _state = HubConnectionState.Connected;
    }
}

internal sealed class SingleProxyHubConnectionFactory(FakeHubConnectionProxy proxy) : IHubConnectionFactory
{
    public IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider) => proxy;
}

internal sealed class SingleProxyEventHubConnectionFactory(FakeHubConnectionProxy proxy) : IEventHubConnectionFactory
{
    public IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider) => proxy;
}
