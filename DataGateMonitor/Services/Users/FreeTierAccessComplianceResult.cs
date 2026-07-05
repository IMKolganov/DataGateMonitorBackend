namespace DataGateMonitor.Services.Users;

public sealed class FreeTierAccessComplianceResult
{
    public bool IsApplicable { get; init; }

    public bool IsCompliant { get; init; }

    public bool IsMergedAccount { get; init; }

    public bool IsChannelSubscribed { get; init; }

    public string? ActivePlanName { get; init; }

    public int? UserId { get; init; }

    public long? TelegramId { get; set; }

    public bool AdminsNotified { get; init; }
}
