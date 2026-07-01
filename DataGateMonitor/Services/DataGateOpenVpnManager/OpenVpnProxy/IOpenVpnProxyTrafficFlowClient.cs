namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnProxyTrafficFlowClient
{
    string RegisteredApiUrl { get; }
    Task StartListeningAsync(CancellationToken cancellationToken);
    Task StopAsync();
}
