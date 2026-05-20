namespace DataGateMonitor.Services.Api.Auth.Totp;

internal sealed class AdminLoginChallenge
{
    public required int UserId { get; init; }
    public string? ExternalId { get; init; }
    public string? DeviceId { get; init; }
    public string? UserAgent { get; init; }
    public DateTimeOffset ExpiresAtUtc { get; init; }
}
