using FluentAssertions;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Requests;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.Api.Auth.Registers;

public class UserQuotaPlanServiceTests
{
    private readonly Mock<IUserQuotaPlanQueryService> _query;
    private readonly Mock<ICommandService<UserQuotaPlan, int>> _command;
    private readonly UserQuotaPlanService _sut;

    public UserQuotaPlanServiceTests()
    {
        _query = new Mock<IUserQuotaPlanQueryService>(MockBehavior.Strict);
        _command = new Mock<ICommandService<UserQuotaPlan, int>>(MockBehavior.Strict);
        _sut = new UserQuotaPlanService(_query.Object, _command.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenNonUtcDates_NormalizesToUtcBeforeAdd()
    {
        _query.Setup(q => q.GetActiveByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserQuotaPlan?)null);

        // Client sends local time (e.g. +02:00); Npgsql accepts only UTC (offset 0).
        var localFrom = new DateTimeOffset(2025, 2, 15, 10, 0, 0, TimeSpan.FromHours(2));
        var localTo = new DateTimeOffset(2025, 3, 1, 12, 0, 0, TimeSpan.FromHours(2));

        UserQuotaPlan? captured = null;
        _command
            .Setup(c => c.Add(It.IsAny<UserQuotaPlan>(), true, It.IsAny<CancellationToken>()))
            .Callback<UserQuotaPlan, bool, CancellationToken>((e, _, _) => captured = e)
            .ReturnsAsync((UserQuotaPlan e, bool _, CancellationToken _) => e);

        var request = new CreateOrUpdateUserQuotaPlanRequest
        {
            UserId = 1,
            QuotaPlanId = 2,
            EffectiveFrom = localFrom,
            EffectiveTo = localTo
        };

        await _sut.CreateAsync(request, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.EffectiveFrom.Offset.Should().Be(TimeSpan.Zero, "PostgreSQL timestamptz requires UTC");
        captured.EffectiveTo.Should().NotBeNull();
        captured.EffectiveTo!.Value.Offset.Should().Be(TimeSpan.Zero, "PostgreSQL timestamptz requires UTC");
        captured.EffectiveFrom.UtcDateTime.Should().Be(localFrom.UtcDateTime);
        captured.EffectiveTo.Value.UtcDateTime.Should().Be(localTo.UtcDateTime);
        _command.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_WhenEffectiveToBeforeEffectiveFrom_Throws()
    {
        _query.Setup(q => q.GetActiveByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync((UserQuotaPlan?)null);

        var from = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero);

        var request = new CreateOrUpdateUserQuotaPlanRequest
        {
            UserId = 1,
            QuotaPlanId = 2,
            EffectiveFrom = from,
            EffectiveTo = to
        };

        var act = () => _sut.CreateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*EffectiveTo*EffectiveFrom*");
        _command.Verify(c => c.Add(It.IsAny<UserQuotaPlan>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WhenUserAlreadyHasActivePlan_Throws()
    {
        var active = new UserQuotaPlan
        {
            Id = 10,
            UserId = 1,
            QuotaPlanId = 2,
            EffectiveFrom = DateTimeOffset.UtcNow,
            EffectiveTo = null,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        _query.Setup(q => q.GetActiveByUserId(1, It.IsAny<CancellationToken>())).ReturnsAsync(active);

        var request = new CreateOrUpdateUserQuotaPlanRequest
        {
            UserId = 1,
            QuotaPlanId = 3,
            EffectiveFrom = DateTimeOffset.UtcNow,
            EffectiveTo = null
        };

        var act = () => _sut.CreateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already has an active quota plan*");
        _command.Verify(c => c.Add(It.IsAny<UserQuotaPlan>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenEffectiveToBeforeEffectiveFrom_Throws()
    {
        var existing = new UserQuotaPlan
        {
            Id = 1,
            UserId = 1,
            QuotaPlanId = 2,
            EffectiveFrom = DateTimeOffset.UtcNow,
            EffectiveTo = null,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        _query.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var from = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero);
        var request = new CreateOrUpdateUserQuotaPlanRequest
        {
            Id = 1,
            UserId = 1,
            QuotaPlanId = 2,
            EffectiveFrom = from,
            EffectiveTo = to
        };

        var act = () => _sut.UpdateAsync(request, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*EffectiveTo*EffectiveFrom*");
        _command.Verify(c => c.Update(It.IsAny<UserQuotaPlan>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
