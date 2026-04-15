using Microsoft.Extensions.Caching.Memory;
using Moq;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.TelegramLogin;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.TelegramLogin;

public class TelegramLoginCodeServiceTests
{
    [Fact]
    public async Task LoginWithCodeAsync_When_CodeEmpty_ThrowsUnauthorizedAccessException()
    {
        var telegramUserQuery = new Mock<DataGateMonitor.DataBase.Services.Query.TelegramBotUserTable.ITelegramBotUserQueryService>();
        var userService = new Mock<IUserService>();
        var tokenService = new Mock<ITokenService>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var httpContextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();

        var sut = new TelegramLoginCodeService(
            telegramUserQuery.Object,
            userService.Object,
            tokenService.Object,
            cache,
            httpContextAccessor.Object);

        var request = new TelegramCodeLoginRequest { Code = "   " };

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.LoginWithCodeAsync(request, CancellationToken.None));

        Assert.Equal("Invalid or expired code.", ex.Message);
    }
}
