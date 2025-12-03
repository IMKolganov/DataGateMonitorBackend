namespace OpenVPNGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnMicroserviceClient : IDisposable, IAsyncDisposable
{
    string CurrentApiUrl { get; }
    Task<string> SendCommandWithResponseAsync(string command, CancellationToken cancellationToken);
    Task SendCommandAsync(string command, CancellationToken cancellationToken);
    Task SendCommandToMicroserviceAsync(string command, CancellationToken cancellationToken);
}