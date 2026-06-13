using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Users;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Users;

public class LocalUserIdentityLinkEnsurerTests
{
    [Fact]
    public void CreateLink_UsesLocalProviderAndUserIdAsExternalId()
    {
        var link = LocalUserIdentityLinkEnsurer.CreateLink(77);

        Assert.Equal(77, link.UserId);
        Assert.Equal(AuthIdentityProviders.Local, link.Provider);
        Assert.Equal("local:77", link.ExternalId);
    }

    [Fact]
    public async Task EnsureAsync_WhenLinkExists_DoesNotCreate()
    {
        var query = new Mock<IUserIdentityLinkQueryService>();
        var command = new Mock<ICommandService<UserIdentityLink, int>>();

        query.Setup(q => q.AnyByUserId(5, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await LocalUserIdentityLinkEnsurer.EnsureAsync(5, query.Object, command.Object, CancellationToken.None);

        command.Verify(
            c => c.Add(It.IsAny<UserIdentityLink>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task EnsureAsync_WhenLinkMissing_CreatesLocalLink()
    {
        var query = new Mock<IUserIdentityLinkQueryService>();
        var command = new Mock<ICommandService<UserIdentityLink, int>>();

        query.Setup(q => q.AnyByUserId(9, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        UserIdentityLink? captured = null;
        command
            .Setup(c => c.Add(It.IsAny<UserIdentityLink>(), true, It.IsAny<CancellationToken>()))
            .Callback<UserIdentityLink, bool, CancellationToken>((l, _, _) => captured = l)
            .ReturnsAsync((UserIdentityLink l, bool _, CancellationToken _) => l);

        await LocalUserIdentityLinkEnsurer.EnsureAsync(9, query.Object, command.Object, CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal(9, captured!.UserId);
        Assert.Equal(AuthIdentityProviders.Local, captured.Provider);
        Assert.Equal("local:9", captured.ExternalId);
    }
}
