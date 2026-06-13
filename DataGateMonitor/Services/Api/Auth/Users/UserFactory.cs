using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Api.Auth.Users;


public static class UserFactory
{
    public static User CreateNew(string displayName, string? email, bool isEmailConfirmed = false)
    {
        return new User
        {
            DisplayName = displayName,
            Email = email,
            IsEmailConfirmed = string.IsNullOrWhiteSpace(email) || isEmailConfirmed,
            IsAdmin = false,
            IsBlocked = false,
            HasDashboardAccess = true
        };
    }
}