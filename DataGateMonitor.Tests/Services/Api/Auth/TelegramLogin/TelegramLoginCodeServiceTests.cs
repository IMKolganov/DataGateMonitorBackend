using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using DataGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using DataGateMonitor.DataBase.Services.Query.UserCredentialTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.TelegramLogin;
using DataGateMonitor.Services.Api.Auth.Totp;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.TelegramLogin;

public class TelegramLoginCodeServiceTests
{
    [Fact]
    public async Task LoginWithCodeAsync_When_CodeEmpty_ThrowsUnauthorizedAccessException()
    {
        var sut = CreateSut(new ConfigurationBuilder().Build());

        var request = new TelegramCodeLoginRequest { Code = "   " };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.LoginWithCodeAsync(request, CancellationToken.None));

        Assert.Equal("Invalid or expired code.", ex.Message);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_WhenUserMissing_ReturnsNull()
    {
        const long telegramId = 1001;
        var telegramQuery = new Mock<ITelegramBotUserQueryService>();
        telegramQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        var sut = CreateSut(new ConfigurationBuilder().Build(), telegramQuery);

        var result = await sut.RequestLoginCodeAsync(
            new TelegramRequestLoginCodeRequest { TelegramId = telegramId },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_WhenUserBlocked_ReturnsNull()
    {
        const long telegramId = 1002;
        var telegramQuery = new Mock<ITelegramBotUserQueryService>();
        telegramQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelegramBotUser { TelegramId = telegramId, IsBlocked = true });

        var sut = CreateSut(new ConfigurationBuilder().Build(), telegramQuery);

        var result = await sut.RequestLoginCodeAsync(
            new TelegramRequestLoginCodeRequest { TelegramId = telegramId },
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_WhenUserValid_ReturnsCodeAndExpiryFromConfig()
    {
        const long telegramId = 1003;
        var telegramQuery = new Mock<ITelegramBotUserQueryService>();
        telegramQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelegramBotUser { TelegramId = telegramId, IsBlocked = false });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:TelegramLoginCodeMinutes"] = "7",
            })
            .Build();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(config, telegramQuery, cache);

        var result = await sut.RequestLoginCodeAsync(
            new TelegramRequestLoginCodeRequest { TelegramId = telegramId },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(8, result!.Code.Length);
        Assert.Equal(7 * 60, result.ExpiresInSeconds);
        Assert.True(cache.TryGetValue($"telegram_login:{result.Code.ToUpperInvariant()}", out long cachedId));
        Assert.Equal(telegramId, cachedId);
    }

    [Fact]
    public async Task RequestLoginCodeAsync_WhenConfigInvalid_UsesDefaultFiveMinutes()
    {
        const long telegramId = 1004;
        var telegramQuery = new Mock<ITelegramBotUserQueryService>();
        telegramQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelegramBotUser { TelegramId = telegramId });

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:TelegramLoginCodeMinutes"] = "0",
            })
            .Build();

        var sut = CreateSut(config, telegramQuery);

        var result = await sut.RequestLoginCodeAsync(
            new TelegramRequestLoginCodeRequest { TelegramId = telegramId },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(5 * 60, result!.ExpiresInSeconds);
    }

    private static TelegramLoginCodeService CreateSut(
        IConfiguration configuration,
        Mock<ITelegramBotUserQueryService>? telegramQuery = null,
        IMemoryCache? cache = null)
    {
        telegramQuery ??= new Mock<ITelegramBotUserQueryService>();

        return new TelegramLoginCodeService(
            telegramQuery.Object,
            new Mock<IUserService>().Object,
            new Mock<ITokenService>().Object,
            new Mock<IAdminTotpService>().Object,
            new Mock<IUserCredentialQueryService>().Object,
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>().Object,
            configuration);
    }
}
