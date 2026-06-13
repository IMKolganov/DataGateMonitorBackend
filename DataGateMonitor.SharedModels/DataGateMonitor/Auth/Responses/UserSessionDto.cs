namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

public sealed class UserSessionDto
{
    public int Id { get; init; }
    public string? DeviceId { get; init; }
    public string? UserAgent { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class GetUserSessionsResponse
{
    public IReadOnlyList<UserSessionDto> Sessions { get; init; } = [];
}
