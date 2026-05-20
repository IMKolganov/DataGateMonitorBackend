namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

public interface IOpenVpnProxyTrafficFlowClient
{
    Task StartListeningAsync(CancellationToken cancellationToken);
    Task StopAsync();
}
