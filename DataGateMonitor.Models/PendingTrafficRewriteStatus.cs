namespace DataGateMonitor.Models;

/// <summary>Lifecycle of an async VpnServerClientTraffics ExternalId rewrite after user merge.</summary>
public enum PendingTrafficRewriteStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}
