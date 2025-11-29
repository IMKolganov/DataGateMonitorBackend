using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class TelegramBotLocalizationControllerTests
{
    private readonly Mock<ILocalizationService> _loc = new();
    private readonly TelegramBotLocalizationController _controller;

    public TelegramBotLocalizationControllerTests()
    {
        _controller = new TelegramBotLocalizationController(_loc.Object);
    }

    [Fact]
    public async Task SetTelegramUserLanguage_Returns_Ok()
    {
        _loc.Setup(l => l.SetTelegramUserLanguageAsync(
                It.IsAny<TelegramUserLanguagePreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelegramUserLanguagePreference
            {
                TelegramId = 42,
                PreferredLanguage = Language.English
            });

        var result = await _controller.SetTelegramUserLanguageAsync(
            new SetTelegramUserLanguageRequest
            {
                TelegramId = 42,
                PreferredLanguage = Language.English
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SetTelegramUserLanguageResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var data = response.Data!;
        Assert.Equal(Language.English, data.PreferredLanguage);
    }

    [Fact]
    public async Task GetText_Returns_Ok()
    {
        _loc.Setup(l => l.GetTextForTelegramUser(
                "hello",
                42,
                It.IsAny<CancellationToken>(),
                It.IsAny<Language?>()))
            .ReturnsAsync("Hello");

        var result = await _controller.GetTextAsync(
            new GetTextForTelegramUserRequest
            {
                TelegramId = 42,
                Key = "hello"
            },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetTextForTelegramUserResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var data = response.Data!;
        Assert.Equal("Hello", data.Text);
    }
}
