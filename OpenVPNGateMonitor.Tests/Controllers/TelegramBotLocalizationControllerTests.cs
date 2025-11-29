using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotLocalization.Responses;
using OpenVPNGateMonitor.SharedModels.Enums;
using OpenVPNGateMonitor.SharedModels.Responses;

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
        // Arrange
        _loc.Setup(l => l.SetTelegramUserLanguageAsync(
                It.IsAny<TelegramUserLanguagePreference>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TelegramUserLanguagePreference
            {
                TelegramId = 42,
                PreferredLanguage = Language.English
            });

        var request = new SetTelegramUserLanguageRequest
        {
            TelegramId = 42,
            PreferredLanguage = Language.English
        };

        // Act
        var result = await _controller.SetTelegramUserLanguageAsync(
            request,
            CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<SetTelegramUserLanguageResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var data = response.Data!;
        Assert.Equal(42, data.TelegramId);
        Assert.Equal(Language.English, data.PreferredLanguage);

        _loc.Verify(l => l.SetTelegramUserLanguageAsync(
                It.Is<TelegramUserLanguagePreference>(p =>
                    p.TelegramId == 42 &&
                    p.PreferredLanguage == Language.English),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetTelegramUserLanguage_Returns_Ok()
    {
        // Arrange
        _loc.Setup(l => l.GetTelegramUserLanguageAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Language.Greek);

        var request = new GetTelegramUserLanguageRequest
        {
            TelegramId = 42
        };

        // Act
        var result = await _controller.GetTelegramUserLanguageAsync(
            request,
            CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetTelegramUserLanguageResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var data = response.Data!;
        Assert.Equal(Language.Greek, data.PreferredLanguage);

        _loc.Verify(l => l.GetTelegramUserLanguageAsync(42, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task IsExistTelegramUserLanguagePreference_Returns_Ok()
    {
        // Arrange
        _loc.Setup(l => l.IsExistTelegramUserLanguagePreferenceAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new IsExistTelegramUserLanguagePreferenceRequest
        {
            TelegramId = 42
        };

        // Act
        var result = await _controller.IsExistTelegramUserLanguagePreferenceAsync(
            request,
            CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response =
            Assert.IsType<ApiResponse<IsExistTelegramUserLanguagePreferenceResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var data = response.Data!;
        Assert.True(data.IsExistTelegramUserLanguagePreference);

        _loc.Verify(l => l.IsExistTelegramUserLanguagePreferenceAsync(42, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetText_Returns_Ok()
    {
        // Arrange
        _loc.Setup(l => l.GetTextForTelegramUser(
                "hello",
                42,
                It.IsAny<CancellationToken>(),
                null))
            .ReturnsAsync("Hello");

        var request = new GetTextForTelegramUserRequest
        {
            TelegramId = 42,
            Key = "hello"
        };

        // Act
        var result = await _controller.GetTextAsync(
            request,
            CancellationToken.None);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<GetTextForTelegramUserResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.NotNull(response.Data);

        var data = response.Data!;
        Assert.Equal("Hello", data.Text);

        _loc.Verify(l => l.GetTextForTelegramUser(
                "hello",
                42,
                It.IsAny<CancellationToken>(),
                null),
            Times.Once);
    }
}
