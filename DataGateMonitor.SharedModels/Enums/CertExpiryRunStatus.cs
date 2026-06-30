namespace DataGateMonitor.SharedModels.Enums;

public enum CertExpiryRunStatus
{
    Running = 0,
    Completed = 1,
    Failed = 2,
    SkippedAlreadyRunning = 3
}
