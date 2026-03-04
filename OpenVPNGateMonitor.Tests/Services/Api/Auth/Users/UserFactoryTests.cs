using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Users;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Api.Auth.Users;

public class UserFactoryTests
{
    [Fact]
    public void CreateNew_Sets_DisplayName_And_Email()
    {
        var user = UserFactory.CreateNew("Display", "a@b.com");

        Assert.Equal("Display", user.DisplayName);
        Assert.Equal("a@b.com", user.Email);
        Assert.False(user.IsAdmin);
        Assert.False(user.IsBlocked);
        Assert.True(user.HasDashboardAccess);
    }

    [Fact]
    public void CreateNew_Allows_Null_Email()
    {
        var user = UserFactory.CreateNew("Name", null);

        Assert.Equal("Name", user.DisplayName);
        Assert.Null(user.Email);
        Assert.False(user.IsAdmin);
        Assert.True(user.HasDashboardAccess);
    }
}
