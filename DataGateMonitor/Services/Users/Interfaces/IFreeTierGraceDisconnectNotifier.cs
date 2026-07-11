namespace DataGateMonitor.Services.Users.Interfaces;

/// <summary>Which channel was attempted (if any) and whether it was actually delivered.</summary>
/// <param name="Channel">"telegram", "email", or null if no channel was available.</param>
public sealed record FreeTierGraceDisconnectOutcome(string? Channel, bool Sent)
{
    public static readonly FreeTierGraceDisconnectOutcome NoChannelAvailable = new(null, false);
}

/// <summary>
/// Tells a user they were disconnected by free-tier session enforcement (grace period expired /
/// never became compliant): Telegram DM if they have a linked Telegram account, otherwise email.
/// </summary>
public interface IFreeTierGraceDisconnectNotifier
{
    Task<FreeTierGraceDisconnectOutcome> NotifyAsync(int userId, CancellationToken ct = default);
}
