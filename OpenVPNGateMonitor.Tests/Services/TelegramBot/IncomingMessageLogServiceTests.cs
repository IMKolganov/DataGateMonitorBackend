using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.IncomingMessageLogTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.TelegramBotIncomingMessageLog.Requests;

namespace OpenVPNGateMonitor.Tests.Services.TelegramBot;

public class IncomingMessageLogServiceTests
{
    private readonly Mock<ILogger<IncomingMessageLogService>> _logger = new();
    private readonly Mock<ICommandService<IncomingMessageLog, int>> _command = new();
    private readonly Mock<IIncomingMessageLogQueryService> _query = new();
    private readonly IncomingMessageLogService _service;

    public IncomingMessageLogServiceTests()
    {
        _service = new IncomingMessageLogService(
            _logger.Object,
            _command.Object,
            _query.Object
        );
    }

    // ==================================================
    // SaveMessageAsync
    // ==================================================

    [Fact]
    public async Task SaveMessageAsync_Saves_Entity_And_Returns_Mapped_Dto()
    {
        // Arrange
        var dto = new MessageDto
        {
            TelegramId = 123456,
            Username = "testuser",
            FirstName = "Test",
            LastName = "User",
            MessageText = "hello",
            FileType = "image",
            FileId = "file-id",
            FileName = "photo.png",
            FileSize = 42,
            FilePath = "/tmp/photo.png",
            ReceivedAt = DateTimeOffset.UtcNow,
            CreateDate = DateTimeOffset.UtcNow.AddMinutes(-1),
            LastUpdate = DateTimeOffset.UtcNow
        };

        var request = new AddMessageRequest
        {
            Message = dto
        };

        IncomingMessageLog? savedEntity = null;

        _command
            .Setup(c => c.AddAsync(
                It.IsAny<IncomingMessageLog>(),
                true,
                It.IsAny<CancellationToken>()))
            .Callback<IncomingMessageLog, bool, CancellationToken>((entity, _, _) =>
            {
                savedEntity = entity;
            })
            .ReturnsAsync((IncomingMessageLog entity, bool _, CancellationToken _) => entity);
        // Act
        var response = await _service.SaveMessageAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.Message);

        _command.Verify(
            c => c.AddAsync(It.IsAny<IncomingMessageLog>(), true, It.IsAny<CancellationToken>()),
            Times.Once);

        // ----- Entity mapping check -----
        Assert.NotNull(savedEntity);

        Assert.Equal(dto.TelegramId, savedEntity!.TelegramId);
        Assert.Equal(dto.Username, savedEntity.Username);
        Assert.Equal(dto.FirstName, savedEntity.FirstName);
        Assert.Equal(dto.LastName, savedEntity.LastName);
        Assert.Equal(dto.MessageText, savedEntity.MessageText);
        Assert.Equal(dto.FileType, savedEntity.FileType);
        Assert.Equal(dto.FileId, savedEntity.FileId);
        Assert.Equal(dto.FileName, savedEntity.FileName);
        Assert.Equal(dto.FileSize, savedEntity.FileSize);
        Assert.Equal(dto.FilePath, savedEntity.FilePath);

        // ----- Returned DTO mapping check -----
        var result = response.Message;

        Assert.Equal(dto.TelegramId, result.TelegramId);
        Assert.Equal(dto.Username, result.Username);
        Assert.Equal(dto.FirstName, result.FirstName);
        Assert.Equal(dto.LastName, result.LastName);
        Assert.Equal(dto.MessageText, result.MessageText);
        Assert.Equal(dto.FileType, result.FileType);
        Assert.Equal(dto.FileId, result.FileId);
        Assert.Equal(dto.FileName, result.FileName);
        Assert.Equal(dto.FileSize, result.FileSize);
        Assert.Equal(dto.FilePath, result.FilePath);
    }

    // ==================================================
    // GetByIdAsync (success path)
    // ==================================================

    [Fact]
    public async Task GetByIdAsync_WhenFound_Returns_Mapped_Dto()
    {
        // Arrange
        var entity = new IncomingMessageLog
        {
            Id = 55,
            TelegramId = 777,
            Username = "fetch",
            FirstName = "Test",
            LastName = "User",
            MessageText = "stored",
            FileType = "txt",
            FileId = "f",
            FileName = "file.txt",
            FileSize = 20,
            FilePath = "/data/file.txt",
            ReceivedAt = DateTimeOffset.UtcNow,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

        _query
            .Setup(q => q.GetByIdAsync(55, It.IsAny<CancellationToken>()))
            .ReturnsAsync(entity);

        // Act
        var result = await _service.GetByIdAsync(55, CancellationToken.None);

        // Assert
        Assert.NotNull(result);

        Assert.Equal(entity.Id, result!.Id);
        Assert.Equal(entity.TelegramId, result.TelegramId);
        Assert.Equal(entity.Username, result.Username);
        Assert.Equal(entity.FirstName, result.FirstName);
        Assert.Equal(entity.LastName, result.LastName);
        Assert.Equal(entity.MessageText, result.MessageText);
        Assert.Equal(entity.FileType, result.FileType);
        Assert.Equal(entity.FileId, result.FileId);
        Assert.Equal(entity.FileName, result.FileName);
        Assert.Equal(entity.FileSize, result.FileSize);
        Assert.Equal(entity.FilePath, result.FilePath);

        _query.Verify(
            q => q.GetByIdAsync(55, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ==================================================
    // GetByIdAsync (null path)
    // ==================================================

    [Fact]
    public async Task GetByIdAsync_When_NotFound_Returns_Null()
    {
        // Arrange
        _query
            .Setup(q => q.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingMessageLog?)null);

        // Act
        var result = await _service.GetByIdAsync(99, CancellationToken.None);

        // Assert
        Assert.Null(result);

        _query.Verify(
            q => q.GetByIdAsync(99, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
