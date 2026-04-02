using System.Security.Claims;

namespace OpenVPNGateMonitor.Services.Api;

/// <summary>JWT / role helpers for API controllers.</summary>
public static class HttpUserContext
{
    /// <summary>Admin and service accounts — full server list and no per-server quota checks.</summary>
    public static bool IsPrivileged(ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("App");

    public static bool TryGetUserId(ClaimsPrincipal user, out int userId)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        userId = 0;
        return !string.IsNullOrEmpty(raw) && int.TryParse(raw, out userId);
    }
}
