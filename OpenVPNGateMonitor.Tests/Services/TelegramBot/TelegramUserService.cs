using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.TelegramBot;

namespace OpenVPNGateMonitor.Tests.Services.TelegramBot;

public class TelegramUserServiceTests
{
    private readonly Mock<ILogger<TelegramUserService>> _logger = new();
    private readonly Mock<ITelegramBotUserQueryService> _userQuery = new();
    private readonly Mock<ICommandService<TelegramBotUser, int>> _userCommand = new();

    private readonly TelegramUserService _service;

    public TelegramUserServiceTests()
    {
        _service = new TelegramUserService(
            _logger.Object,
            _userQuery.Object,
            _userCommand.Object);
    }

    // -----------------------------------------------------------------------
    // RegisterUserAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task RegisterUserAsync_WhenUserDoesNotExist_AddsAndReturnsNewUser()
    {
        var request = new TelegramBotUser
        {
            TelegramId = 1001,
            Username = "new_user",
            FirstName = "New",
            LastName = "User"
        };

        TelegramBotUser? addedEntity = null;

        _userQuery
            .Setup(q => q.GetByTelegramId(request.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        _userCommand
            .Setup(c => c.Add(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()))
            .Callback<TelegramBotUser, bool, CancellationToken>((entity, _, _) =>
            {
                addedEntity = entity;
            })
            .ReturnsAsync((TelegramBotUser entity, bool _, CancellationToken _) => entity);

        var result = await _service.RegisterUserAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(request.TelegramId, result.TelegramId);
        Assert.Equal(request.Username, result.Username);
        Assert.NotEqual(default, result.CreateDate);
        Assert.NotEqual(default, result.LastUpdate);

        Assert.NotNull(addedEntity);
        Assert.Equal(request.TelegramId, addedEntity!.TelegramId);

        _userQuery.Verify(
            q => q.GetByTelegramId(request.TelegramId, It.IsAny<CancellationToken>()),
            Times.Once);

        _userCommand.Verify(
            c => c.Add(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Once);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RegisterUserAsync_WhenUserExists_UpdatesAndReturnsUser()
    {
        var request = new TelegramBotUser
        {
            TelegramId = 2002,
            Username = "updated_username",
            FirstName = "Updated",
            LastName = "User"
        };

        var existing = new TelegramBotUser
        {
            Id = 10,
            TelegramId = 2002,
            Username = "old_username",
            FirstName = "Old",
            LastName = "Name",
            CreateDate = DateTimeOffset.UtcNow.AddDays(-1),
            LastUpdate = DateTimeOffset.UtcNow.AddDays(-1)
        };

        TelegramBotUser? updatedEntity = null;

        _userQuery
            .Setup(q => q.GetByTelegramId(request.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        _userCommand
            .Setup(c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()))
            .Callback<TelegramBotUser, bool, CancellationToken>((entity, _, _) =>
            {
                updatedEntity = entity;
            })
            .ReturnsAsync(1);

        var result = await _service.RegisterUserAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Same(existing, result);
        Assert.Equal(request.Username, result.Username);
        Assert.Equal(request.FirstName, result.FirstName);
        Assert.Equal(request.LastName, result.LastName);
        Assert.Equal(existing.CreateDate, result.CreateDate); // not changed
        Assert.NotEqual(existing.CreateDate, result.LastUpdate); // updated

        Assert.NotNull(updatedEntity);
        Assert.Equal(request.Username, updatedEntity!.Username);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Once);

        _userCommand.Verify(
            c => c.Add(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -----------------------------------------------------------------------
    // GetUserAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUserAsync_WhenUserExists_ReturnsUser()
    {
        var user = new TelegramBotUser { TelegramId = 3003, Username = "exists" };

        _userQuery
            .Setup(q => q.GetByTelegramId(user.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.GetUserAsync(user.TelegramId, CancellationToken.None);

        Assert.Same(user, result);
    }

    [Fact]
    public async Task GetUserAsync_WhenUserMissing_ThrowsInvalidOperationException()
    {
        long telegramId = 4004;

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetUserAsync(telegramId, CancellationToken.None));
    }

    // -----------------------------------------------------------------------
    // GetAdminsAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAdminsAsync_WhenAdminsExist_ReturnsList()
    {
        var admins = new List<TelegramBotUser>
        {
            new TelegramBotUser { Id = 1, TelegramId = 1, IsAdmin = true },
            new TelegramBotUser { Id = 2, TelegramId = 2, IsAdmin = true }
        };

        _userQuery
            .Setup(q => q.GetAllAdmins(It.IsAny<CancellationToken>()))
            .ReturnsAsync(admins);

        var result = await _service.GetAdminsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
    }

    [Fact]
    public async Task GetAdminsAsync_WhenListEmpty_ReturnsEmptyList()
    {
        _userQuery
            .Setup(q => q.GetAllAdmins(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TelegramBotUser>());

        var result = await _service.GetAdminsAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result!);
    }

    // -----------------------------------------------------------------------
    // GetAllUsersAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetAllUsersAsync_ReturnsUsersOrderedById()
    {
        var users = new List<TelegramBotUser>
        {
            new TelegramBotUser { Id = 2, TelegramId = 2 },
            new TelegramBotUser { Id = 1, TelegramId = 1 }
        };

        _userQuery
            .Setup(q => q.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);

        var result = await _service.GetAllUsersAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new[] { 1, 2 }, result!.Select(u => u.Id).ToArray());
    }

    // -----------------------------------------------------------------------
    // GetUserByTelegramIdAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task GetUserByTelegramIdAsync_ReturnsUserFromQuery()
    {
        var user = new TelegramBotUser { Id = 10, TelegramId = 5555 };

        _userQuery
            .Setup(q => q.GetByTelegramId(user.TelegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.GetUserByTelegramIdAsync(user.TelegramId, CancellationToken.None);

        Assert.Same(user, result);
    }

    [Fact]
    public async Task GetUserByTelegramIdAsync_WhenMissing_ReturnsNull()
    {
        long telegramId = 9999;

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        var result = await _service.GetUserByTelegramIdAsync(telegramId, CancellationToken.None);

        Assert.Null(result);
    }

    // -----------------------------------------------------------------------
    // BlockUserAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task BlockUserAsync_WhenUserMissing_ReturnsFalse()
    {
        long telegramId = 6006;

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        var result = await _service.BlockUserAsync(telegramId, CancellationToken.None);

        Assert.False(result);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BlockUserAsync_WhenAlreadyBlocked_ReturnsTrueWithoutUpdate()
    {
        long telegramId = 7007;
        var user = new TelegramBotUser { TelegramId = telegramId, IsBlocked = true };

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.BlockUserAsync(telegramId, CancellationToken.None);

        Assert.True(result);
        Assert.True(user.IsBlocked);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BlockUserAsync_WhenActive_BlocksAndUpdates()
    {
        long telegramId = 8008;
        var user = new TelegramBotUser { TelegramId = telegramId, IsBlocked = false };

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userCommand
            .Setup(c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.BlockUserAsync(telegramId, CancellationToken.None);

        Assert.True(result);
        Assert.True(user.IsBlocked);

        _userCommand.Verify(
            c => c.Update(It.Is<TelegramBotUser>(u => u.IsBlocked), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // UnblockUserAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UnblockUserAsync_WhenUserMissing_ReturnsFalse()
    {
        long telegramId = 9009;

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        var result = await _service.UnblockUserAsync(telegramId, CancellationToken.None);

        Assert.False(result);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnblockUserAsync_WhenNotBlocked_ReturnsTrueWithoutUpdate()
    {
        long telegramId = 10010;
        var user = new TelegramBotUser { TelegramId = telegramId, IsBlocked = false };

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.UnblockUserAsync(telegramId, CancellationToken.None);

        Assert.True(result);
        Assert.False(user.IsBlocked);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnblockUserAsync_WhenBlocked_UnblocksAndUpdates()
    {
        long telegramId = 11011;
        var user = new TelegramBotUser { TelegramId = telegramId, IsBlocked = true };

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userCommand
            .Setup(c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.UnblockUserAsync(telegramId, CancellationToken.None);

        Assert.True(result);
        Assert.False(user.IsBlocked);

        _userCommand.Verify(
            c => c.Update(It.Is<TelegramBotUser>(u => !u.IsBlocked), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // SetAdminAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task SetAdminAsync_WhenUserMissing_ReturnsFalse()
    {
        long telegramId = 12012;

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        var result = await _service.SetAdminAsync(telegramId, CancellationToken.None);

        Assert.False(result);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SetAdminAsync_WhenAlreadyAdmin_ReturnsTrueWithoutUpdate()
    {
        long telegramId = 13013;
        var user = new TelegramBotUser { TelegramId = telegramId, IsAdmin = true };

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.SetAdminAsync(telegramId, CancellationToken.None);

        Assert.True(result);
        Assert.True(user.IsAdmin);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SetAdminAsync_WhenNotAdmin_SetsAdminAndUpdates()
    {
        long telegramId = 14014;
        var user = new TelegramBotUser { TelegramId = telegramId, IsAdmin = false };

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userCommand
            .Setup(c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.SetAdminAsync(telegramId, CancellationToken.None);

        Assert.True(result);
        Assert.True(user.IsAdmin);

        _userCommand.Verify(
            c => c.Update(It.Is<TelegramBotUser>(u => u.IsAdmin), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -----------------------------------------------------------------------
    // UnsetAdminAsync
    // -----------------------------------------------------------------------

    [Fact]
    public async Task UnsetAdminAsync_WhenUserMissing_ReturnsFalse()
    {
        long telegramId = 15015;

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TelegramBotUser?)null);

        var result = await _service.UnsetAdminAsync(telegramId, CancellationToken.None);

        Assert.False(result);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnsetAdminAsync_WhenNotAdmin_ReturnsTrueWithoutUpdate()
    {
        long telegramId = 16016;
        var user = new TelegramBotUser { TelegramId = telegramId, IsAdmin = false };

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.UnsetAdminAsync(telegramId, CancellationToken.None);

        Assert.True(result);
        Assert.False(user.IsAdmin);

        _userCommand.Verify(
            c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UnsetAdminAsync_WhenAdmin_UnsetsAndUpdates()
    {
        long telegramId = 17017;
        var user = new TelegramBotUser { TelegramId = telegramId, IsAdmin = true };

        _userQuery
            .Setup(q => q.GetByTelegramId(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userCommand
            .Setup(c => c.Update(It.IsAny<TelegramBotUser>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var result = await _service.UnsetAdminAsync(telegramId, CancellationToken.None);

        Assert.True(result);
        Assert.False(user.IsAdmin);

        _userCommand.Verify(
            c => c.Update(It.Is<TelegramBotUser>(u => !u.IsAdmin), true, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
