namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Responses.Dto;

public class UserDto
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = null!;
    /// <summary>Profile image URL when known (e.g. Google OAuth <c>picture</c> stored on the user row).</summary>
    public string? AvatarUrl { get; set; }
    public string? Email { get; set; }
    public bool IsAdmin { get; set; } = false;
    public bool IsBlocked { get; set; } = false;
    public bool HasDashboardAccess { get; set; } = false;
    public string Provider { get; set; } = default!; // e.g., "telegram", "google"
    public string ExternalId { get; set; } = default!; // provider user id as string
    public int? ProviderRowId { get; set; } // optional, e.g. TelegramBotUser.Id
    public DateTimeOffset CreateDate { get; set; }
    public DateTimeOffset LastUpdate { get; set; }
}