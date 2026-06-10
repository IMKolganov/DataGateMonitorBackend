using FluentAssertions;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers;
using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Requests;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Services.Api.Auth.Registers;

public class UserQuotaPlanServiceExtendedTests
{
    private readonly Mock<IUserQuotaPlanQueryService> _query = new(MockBehavior.Strict);
    private readonly Mock<ICommandService<UserQuotaPlan, int>> _command = new(MockBehavior.Strict);
    private readonly UserQuotaPlanService _sut;

    public UserQuotaPlanServiceExtendedTests()
    {
        _sut = new UserQuotaPlanService(_query.Object, _command.Object);
    }

    [Fact]
    public async Task AssignQuotaPlanAsync_ReturnsExisting_WhenAlreadyAssigned()
    {
        var existing = new UserQuotaPlan { Id = 5, UserId = 1, QuotaPlanId = 2 };
        _query.Setup(q => q.GetByUserIdAndQuotaPlanId(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.AssignQuotaPlanAsync(1, 2, CancellationToken.None);

        result.Should().BeSameAs(existing);
        _command.Verify(c => c.Add(It.IsAny<UserQuotaPlan>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AssignQuotaPlanAsync_CreatesNew_WhenMissing()
    {
        _query.Setup(q => q.GetByUserIdAndQuotaPlanId(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync((UserQuotaPlan?)null);
        _command.Setup(c => c.Add(It.IsAny<UserQuotaPlan>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserQuotaPlan p, bool _, CancellationToken _) => { p.Id = 9; return p; });

        var result = await _sut.AssignQuotaPlanAsync(1, 2, CancellationToken.None);

        result.Id.Should().Be(9);
        result.UserId.Should().Be(1);
        result.QuotaPlanId.Should().Be(2);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsMappedDtos()
    {
        var paged = new PagedResponse<UserQuotaPlan>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            Items = [new UserQuotaPlan { Id = 1, UserId = 3, QuotaPlanId = 2 }],
        };
        _query.Setup(q => q.GetPage(1, 20, null, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var result = await _sut.GetPageAsync(new GetAllUserQuotaPlansRequest { Page = 1, PageSize = 20 }, CancellationToken.None);

        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.UserId == 3);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenMissing()
    {
        _query.Setup(q => q.GetById(404, It.IsAny<CancellationToken>())).ReturnsAsync((UserQuotaPlan?)null);

        var result = await _sut.GetByIdAsync(404, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsResponse_WhenFound()
    {
        var entity = new UserQuotaPlan { Id = 1, UserId = 2, QuotaPlanId = 3 };
        _query.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(1, CancellationToken.None);

        result.Should().NotBeNull();
        result!.UserQuotaPlan.UserId.Should().Be(2);
    }

    [Fact]
    public async Task GetListByUserIdAsync_ReturnsMappedList()
    {
        _query.Setup(q => q.GetListByUserId(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new UserQuotaPlan { Id = 1, UserId = 5, QuotaPlanId = 1 }]);

        var result = await _sut.GetListByUserIdAsync(5, CancellationToken.None);

        result.Should().ContainSingle(x => x.UserId == 5);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEntity_WhenValid()
    {
        var entity = new UserQuotaPlan
        {
            Id = 1,
            UserId = 1,
            QuotaPlanId = 2,
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-1),
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow,
        };
        _query.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _command.Setup(c => c.Update(entity, true, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var from = DateTimeOffset.UtcNow;
        await _sut.UpdateAsync(new CreateOrUpdateUserQuotaPlanRequest
        {
            Id = 1,
            UserId = 1,
            QuotaPlanId = 3,
            EffectiveFrom = from,
            Note = "updated",
        }, CancellationToken.None);

        entity.QuotaPlanId.Should().Be(3);
        entity.Note.Should().Be("updated");
    }

    [Fact]
    public async Task UpdateAsync_Throws_WhenIdMissing()
    {
        var act = () => _sut.UpdateAsync(new CreateOrUpdateUserQuotaPlanRequest { Id = 0 }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Id is required*");
    }

    [Fact]
    public async Task DeleteAsync_Throws_WhenNotFound()
    {
        _command.Setup(c => c.DeleteById(404, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var act = () => _sut.DeleteAsync(404, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*404*");
    }

    [Fact]
    public async Task DeleteAsync_Succeeds_WhenFound()
    {
        _command.Setup(c => c.DeleteById(1, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.DeleteAsync(1, CancellationToken.None);

        _command.VerifyAll();
    }
}
