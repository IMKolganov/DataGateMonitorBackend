namespace OpenVPNGateMonitor.Services.Api.Auth.Login;

public interface ITokenService
{
    Task<TokenPair> IssueAsync(int userId, string? externalId, string? deviceId, string? userAgent, CancellationToken ct);
}