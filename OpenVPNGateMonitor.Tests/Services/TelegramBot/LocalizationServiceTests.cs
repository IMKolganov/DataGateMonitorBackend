using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.LocalizationTextTable;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramUserLanguagePreferenceTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.SharedModels.Enums;

namespace OpenVPNGateMonitor.Tests.Services.TelegramBot;

public class LocalizationServiceTests
{
    private readonly Mock<ILogger<LocalizationService>> _logger = new();
    private readonly Mock<ITelegramUserLanguagePreferenceQueryService> _userPrefQuery = new();
    private readonly Mock<ICommandService<TelegramUserLanguagePreference, int>> _userPrefCommand = new();
    private readonly Mock<ILocalizationTextQueryService> _textQuery = new();

    private readonly LocalizationService _service;

    public LocalizationServiceTests()
    {
        _service = new LocalizationService(
            _logger.Object,
            _userPrefQuery.Object,
            _userPrefCommand.Object,
            _textQuery.Object);
    }

    // -------------------------------------------------
    // SetTelegramUserLanguageAsync
    // -------------------------------------------------

    [Fact]
    public async Task SetTelegramUserLanguageAsync_WhenPreferenceDoesNotExist_AddsNew()
    {
        // Arrange
        var request = new TelegramUserLanguagePreference
        {
            TelegramId = 10,
            PreferredLanguage = Language.Greek
        };

        var stored = new TelegramUserLanguagePreference
        {
            Id = 1,
            TelegramId = 10,
            PreferredLanguage = Language.Greek
        };

        TelegramUserLanguagePreference? addedEntity = null;

        _userPrefQuery
            .SetupSequence(s => s.GetByTelegramId(request.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramUserLanguagePreference?)null) // first call
            .ReturnsAsync(stored);                               // second call

        _userPrefCommand
            .Setup(s => s.AddAsync(
                It.IsAny<TelegramUserLanguagePreference>(),
                true,
                It.IsAny<CancellationToken>()))
            .Callback<TelegramUserLanguagePreference, bool, CancellationToken>((e, _, _) =>
            {
                addedEntity = e;
            })
            .ReturnsAsync((TelegramUserLanguagePreference e, bool _, CancellationToken _) => e);

        // Act
        var result = await _service.SetTelegramUserLanguageAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(stored.TelegramId, result.TelegramId);
        Assert.Equal(stored.PreferredLanguage, result.PreferredLanguage);

        Assert.NotNull(addedEntity);
        Assert.Equal(request.TelegramId, addedEntity!.TelegramId);
        Assert.Equal(request.PreferredLanguage, addedEntity.PreferredLanguage);

        _userPrefQuery.Verify(
            s => s.GetByTelegramId(request.TelegramId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _userPrefCommand.Verify(
            s => s.AddAsync(It.IsAny<TelegramUserLanguagePreference>(), true, It.IsAny<CancellationToken>()),
            Times.Once);

        _userPrefCommand.Verify(
            s => s.UpdateAsync(It.IsAny<TelegramUserLanguagePreference>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SetTelegramUserLanguageAsync_WhenPreferenceExists_Updates()
    {
        // Arrange
        var request = new TelegramUserLanguagePreference
        {
            TelegramId = 20,
            PreferredLanguage = Language.Russian
        };

        var existing = new TelegramUserLanguagePreference
        {
            Id = 2,
            TelegramId = 20,
            PreferredLanguage = Language.English
        };

        TelegramUserLanguagePreference? updatedEntity = null;

        _userPrefQuery
            .SetupSequence(s => s.GetByTelegramId(request.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing) // first call
            .ReturnsAsync(existing); // second call

        _userPrefCommand
            .Setup(s => s.UpdateAsync(
                It.IsAny<TelegramUserLanguagePreference>(),
                true,
                It.IsAny<CancellationToken>()))
            .Callback<TelegramUserLanguagePreference, bool, CancellationToken>((e, _, _) =>
            {
                updatedEntity = e;
            })
            .ReturnsAsync(1);

        // Act
        var result = await _service.SetTelegramUserLanguageAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.TelegramId, result.TelegramId);
        Assert.Equal(request.PreferredLanguage, result.PreferredLanguage);

        Assert.NotNull(updatedEntity);
        Assert.Equal(request.TelegramId, updatedEntity!.TelegramId);
        Assert.Equal(request.PreferredLanguage, updatedEntity.PreferredLanguage);

        _userPrefQuery.Verify(
            s => s.GetByTelegramId(request.TelegramId, It.IsAny<CancellationToken>()),
            Times.Exactly(2));

        _userPrefCommand.Verify(
            s => s.UpdateAsync(It.IsAny<TelegramUserLanguagePreference>(), true, It.IsAny<CancellationToken>()),
            Times.Once);

        _userPrefCommand.Verify(
            s => s.AddAsync(It.IsAny<TelegramUserLanguagePreference>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SetTelegramUserLanguageAsync_WhenSecondReadReturnsNull_Throws()
    {
        // Arrange
        var request = new TelegramUserLanguagePreference
        {
            TelegramId = 30,
            PreferredLanguage = Language.English
        };

        _userPrefQuery
            .SetupSequence(s => s.GetByTelegramId(request.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramUserLanguagePreference?)null) // first
            .ReturnsAsync((TelegramUserLanguagePreference?)null); // second

        _userPrefCommand
            .Setup(s => s.AddAsync(
                It.IsAny<TelegramUserLanguagePreference>(),
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramUserLanguagePreference e, bool _, CancellationToken _) => e);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetTelegramUserLanguageAsync(request, CancellationToken.None));
    }

    // -------------------------------------------------
    // GetTelegramUserLanguageAsync
    // -------------------------------------------------

    [Fact]
    public async Task GetTelegramUserLanguageAsync_WhenPreferenceExists_ReturnsPreferred()
    {
        var pref = new TelegramUserLanguagePreference
        {
            TelegramId = 40,
            PreferredLanguage = Language.Russian
        };

        _userPrefQuery
            .Setup(s => s.GetByTelegramId(pref.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pref);

        var result = await _service.GetTelegramUserLanguageAsync(pref.TelegramId, CancellationToken.None);

        Assert.Equal(Language.Russian, result);
    }

    [Fact]
    public async Task GetTelegramUserLanguageAsync_WhenPreferenceMissing_ReturnsEnglish()
    {
        long telegramId = 50;

        _userPrefQuery
            .Setup(s => s.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramUserLanguagePreference?)null);

        var result = await _service.GetTelegramUserLanguageAsync(telegramId, CancellationToken.None);

        Assert.Equal(Language.English, result);
    }

    // -------------------------------------------------
    // IsExistTelegramUserLanguagePreferenceAsync
    // -------------------------------------------------

    [Fact]
    public async Task IsExistTelegramUserLanguagePreferenceAsync_WhenExists_ReturnsTrue()
    {
        long telegramId = 60;

        _userPrefQuery
            .Setup(s => s.AnyByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.IsExistTelegramUserLanguagePreferenceAsync(telegramId, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task IsExistTelegramUserLanguagePreferenceAsync_WhenNotExists_ReturnsFalse()
    {
        long telegramId = 70;

        _userPrefQuery
            .Setup(s => s.AnyByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.IsExistTelegramUserLanguagePreferenceAsync(telegramId, CancellationToken.None);

        Assert.False(result);
    }

    // -------------------------------------------------
    // GetTextForTelegramUser
    // -------------------------------------------------

    [Fact]
    public async Task GetTextForTelegramUser_WhenLanguagePassed_UsesItAndSkipsPreference()
    {
        const string key = "hello";
        long telegramId = 80;

        _textQuery
            .Setup(s => s.GetTextValueByKeyAndLanguageAsync(
                key,
                Language.Greek,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Γεια");

        var result = await _service.GetTextForTelegramUser(
            key,
            telegramId,
            CancellationToken.None,
            Language.Greek);

        Assert.Equal("Γεια", result);

        _userPrefQuery.Verify(
            s => s.GetByTelegramId(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTextForTelegramUser_WhenNoLanguageAndPreferenceExists_UsesPreference()
    {
        const string key = "bye";
        long telegramId = 90;

        var pref = new TelegramUserLanguagePreference
        {
            TelegramId = telegramId,
            PreferredLanguage = Language.Russian
        };

        _userPrefQuery
            .Setup(s => s.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pref);

        _textQuery
            .Setup(s => s.GetTextValueByKeyAndLanguageAsync(
                key,
                Language.Russian,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Пока");

        var result = await _service.GetTextForTelegramUser(
            key,
            telegramId,
            CancellationToken.None);

        Assert.Equal("Пока", result);
    }

    [Fact]
    public async Task GetTextForTelegramUser_WhenNoLanguageAndNoPreference_UsesEnglish()
    {
        const string key = "greeting";
        long telegramId = 100;

        _userPrefQuery
            .Setup(s => s.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramUserLanguagePreference?)null);

        _textQuery
            .Setup(s => s.GetTextValueByKeyAndLanguageAsync(
                key,
                Language.English,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Hello");

        var result = await _service.GetTextForTelegramUser(
            key,
            telegramId,
            CancellationToken.None);

        Assert.Equal("Hello", result);
    }

    [Fact]
    public async Task GetTextForTelegramUser_WhenTranslationMissing_ReturnsFallback()
    {
        const string key = "missing_key";
        long telegramId = 110;

        _userPrefQuery
            .Setup(s => s.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramUserLanguagePreference?)null);

        _textQuery
            .Setup(s => s.GetTextValueByKeyAndLanguageAsync(
                key,
                Language.English,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _service.GetTextForTelegramUser(
            key,
            telegramId,
            CancellationToken.None);

        Assert.Equal(
            "[Translation missing for key: missing_key, language: English]",
            result);
    }
}
