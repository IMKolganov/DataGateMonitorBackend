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
    [Fact]
    public async Task RequestLinkCodeAsync_WhenUserHasGoogleLink_ReturnsCode()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userQuery = new Mock<IUserQueryService>();
        var links = new Mock<IUserIdentityLinkQueryService>();
        var merge = new Mock<IUserMergeService>();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection(It.IsAny<string>())).Returns(Mock.Of<IConfigurationSection>());

        var user = new User { Id = 10, IsBlocked = false };
        userQuery.Setup(q => q.GetById(10, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        links.Setup(q => q.GetListByUserId(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "sub" }]);

        var sut = new TelegramAccountLinkService(
            cache,
            userQuery.Object,
            links.Object,
            merge.Object,
            config.Object,
            Mock.Of<ILogger<TelegramAccountLinkService>>());

        var result = await sut.RequestLinkCodeAsync(10, CancellationToken.None);

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
            () => sut.RequestLinkCodeAsync(5, CancellationToken.None));

        Assert.Contains("already linked", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RequestLinkCodeAsync_WhenUserHasLocalLink_ReturnsCode()
    {
        var sut = CreateSut(
            userId: 6,
            links: [new UserIdentityLink { Provider = AuthIdentityProviders.Local, ExternalId = "6" }]);

        var result = await sut.RequestLinkCodeAsync(6, CancellationToken.None);

        Assert.Equal(8, result.Code.Length);
    }

    [Fact]
    public async Task RequestLinkCodeAsync_WhenNoGoogleOrLocalLink_Throws()
    {
        var sut = CreateSut(userId: 7, links: []);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.RequestLinkCodeAsync(7, CancellationToken.None));

        Assert.Contains("Google or password", ex.Message, StringComparison.OrdinalIgnoreCase);
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
    public async Task CompleteLinkByCodeAsync_WhenValid_CallsMerge()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userQuery = new Mock<IUserQueryService>();
        var links = new Mock<IUserIdentityLinkQueryService>();
        var merge = new Mock<IUserMergeService>();
        var config = new Mock<IConfiguration>();

        const string code = "ABCD2345";
        cache.Set("account-link:code:ABCD2345", 20);

        links.Setup(q => q.GetByProviderAndExternalId(
                AuthIdentityProviders.Telegram,
                "999",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserIdentityLink { UserId = 10, Provider = AuthIdentityProviders.Telegram, ExternalId = "999" });

        links.Setup(q => q.GetListByUserId(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserIdentityLink { Provider = AuthIdentityProviders.Google, ExternalId = "g-sub" }]);

        merge.Setup(m => m.MergeTelegramGoogleAsync(
                It.Is<MergeTelegramGoogleUsersRequest>(r => r.TelegramUserId == 10 && r.GoogleUserId == 20),
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MergeTelegramGoogleUsersResponse
            {
                SurvivorUserId = 10,
                MergedUserId = 20,
            });

        var sut = new TelegramAccountLinkService(
            cache,
            userQuery.Object,
            links.Object,
            merge.Object,
            config.Object,
            Mock.Of<ILogger<TelegramAccountLinkService>>());

        var result = await sut.CompleteLinkByCodeAsync(code, 999, CancellationToken.None);

        Assert.True(result.Success);
        Assert.NotNull(result.Merge);
        merge.Verify(
            m => m.MergeTelegramGoogleAsync(
                It.IsAny<MergeTelegramGoogleUsersRequest>(),
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static TelegramAccountLinkService CreateSut(int userId, IReadOnlyList<UserIdentityLink> links)
    {
        var cache = new MemoryCache(new MemoryCacheOptions());
        var userQuery = new Mock<IUserQueryService>();
        var linkQuery = new Mock<IUserIdentityLinkQueryService>();
        var merge = new Mock<IUserMergeService>();
        var config = new Mock<IConfiguration>();
        config.Setup(c => c.GetSection(It.IsAny<string>())).Returns(Mock.Of<IConfigurationSection>());

        userQuery.Setup(q => q.GetById(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User { Id = userId, IsBlocked = false });
        linkQuery.Setup(q => q.GetListByUserId(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(links.ToList());

        return new TelegramAccountLinkService(
            cache,
            userQuery.Object,
            linkQuery.Object,
            merge.Object,
            config.Object,
            Mock.Of<ILogger<TelegramAccountLinkService>>());
    }
}
