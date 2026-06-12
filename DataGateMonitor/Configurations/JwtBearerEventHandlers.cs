using Microsoft.IdentityModel.Tokens;

namespace DataGateMonitor.Configurations;

/// <summary>
/// JWT bearer events where failure is expected client behaviour (stale tab, SignalR reconnect), not a server fault.
/// </summary>
public static class JwtBearerEventHandlers
{
    public static bool IsExpectedClientTokenFailure(Exception? exception) =>
        exception is SecurityTokenExpiredException or SecurityTokenNotYetValidException;
}
