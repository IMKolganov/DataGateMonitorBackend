namespace OpenVPNGateMonitor.Services.Api.Auth.Login;

public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt
);