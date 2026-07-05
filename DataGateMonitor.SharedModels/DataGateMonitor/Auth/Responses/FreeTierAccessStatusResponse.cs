namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

/// <summary>
/// Free/Default plan access status for client apps (channel subscription or merged Telegram account).
/// </summary>
public sealed class FreeTierAccessStatusResponse
{
    /// <summary>True when the user has an active Free or Default quota plan.</summary>
    public bool IsApplicable { get; set; }

    /// <summary>True when no onboarding is required (merged, subscribed, or active grace).</summary>
    public bool IsCompliant { get; set; }

    public bool IsMergedAccount { get; set; }

    public bool IsChannelSubscribed { get; set; }

    public bool IsGracePeriod { get; set; }

    /// <summary>User already has a Telegram identity link on this account.</summary>
    public bool IsLinkedToTelegram { get; set; }

    /// <summary>May call POST /api/auth/telegram/request-account-link-code.</summary>
    public bool CanRequestAccountLinkCode { get; set; }

    public string? ActivePlanName { get; set; }

    /// <summary>Required public channel, e.g. @DataGateVPNBot.</summary>
    public string RequiredChannel { get; set; } = string.Empty;
}
