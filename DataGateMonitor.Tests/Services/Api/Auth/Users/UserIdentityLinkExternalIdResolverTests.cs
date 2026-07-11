using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Users;
using DataGateMonitor.SharedModels.Auth;
using Moq;

namespace DataGateMonitor.Tests.Services.Api.Auth.Users;

public class UserIdentityLinkExternalIdResolverTests
{
    [Fact]
    public void PickPreferredLink_PrefersGoogleOverTelegram()
    {
        var links = new List<UserIdentityLink>
        {
            new() { Provider = AuthIdentityProviders.Telegram, ExternalId = "12345" },
            new() { Provider = AuthIdentityProviders.Google, ExternalId = "google-sub" },
        };

        var picked = UserIdentityLinkExternalIdResolver.PickPreferredLink(links);

        Assert.Equal(AuthIdentityProviders.Google, picked!.Provider);
        Assert.Equal("google-sub", picked.ExternalId);
    }

    [Fact]
    public void PickPreferredLink_PrefersTelegramOverLocal()
    {
        var links = new List<UserIdentityLink>
        {
            new() { Provider = AuthIdentityProviders.Local, ExternalId = "local:42" },
            new() { Provider = AuthIdentityProviders.Telegram, ExternalId = "999" },
        };

        var picked = UserIdentityLinkExternalIdResolver.PickPreferredLink(links);

        Assert.Equal(AuthIdentityProviders.Telegram, picked!.Provider);
        Assert.Equal("999", picked.ExternalId);
    }

    [Fact]
    public async Task ResolveVpnExternalIdAsync_MapsTelegramIdToGoogleSub_WhenMerged()
    {
        const string telegramId = "555666777";
        const string googleSub = "accounts.google.com:sub123";
        var linkQuery = new Mock<IUserIdentityLinkQueryService>();

        linkQuery.Setup(q => q.GetByExternalId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityLink { UserId = 10, Provider = AuthIdentityProviders.Telegram, ExternalId = telegramId });
        linkQuery.Setup(q => q.GetListByUserId(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new UserIdentityLink { Provider = AuthIdentityProviders.Telegram, ExternalId = telegramId },
                new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = googleSub },
            ]);

        var resolved = await UserIdentityLinkExternalIdResolver.ResolveVpnExternalIdAsync(
            telegramId, linkQuery.Object, CancellationToken.None);

        Assert.Equal(googleSub, resolved);
    }

    [Fact]
    public async Task ResolveVpnExternalIdAsync_PassesThroughUnknownId()
    {
        var linkQuery = new Mock<IUserIdentityLinkQueryService>();
        linkQuery.Setup(q => q.GetByExternalId("legacy-user", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityLink?)null);

        var resolved = await UserIdentityLinkExternalIdResolver.ResolveVpnExternalIdAsync(
            "legacy-user", linkQuery.Object, CancellationToken.None);

        Assert.Equal("legacy-user", resolved);
    }

    [Fact]
    public async Task ResolveVpnExternalIdQueryKeysAsync_ReturnsAllLinkedIds()
    {
        const string telegramId = "555666777";
        const string googleSub = "accounts.google.com:sub123";
        var linkQuery = new Mock<IUserIdentityLinkQueryService>();

        linkQuery.Setup(q => q.GetByExternalId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityLink { UserId = 10, Provider = AuthIdentityProviders.Telegram, ExternalId = telegramId });
        linkQuery.Setup(q => q.GetListByUserId(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new UserIdentityLink { Provider = AuthIdentityProviders.Telegram, ExternalId = telegramId },
                new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = googleSub },
            ]);

        var keys = await UserIdentityLinkExternalIdResolver.ResolveVpnExternalIdQueryKeysAsync(
            telegramId, linkQuery.Object, CancellationToken.None);

        Assert.Equal([googleSub, telegramId], keys);
    }
}
