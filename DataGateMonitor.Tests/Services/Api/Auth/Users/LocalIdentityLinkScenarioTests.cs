using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Users;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Users;

/// <summary>
/// End-to-end scenarios for email/password users and migration backfill behaviour.
/// </summary>
public class LocalIdentityLinkScenarioTests
{
    [Fact]
    public void FormatExternalId_DoesNotCollideWithNumericTelegramId()
    {
        Assert.Equal("local:42", LocalUserIdentityLinkEnsurer.FormatExternalId(42));
        Assert.NotEqual("42", LocalUserIdentityLinkEnsurer.FormatExternalId(42));
    }

    [Fact]
    public async Task BackfillScenario_ExistingCredentialWithoutLink_GetsLocalPrefixedExternalId()
    {
        var query = new Mock<IUserIdentityLinkQueryService>();
        var command = new Mock<ICommandService<UserIdentityLink, int>>();

        const int existingUserId = 100;

        query.Setup(q => q.AnyByUserId(existingUserId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        UserIdentityLink? created = null;
        command
            .Setup(c => c.Add(It.IsAny<UserIdentityLink>(), true, It.IsAny<CancellationToken>()))
            .Callback<UserIdentityLink, bool, CancellationToken>((l, _, _) => created = l)
            .ReturnsAsync((UserIdentityLink l, bool _, CancellationToken _) => l);

        await LocalUserIdentityLinkEnsurer.EnsureAsync(existingUserId, query.Object, command.Object, CancellationToken.None);

        Assert.NotNull(created);
        Assert.Equal(AuthIdentityProviders.Local, created!.Provider);
        Assert.Equal("local:100", created.ExternalId);
    }

    [Fact]
    public async Task BackfillScenario_UserWithTelegramLink_IsNotGivenLocalLink()
    {
        var query = new Mock<IUserIdentityLinkQueryService>();
        var command = new Mock<ICommandService<UserIdentityLink, int>>();

        query.Setup(q => q.AnyByUserId(200, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await LocalUserIdentityLinkEnsurer.EnsureAsync(200, query.Object, command.Object, CancellationToken.None);

        command.Verify(
            c => c.Add(It.IsAny<UserIdentityLink>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HybridScenario_EmailThenGoogle_ResolverKeepsGoogleForVpn()
    {
        var query = new Mock<IUserIdentityLinkQueryService>();
        query
            .Setup(q => q.GetListByUserId(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityLink>
            {
                new() { UserId = 10, Provider = AuthIdentityProviders.Local, ExternalId = "local:10" },
                new() { UserId = 10, Provider = AuthIdentityProviders.Google, ExternalId = "accounts.google.com:sub" },
            });

        var resolved = await UserIdentityLinkExternalIdResolver.ResolveAsync(10, query.Object, CancellationToken.None);

        Assert.Equal("accounts.google.com:sub", resolved);
    }
}
