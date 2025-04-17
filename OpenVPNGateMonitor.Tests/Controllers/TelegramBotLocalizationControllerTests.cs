using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using OpenVPNGateMonitor.Controllers;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Models.Enums;
using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.SharedModels.Responses;
using OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Requests;
using OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Responses;
using SharedLanguage = OpenVPNGateMonitor.SharedModels.TelegramBotLocalization.Enums.Language;

namespace OpenVPNGateMonitor.Tests.Controllers;

public class TelegramBotLocalizationControllerTests
{
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly TelegramBotLocalizationController _controller;

    public TelegramBotLocalizationControllerTests()
    {
        _localizationServiceMock = new Mock<ILocalizationService>();
        _controller = new TelegramBotLocalizationController(_localizationServiceMock.Object);
    }

    [Fact]
    public async Task SetTelegramUserLanguageAsync_ReturnsExpectedResponse()
    {
        var request = new SetTelegramUserLanguageRequest
        {
            TelegramId = 12345,
            PreferredLanguage = SharedLanguage.English
        };

        var preference = new TelegramUserLanguagePreference
        {
            TelegramId = 12345,
            PreferredLanguage = Language.English
        };

        _localizationServiceMock
            .Setup(x => x.SetTelegramUserLanguageAsync(It.IsAny<TelegramUserLanguagePreference>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        var result = await _controller.SetTelegramUserLanguageAsync(request);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var response = okResult!.Value as ApiResponse<SetTelegramUserLanguageResponse>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.TelegramId.Should().Be(12345);
        response.Data.PreferredLanguage.Should().Be(SharedLanguage.English);
    }

    [Fact]
    public async Task GetTelegramUserLanguageAsync_ReturnsExpectedResponse()
    {
        var request = new GetTelegramUserLanguageRequest
        {
            TelegramId = 12345
        };

        _localizationServiceMock
            .Setup(x => x.GetTelegramUserLanguageAsync(
                request.TelegramId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Language.Russian);

        var result = await _controller.GetTelegramUserLanguageAsync(request);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var response = okResult!.Value as ApiResponse<GetTelegramUserLanguageResponse>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.PreferredLanguage.Should().Be(SharedLanguage.Russian);
    }

    [Fact]
    public async Task IsExistTelegramUserLanguagePreferenceAsync_ReturnsExpectedResponse()
    {
        var request = new IsExistTelegramUserLanguagePreferenceRequest { TelegramId = 54321 };

        _localizationServiceMock
            .Setup(x => x.IsExistTelegramUserLanguagePreferenceAsync(
                request.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.IsExistTelegramUserLanguagePreferenceAsync(request);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var response = okResult!.Value as ApiResponse<IsExistTelegramUserLanguagePreferenceResponse>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.IsExistTelegramUserLanguagePreference.Should().BeTrue();
    }

    [Fact]
    public async Task GetTextAsync_ReturnsExpectedResponse()
    {
        var request = new GetTextForTelegramUserRequest
        {
            TelegramId = 111,
            Key = "some_text"
        };

        _localizationServiceMock
            .Setup(x => x.GetTextForTelegramUser(
                It.Is<string>(k => k == request.Key),
                It.Is<long>(id => id == request.TelegramId),
                It.IsAny<CancellationToken>(),
                It.Is<Language?>(lang => lang == null)))
            .ReturnsAsync("long text");

        var result = await _controller.GetTextAsync(request);

        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();

        var response = okResult!.Value as ApiResponse<GetTextForTelegramUserResponse>;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.Data!.Text.Should().Be("long text");
    }
}
