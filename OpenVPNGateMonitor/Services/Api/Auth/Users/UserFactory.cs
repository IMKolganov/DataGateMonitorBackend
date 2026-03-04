using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Api.Auth.Users;


public static class UserFactory
{
    public static User CreateNew(string displayName, string? email)
    {
        return new User
        {
            DisplayName = displayName,
            Email = email,
            IsAdmin = false,
            IsBlocked = false,
            HasDashboardAccess = true
        };
    }
}