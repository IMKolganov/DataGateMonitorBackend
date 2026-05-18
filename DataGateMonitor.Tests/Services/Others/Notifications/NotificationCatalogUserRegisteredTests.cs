using DataGateMonitor.Services.Others.Notifications;
using DataGateMonitor.SharedModels.Enums;
using Xunit;

namespace DataGateMonitor.Tests.Services.Others.Notifications;

public class NotificationCatalogUserRegisteredTests
{
    private readonly NotificationCatalog _catalog = new();

    [Fact]
    public void UserRegistered_IncludesGoogleSource()
    {
        var env = _catalog.UserRegistered(7, "Alice", login: null, email: "a@example.com", registrationSource: "Google");

        Assert.Equal("user.registered", env.Request.Type);
        Assert.Contains("Via Google", env.Request.Message);
        Assert.Contains("a@example.com", env.Request.Message);
        Assert.Equal(ApplicationNotificationKind.AppUserRegistered, env.Request.PreferenceKind);
        Assert.Contains("web", env.Channels);
        Assert.Contains("telegram", env.Channels);
    }

    [Fact]
    public void UserRegistered_IncludesTelegramSource()
    {
        var env = _catalog.UserRegistered(8, "tg_user", login: null, email: null, registrationSource: "Telegram bot");

        Assert.Contains("Via Telegram bot", env.Request.Message);
        Assert.DoesNotContain("Login:", env.Request.Message);
    }
}
