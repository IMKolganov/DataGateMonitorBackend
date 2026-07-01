namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy.Hubs;

internal static class OpenVpnHubConnectionDefaults
{
    /// <summary>
    /// Shared automatic-reconnect schedule for all OpenVPN SignalR clients.
    /// Microsoft default is 4 attempts (0, 2, 10, 30 s); we use 6 for longer resilience.
    /// </summary>
    public static readonly TimeSpan[] AutomaticReconnectDelays =
    [
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromSeconds(60),
    ];

    public static readonly TimeSpan ServerTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(15);
    public static readonly TimeSpan StartFailureRetryDelay = TimeSpan.FromSeconds(5);
}
