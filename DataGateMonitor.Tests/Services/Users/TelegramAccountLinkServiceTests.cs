using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Users;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;
using Xunit;

namespace DataGateMonitor.Tests.Services.Users;

public class TelegramAccountLinkServiceTests
{
    private const long DefaultTelegramId = 999;

    [Fact]
    public async Task RequestLinkCodeAsync_WithoutTelegramId_ReturnsCodeForBotCompletion()
    {
        var merge = new Mock<IUserMergeService>();
        merge.Setup(m => m.MergeTelegramGoogleAsync(
                It.IsAny<MergeTelegramGoogleUsersRequest>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MergeTelegramGoogleUsersResponse { SurvivorUserId = 10, MergedUserId = 10 });

        var sut = CreateSut(
            userId: 20,
            telegramUserId: 10,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "sub" }],
            merge: merge.Object);

        var result = await sut.RequestLinkCodeAsync(20, null, CancellationToken.None);

        Assert.Equal(8, result.Code.Length);

        var completed = await sut.CompleteLinkByCodeAsync(result.Code, DefaultTelegramId, CancellationToken.None);
        Assert.True(completed.Success);
    }

    [Fact]
    public async Task RequestLinkCodeFromBot_AndCompleteFromApp_LinksAccounts()
    {
        var merge = new Mock<IUserMergeService>();
        merge.Setup(m => m.MergeTelegramGoogleAsync(
                It.Is<MergeTelegramGoogleUsersRequest>(r => r.TelegramUserId == 10 && r.GoogleUserId == 20),
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MergeTelegramGoogleUsersResponse { SurvivorUserId = 10, MergedUserId = 20 });

        var sut = CreateSut(
            userId: 20,
            telegramUserId: 10,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "g-sub" }],
            merge: merge.Object);

        var issued = await sut.RequestLinkCodeFromBotAsync(DefaultTelegramId, CancellationToken.None);
        var result = await sut.CompleteLinkFromAppAsync(20, issued.Code, CancellationToken.None);

        Assert.True(result.Success);
        merge.Verify(
            m => m.MergeTelegramGoogleAsync(
                It.IsAny<MergeTelegramGoogleUsersRequest>(),
                20,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CompleteLinkFromApp_WhenCodeWasForBot_ReturnsFailure()
    {
        var sut = CreateSut(
            userId: 10,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "sub" }]);

        var issued = await sut.RequestLinkCodeAsync(10, null, CancellationToken.None);
        var result = await sut.CompleteLinkFromAppAsync(10, issued.Code, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Telegram bot", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestLinkCodeAsync_WhenUserHasGoogleLink_ReturnsCode()
    {
        var sut = CreateSut(
            userId: 10,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "sub" }]);

        var result = await sut.RequestLinkCodeAsync(10, DefaultTelegramId, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        Assert.Equal(8, result.Code.Length);
        Assert.True(result.ExpiresInSeconds > 0);
    }

    [Fact]
    public async Task RequestLinkCodeAsync_WhenAlreadyLinkedToTelegram_Throws()
    {
        var sut = CreateSut(
            userId: 5,
            links:
            [
                new UserIdentityLink { Provider = AuthIdentityProviders.Telegram, ExternalId = "123" },
                new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "sub" },
            ]);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RequestLinkCodeAsync(5, DefaultTelegramId, CancellationToken.None));

        Assert.Contains("already linked", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestLinkCodeAsync_WhenUserHasLocalLink_ReturnsCode()
    {
        var sut = CreateSut(
            userId: 6,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Local, ExternalId = "6" }]);

        var result = await sut.RequestLinkCodeAsync(6, DefaultTelegramId, CancellationToken.None);

        Assert.Equal(8, result.Code.Length);
    }

    [Fact]
    public async Task RequestLinkCodeAsync_WhenNoGoogleOrLocalLink_Throws()
    {
        var sut = CreateSut(userId: 7, links: []);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RequestLinkCodeAsync(7, DefaultTelegramId, CancellationToken.None));

        Assert.Contains("Google or password", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestLinkCodeAsync_WhenTelegramNotRegistered_Throws()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userQuery = new Mock<IUserQueryService>();
        var linkQuery = new Mock<IUserIdentityLinkQueryService>();
        var merge = new Mock<IUserMergeService>();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection(It.IsAny<string>())).Returns(Mock.Of<IConfigurationSection>());

        userQuery.Setup(q => q.GetById(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = 8, IsBlocked = false });
        linkQuery.Setup(q => q.GetListByUserId(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "sub" }]);
        linkQuery.Setup(q => q.GetByProviderAndExternalId(
                AuthIdentityProviders.Telegram,
                DefaultTelegramId.ToString(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityLink?)null);

        var sut = new TelegramAccountLinkService(
            cache,
            userQuery.Object,
            linkQuery.Object,
            merge.Object,
            config.Object,
            Mock.Of<ILogger<TelegramAccountLinkService>>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RequestLinkCodeAsync(8, DefaultTelegramId, CancellationToken.None));

        Assert.Contains("not registered", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestLinkCodeAsync_InvalidatesPreviousCode()
    {
        var sut = CreateSut(userId: 10, links:
        [
            new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "sub" },
        ]);

        var first = await sut.RequestLinkCodeAsync(10, DefaultTelegramId, CancellationToken.None);
        _ = await sut.RequestLinkCodeAsync(10, DefaultTelegramId, CancellationToken.None);

        var firstResult = await sut.CompleteLinkByCodeAsync(first.Code, DefaultTelegramId, CancellationToken.None);
        Assert.False(firstResult.Success);
        Assert.Contains("Invalid or expired", firstResult.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteLinkByCodeAsync_WhenCodeInvalid_ReturnsFailure()
    {
        var sut = CreateSut(userId: 1, links: []);

        var result = await sut.CompleteLinkByCodeAsync("BADCODE1", 12345, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Invalid or expired", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteLinkByCodeAsync_WhenTelegramIdMismatch_ReturnsFailureAndAllowsCorrectId()
    {
        var merge = new Mock<IUserMergeService>();
        merge.Setup(m => m.MergeTelegramGoogleAsync(
                It.IsAny<MergeTelegramGoogleUsersRequest>(),
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MergeTelegramGoogleUsersResponse { SurvivorUserId = 10, MergedUserId = 20 });

        var sut = CreateSut(
            userId: 20,
            telegramUserId: 10,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "g-sub" }],
            merge: merge.Object,
            registeredTelegramId: 111);

        var issued = await sut.RequestLinkCodeAsync(20, 111, CancellationToken.None);

        var wrong = await sut.CompleteLinkByCodeAsync(issued.Code, DefaultTelegramId, CancellationToken.None);
        Assert.False(wrong.Success);
        Assert.Contains("different Telegram account", wrong.Message, StringComparison.OrdinalIgnoreCase);

        var correct = await sut.CompleteLinkByCodeAsync(issued.Code, 111, CancellationToken.None);
        Assert.True(correct.Success);
    }

    [Fact]
    public async Task CompleteLinkByCodeAsync_WhenMergeFails_CodeRemainsValid()
    {
        var merge = new Mock<IUserMergeService>();
        merge.Setup(m => m.MergeTelegramGoogleAsync(
                It.IsAny<MergeTelegramGoogleUsersRequest>(),
                10,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("merge failed"));

        var sut = CreateSut(
            userId: 20,
            telegramUserId: 10,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "g-sub" }],
            merge: merge.Object);

        var issued = await sut.RequestLinkCodeAsync(20, DefaultTelegramId, CancellationToken.None);
        var first = await sut.CompleteLinkByCodeAsync(issued.Code, DefaultTelegramId, CancellationToken.None);
        var second = await sut.CompleteLinkByCodeAsync(issued.Code, DefaultTelegramId, CancellationToken.None);

        Assert.False(first.Success);
        Assert.False(second.Success);
        Assert.Contains("try again", first.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CompleteLinkByCodeAsync_WhenValid_CallsMergeAndRemovesCode()
    {
        var merge = new Mock<IUserMergeService>();
        merge.Setup(m => m.MergeTelegramGoogleAsync(
                It.Is<MergeTelegramGoogleUsersRequest>(r => r.TelegramUserId == 10 && r.GoogleUserId == 20),
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MergeTelegramGoogleUsersResponse
            {
                SurvivorUserId = 10,
                MergedUserId = 20,
            });

        var sut = CreateSut(
            userId: 20,
            telegramUserId: 10,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "g-sub" }],
            merge: merge.Object);

        var issued = await sut.RequestLinkCodeAsync(20, DefaultTelegramId, CancellationToken.None);
        var result = await sut.CompleteLinkByCodeAsync(issued.Code, DefaultTelegramId, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Merge);

        var reuse = await sut.CompleteLinkByCodeAsync(issued.Code, DefaultTelegramId, CancellationToken.None);
        Assert.False(reuse.Success);
        Assert.Contains("Invalid or expired", reuse.Message, StringComparison.OrdinalIgnoreCase);

        merge.Verify(
            m => m.MergeTelegramGoogleAsync(
                It.IsAny<MergeTelegramGoogleUsersRequest>(),
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static TelegramAccountLinkService CreateSut(
        int userId,
        IReadOnlyList<UserIdentityLink> links,
        long registeredTelegramId = DefaultTelegramId,
        int telegramUserId = 0,
        IMemoryCache? cache = null,
        IUserMergeService? merge = null)
    {
        cache ??= new MemoryCache(new MemoryCacheOptions());
        var userQuery = new Mock<IUserQueryService>();
        var linkQuery = new Mock<IUserIdentityLinkQueryService>();
        var mergeMock = merge is null ? new Mock<IUserMergeService>() : null;
        var mergeService = merge ?? mergeMock!.Object;
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection(It.IsAny<string>())).Returns(Mock.Of<IConfigurationSection>());

        var resolvedTelegramUserId = telegramUserId > 0 ? telegramUserId : userId;

        userQuery.Setup(q => q.GetById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, IsBlocked = false });
        linkQuery.Setup(q => q.GetListByUserId(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(links.ToList());
        linkQuery.Setup(q => q.GetByProviderAndExternalId(
                AuthIdentityProviders.Telegram,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string externalId, CancellationToken _) =>
            {
                if (!string.Equals(externalId, registeredTelegramId.ToString(), StringComparison.Ordinal))
                    return null;

                return new UserIdentityLink
                {
                    UserId = resolvedTelegramUserId,
                    Provider = AuthIdentityProviders.Telegram,
                    ExternalId = externalId,
                };
            });

        return new TelegramAccountLinkService(
            cache,
            userQuery.Object,
            linkQuery.Object,
            mergeService,
            config.Object,
            Mock.Of<ILogger<TelegramAccountLinkService>>());
    }
}
