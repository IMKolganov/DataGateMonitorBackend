namespace DataGateMonitor.Services.LocalIssuance;

public enum LocalIssuanceEventOutcome
{
    Created,
    Duplicate,
    Revoked,
    AlreadyRevoked
}
