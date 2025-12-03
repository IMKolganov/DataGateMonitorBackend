using Microsoft.AspNetCore.SignalR.Client;

namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

// Thin abstraction to make HubConnection unit-testable
public interface IHubConnectionProxy : IAsyncDisposable
{
    HubConnectionState State { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task InvokeAsync(string methodName, CancellationToken cancellationToken = default, params object?[] args);

    void On<T>(string methodName, Func<T, Task> handler);
    void On<T1, T2>(string methodName, Action<T1, T2> handler);
}

public interface IHubConnectionFactory
{
    IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider);
}

internal sealed class HubConnectionProxy : IHubConnectionProxy
{
    private readonly HubConnection _inner;
    public HubConnectionProxy(HubConnection inner) => _inner = inner;

    public HubConnectionState State => _inner.State;
    public Task StartAsync(CancellationToken cancellationToken = default) => _inner.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken = default) => _inner.StopAsync(cancellationToken);
    public async ValueTask DisposeAsync() => await _inner.DisposeAsync();

    public Task InvokeAsync(string methodName, CancellationToken cancellationToken = default, params object?[] args)
        => _inner.InvokeAsync(methodName, args, cancellationToken);

    public void On<T>(string methodName, Func<T, Task> handler) => _inner.On(methodName, handler);
    public void On<T1, T2>(string methodName, Action<T1, T2> handler) => _inner.On(methodName, handler);
}

internal sealed class DefaultHubConnectionFactory : IHubConnectionFactory
{
    public IHubConnectionProxy Create(string fullUrl, Func<Task<string?>> accessTokenProvider)
    {
        var connection = new HubConnectionBuilder()
            .WithUrl(fullUrl, options => { options.AccessTokenProvider = accessTokenProvider; })
            .WithAutomaticReconnect()
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
            })
            .Build();
        return new HubConnectionProxy(connection);
    }
}
