namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnMicroserviceClient : IDisposable, IAsyncDisposable
{
    /// <summary>ApiUrl snapshot captured when the client instance was created (used for cache invalidation).</summary>
    string RegisteredApiUrl { get; }
    /// <summary>Current ApiUrl from the bound <see cref="Models.VpnServer"/> (may change if the entity is mutated).</summary>
    string CurrentApiUrl { get; }
    Task<string> SendCommandWithResponseAsync(string command, CancellationToken cancellationToken);
    Task SendCommandAsync(string command, CancellationToken cancellationToken);
    Task SendCommandToMicroserviceAsync(string command, CancellationToken cancellationToken);
}