using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.DataBase.Services.Query.TelegramBotUserTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserIdentityLinkTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Users;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.User.Responses.Dto;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Users;

public class UserServiceTests
{
    private readonly Mock<IUserQueryService> _userQuery;
    private readonly Mock<ITelegramBotUserQueryService> _telegramBotUserQuery;
    private readonly Mock<IUserIdentityLinkQueryService> _userIdentityLinkQuery;
    private readonly Mock<IQuotaPlanQueryService> _quotaPlanQuery;
    private readonly Mock<ICommandService<User, int>> _userCommand;
    private readonly Mock<ICommandService<UserIdentityLink, int>> _userIdentityLinkCommand;
    private readonly Mock<ICommandService<UserQuotaPlan, int>> _userQuotaPlanCommand;
    private readonly Mock<ICommandService<TelegramBotUser, int>> _telegramBotUserCommand;
    private readonly Mock<ILogger<UserService>> _logger;
    private readonly UserService _sut;

    public UserServiceTests()
    {
        _userQuery = new Mock<IUserQueryService>(MockBehavior.Strict);
        _telegramBotUserQuery = new Mock<ITelegramBotUserQueryService>(MockBehavior.Strict);
        _userIdentityLinkQuery = new Mock<IUserIdentityLinkQueryService>(MockBehavior.Strict);
        _quotaPlanQuery = new Mock<IQuotaPlanQueryService>(MockBehavior.Strict);
        _userCommand = new Mock<ICommandService<User, int>>(MockBehavior.Strict);
        _userIdentityLinkCommand = new Mock<ICommandService<UserIdentityLink, int>>(MockBehavior.Strict);
        _userQuotaPlanCommand = new Mock<ICommandService<UserQuotaPlan, int>>(MockBehavior.Strict);
        _telegramBotUserCommand = new Mock<ICommandService<TelegramBotUser, int>>(MockBehavior.Strict);
        _logger = new Mock<ILogger<UserService>>(MockBehavior.Loose);

        _sut = new UserService(
            _userQuery.Object,
            _telegramBotUserQuery.Object,
            _userIdentityLinkQuery.Object,
            _quotaPlanQuery.Object,
            _userCommand.Object,
            _userIdentityLinkCommand.Object,
            _userQuotaPlanCommand.Object,
            _telegramBotUserCommand.Object,
            _logger.Object);
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ReturnsUsersResponse()
    {
        var request = new GetUserByIdRequest { Id = 42 };
        var user = new User
        {
            Id = 42,
            DisplayName = "Test User",
            Email = null,
            IsAdmin = false,
            IsBlocked = false,
            HasDashboardAccess = false,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var link = new UserIdentityLink
        {
            Id = 1,
            UserId = 42,
            Provider = "telegram",
            ExternalId = "12345",
            ProviderRowId = 10,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

        _userQuery.Setup(q => q.GetById(42, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userIdentityLinkQuery.Setup(q => q.GetByUserId(42, It.IsAny<CancellationToken>())).ReturnsAsync(link);

        var result = await _sut.GetUserById(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.User.Should().NotBeNull();
        result.User!.Id.Should().Be(42);
        result.User.DisplayName.Should().Be("Test User");
        result.User.Provider.Should().Be("telegram");
        result.User.ExternalId.Should().Be("12345");
        result.User.ProviderRowId.Should().Be(10);
        _userQuery.VerifyAll();
        _userIdentityLinkQuery.VerifyAll();
    }

    [Fact]
    public async Task GetUserById_WhenUserNotFound_ThrowsKeyNotFoundException()
    {
        var request = new GetUserByIdRequest { Id = 999 };
        _userQuery.Setup(q => q.GetById(999, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var act = () => _sut.GetUserById(request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*User 999 not found*");
        _userQuery.VerifyAll();
    }

    [Fact]
    public async Task GetAllUsers_ReturnsAllUsersAsDtos()
    {
        var users = new List<User>
        {
            new() { Id = 1, DisplayName = "U1", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow },
            new() { Id = 2, DisplayName = "U2", CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }
        };
        _userQuery.Setup(q => q.GetAll(It.IsAny<CancellationToken>())).ReturnsAsync(users);
        _userIdentityLinkQuery.Setup(q => q.GetByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserIdentityLink?)null);
        _userIdentityLinkQuery.Setup(q => q.GetByUserId(2, It.IsAny<CancellationToken>())).ReturnsAsync((UserIdentityLink?)null);

        var result = await _sut.GetAllUsers(CancellationToken.None);

        result.Should().NotBeNull();
        result.Users.Should().HaveCount(2);
        result.Users![0].Id.Should().Be(1);
        result.Users[0].DisplayName.Should().Be("U1");
        result.Users[1].Id.Should().Be(2);
        result.Users[1].DisplayName.Should().Be("U2");
        _userQuery.VerifyAll();
        _userIdentityLinkQuery.Verify(q => q.GetByUserId(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetUserByExternalId_WhenLinkExists_ReturnsUsersResponse()
    {
        var request = new GetUserByExternalIdRequest { ExternalId = "ext-99" };
        var link = new UserIdentityLink
        {
            Id = 1,
            UserId = 7,
            Provider = "google",
            ExternalId = "ext-99",
            ProviderRowId = null,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var user = new User
        {
            Id = 7,
            DisplayName = "Google User",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

        _userIdentityLinkQuery.Setup(q => q.GetByExternalId("ext-99", It.IsAny<CancellationToken>())).ReturnsAsync(link);
        _userQuery.Setup(q => q.GetById(7, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _userIdentityLinkQuery.Setup(q => q.GetByUserId(7, It.IsAny<CancellationToken>())).ReturnsAsync(link);

        var result = await _sut.GetUserByExternalId(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.User!.Id.Should().Be(7);
        result.User.DisplayName.Should().Be("Google User");
        result.User.Provider.Should().Be("google");
        result.User.ExternalId.Should().Be("ext-99");
        _userIdentityLinkQuery.VerifyAll();
        _userQuery.VerifyAll();
    }

    [Fact]
    public async Task GetUserByExternalId_WhenLinkNotFound_ThrowsKeyNotFoundException()
    {
        var request = new GetUserByExternalIdRequest { ExternalId = "missing" };
        _userIdentityLinkQuery.Setup(q => q.GetByExternalId("missing", It.IsAny<CancellationToken>())).ReturnsAsync((UserIdentityLink?)null);

        var act = () => _sut.GetUserByExternalId(request, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Identity link not found*externalId 'missing'*");
        _userIdentityLinkQuery.VerifyAll();
    }

    [Fact]
    public async Task RegisterUserFromTgBot_WhenTelegramUserAndLinkExist_UpdatesTelegramUserAndReturnsResponse()
    {
        var request = new RegisterUserFromTgBotRequest
        {
            TelegramId = 12345,
            Username = "updated_user",
            FirstName = "Up",
            LastName = "User"
        };
        var ct = CancellationToken.None;
        var existingTgUser = new TelegramBotUser
        {
            Id = 10,
            TelegramId = 12345,
            Username = "old",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var link = new UserIdentityLink
        {
            Id = 1,
            UserId = 5,
            Provider = "telegram",
            ExternalId = "12345",
            ProviderRowId = 10,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        var user = new User
        {
            Id = 5,
            DisplayName = "User",
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };

        _telegramBotUserQuery.Setup(q => q.GetByTelegramId(12345, ct)).ReturnsAsync(existingTgUser);
        _telegramBotUserCommand.Setup(c => c.Update(It.IsAny<TelegramBotUser>(), true, ct)).ReturnsAsync(1);
        _userIdentityLinkQuery.Setup(q => q.GetByProviderAndExternalId("telegram", "12345", ct)).ReturnsAsync(link);
        _userQuery.Setup(q => q.GetById(5, ct)).ReturnsAsync(user);
        _userIdentityLinkQuery.Setup(q => q.GetByUserId(5, ct)).ReturnsAsync(link);

        var result = await _sut.RegisterUserFromTgBot(request, ct);

        result.Should().NotBeNull();
        result.User!.Id.Should().Be(5);
        result.User.Provider.Should().Be("telegram");
        result.User.ExternalId.Should().Be("12345");
        result.User.ProviderRowId.Should().Be(10);
        _telegramBotUserCommand.Verify(c => c.Update(It.IsAny<TelegramBotUser>(), true, ct), Times.Once);
        _userCommand.Verify(c => c.Add(It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterUserFromTgBot_WhenNewTelegramUserAndNoLink_CreatesUserLinkAndAssignsDefaultQuota()
    {
        var request = new RegisterUserFromTgBotRequest
        {
            TelegramId = 99999,
            Username = "newbie",
            FirstName = "New",
            LastName = "User"
        };
        var ct = CancellationToken.None;
        var now = DateTimeOffset.UtcNow;
        var defaultPlan = new QuotaPlan
        {
            Id = 1,
            Name = "Default",
            CreateDate = now,
            LastUpdate = now
        };

        _telegramBotUserQuery.Setup(q => q.GetByTelegramId(99999, ct)).ReturnsAsync((TelegramBotUser?)null);
        _telegramBotUserCommand
            .Setup(c => c.Add(It.IsAny<TelegramBotUser>(), true, ct))
            .Returns((TelegramBotUser e, bool _, CancellationToken _) =>
                Task.FromResult(new TelegramBotUser { Id = 100, TelegramId = e.TelegramId, Username = e.Username, CreateDate = now, LastUpdate = now }));
        _userIdentityLinkQuery.Setup(q => q.GetByProviderAndExternalId("telegram", "99999", ct)).ReturnsAsync((UserIdentityLink?)null);
        _userCommand
            .Setup(c => c.Add(It.IsAny<User>(), true, ct))
            .Returns((User e, bool _, CancellationToken _) =>
                Task.FromResult(new User { Id = 50, DisplayName = e.DisplayName, CreateDate = e.CreateDate, LastUpdate = e.LastUpdate }));
        _userIdentityLinkCommand
            .Setup(c => c.Add(It.IsAny<UserIdentityLink>(), true, ct))
            .Returns((UserIdentityLink e, bool _, CancellationToken _) => Task.FromResult(e));
        _quotaPlanQuery.Setup(q => q.GetDefault(ct)).ReturnsAsync(defaultPlan);
        _userQuotaPlanCommand
            .Setup(c => c.Add(It.IsAny<UserQuotaPlan>(), true, ct))
            .Returns((UserQuotaPlan e, bool _, CancellationToken _) => Task.FromResult(e));
        _userIdentityLinkQuery.Setup(q => q.GetByUserId(50, ct)).ReturnsAsync((UserIdentityLink?)null);

        var result = await _sut.RegisterUserFromTgBot(request, ct);

        result.Should().NotBeNull();
        result.User!.DisplayName.Should().Be("newbie");
        result.User.Provider.Should().Be("telegram");
        result.User.ExternalId.Should().Be("99999");
        result.User.ProviderRowId.Should().Be(100);
        _telegramBotUserCommand.Verify(c => c.Add(It.IsAny<TelegramBotUser>(), true, ct), Times.Once);
        _userCommand.Verify(c => c.Add(It.IsAny<User>(), true, ct), Times.Once);
        _userIdentityLinkCommand.Verify(c => c.Add(It.IsAny<UserIdentityLink>(), true, ct), Times.Once);
        _userQuotaPlanCommand.Verify(c => c.Add(It.IsAny<UserQuotaPlan>(), true, ct), Times.Once);
    }

    [Fact]
    public async Task RegisterUserFromTgBot_WhenNewTelegramUserNoLinkNoDefaultQuota_StillCreatesUserAndLink()
    {
        var request = new RegisterUserFromTgBotRequest { TelegramId = 88888, Username = "minimal" };
        var ct = CancellationToken.None;

        _telegramBotUserQuery.Setup(q => q.GetByTelegramId(88888, ct)).ReturnsAsync((TelegramBotUser?)null);
        _telegramBotUserCommand
            .Setup(c => c.Add(It.IsAny<TelegramBotUser>(), true, ct))
            .Returns((TelegramBotUser e, bool _, CancellationToken _) =>
                Task.FromResult(new TelegramBotUser { Id = 200, TelegramId = e.TelegramId, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }));
        _userIdentityLinkQuery.Setup(q => q.GetByProviderAndExternalId("telegram", "88888", ct)).ReturnsAsync((UserIdentityLink?)null);
        _userCommand
            .Setup(c => c.Add(It.IsAny<User>(), true, ct))
            .Returns((User e, bool _, CancellationToken _) =>
                Task.FromResult(new User { Id = 60, DisplayName = e.DisplayName ?? "tg_88888", CreateDate = e.CreateDate, LastUpdate = e.LastUpdate }));
        _userIdentityLinkCommand
            .Setup(c => c.Add(It.IsAny<UserIdentityLink>(), true, ct))
            .Returns((UserIdentityLink e, bool _, CancellationToken _) => Task.FromResult(e));
        _quotaPlanQuery.Setup(q => q.GetDefault(ct)).ReturnsAsync((QuotaPlan?)null);
        _userIdentityLinkQuery.Setup(q => q.GetByUserId(60, ct)).ReturnsAsync((UserIdentityLink?)null);

        var result = await _sut.RegisterUserFromTgBot(request, ct);

        result.Should().NotBeNull();
        result.User!.DisplayName.Should().Be("minimal");
        result.User.ExternalId.Should().Be("88888");
        _userQuotaPlanCommand.Verify(c => c.Add(It.IsAny<UserQuotaPlan>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
