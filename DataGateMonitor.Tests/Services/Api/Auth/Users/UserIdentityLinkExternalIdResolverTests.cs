using Moq;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Users;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Users;

public class UserIdentityLinkExternalIdResolverTests
{
    [Fact]
    public void PickPreferredLink_PrefersTelegramOverGoogleAndLocal()
    {
        var links = new List<UserIdentityLink>
        {
            new() { Provider = AuthIdentityProviders.Local, ExternalId = "local:1" },
            new() { Provider = AuthIdentityProviders.Google, ExternalId = "google-sub" },
            new() { Provider = AuthIdentityProviders.Telegram, ExternalId = "12345" },
        };

        var picked = UserIdentityLinkExternalIdResolver.PickPreferredLink(links);

        Assert.Equal(AuthIdentityProviders.Telegram, picked!.Provider);
        Assert.Equal("12345", picked.ExternalId);
    }

    [Fact]
    public void PickPreferredLink_PrefersGoogleOverLocal()
    {
        var links = new List<UserIdentityLink>
        {
            new() { Provider = AuthIdentityProviders.Local, ExternalId = "local:9" },
            new() { Provider = AuthIdentityProviders.Google, ExternalId = "google-sub" },
        };

        var picked = UserIdentityLinkExternalIdResolver.PickPreferredLink(links);

        Assert.Equal(AuthIdentityProviders.Google, picked!.Provider);
        Assert.Equal("google-sub", picked.ExternalId);
    }

    [Fact]
    public void PickPreferredLink_UsesLocalWhenOnlyPasswordAuthExists()
    {
        var links = new List<UserIdentityLink>
        {
            new() { Provider = AuthIdentityProviders.Local, ExternalId = "local:42" },
        };

        var picked = UserIdentityLinkExternalIdResolver.PickPreferredLink(links);

        Assert.Equal(AuthIdentityProviders.Local, picked!.Provider);
        Assert.Equal("local:42", picked.ExternalId);
    }

    [Fact]
    public async Task ResolveAsync_ReturnsNull_WhenUserHasNoLinks()
    {
        var query = new Mock<IUserIdentityLinkQueryService>();
        query.Setup(q => q.GetListByUserId(404, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var resolved = await UserIdentityLinkExternalIdResolver.ResolveAsync(404, query.Object, CancellationToken.None);

        Assert.Null(resolved);
    }

    [Fact]
    public async Task ResolveAsync_DelegatesToPickPreferredLink()
    {
        var query = new Mock<IUserIdentityLinkQueryService>();
        query
            .Setup(q => q.GetListByUserId(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityLink>
            {
                new() { Provider = AuthIdentityProviders.Local, ExternalId = "local:1" },
                new() { Provider = AuthIdentityProviders.Telegram, ExternalId = "999888777" },
            });

        var resolved = await UserIdentityLinkExternalIdResolver.ResolveAsync(1, query.Object, CancellationToken.None);

        Assert.Equal("999888777", resolved);
    }
}
