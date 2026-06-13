using Microsoft.IdentityModel.Tokens;

namespace DataGateMonitor.Configurations;

/// <summary>
/// JWT bearer events where failure is expected client behaviour (stale tab, SignalR reconnect), not a server fault.
/// </summary>
public static class JwtBearerEventHandlers
{
    public static bool IsExpectedClientTokenFailure(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SecurityTokenExpiredException or SecurityTokenNotYetValidException)
                return true;

            if (current.Message.Contains("IDX10223", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("IDX10225", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
