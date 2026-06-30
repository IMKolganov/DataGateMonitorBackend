namespace DataGateMonitor.SharedModels.Enums;

public enum CertExpiryProfileOutcome
{
    Healthy = 0,
    ExpiringSoon = 1,
    Expired = 2,
    MissingOnNode = 3
}
