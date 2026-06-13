namespace DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;

public sealed class RevokeUserSessionsRequest
{
    /// <summary>When set, this refresh token's session is kept (revoke-others only).</summary>
    public string? KeepRefreshToken { get; set; }
}
