namespace DataGateMonitor.SharedModels.DataGateMonitor.VpnServers.Responses;

public enum VpnServerPostSetupState
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3
}
