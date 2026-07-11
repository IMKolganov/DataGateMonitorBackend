namespace DataGateMonitor.SharedModels.DataGateMonitor.User;

/// <summary>Who applied a password change recorded in history.</summary>
public enum PasswordSetActorKind
{
    User = 0,
    Admin = 1,
    System = 2,
}
