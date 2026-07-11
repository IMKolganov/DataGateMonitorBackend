namespace DataGateMonitor.Services.Users.Interfaces;

/// <summary>
/// Tells a user they were disconnected by free-tier session enforcement (grace period expired /
/// never became compliant): Telegram DM if they have a linked Telegram account, otherwise email.
/// </summary>
public interface IFreeTierGraceDisconnectNotifier
{
    Task NotifyAsync(int userId, CancellationToken ct = default);
}
