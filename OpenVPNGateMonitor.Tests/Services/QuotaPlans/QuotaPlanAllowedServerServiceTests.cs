using FluentAssertions;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.QuotaPlans;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Requests;
using OpenVPNGateMonitor.SharedModels.Responses;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.QuotaPlans;

public class QuotaPlanAllowedServerServiceTests
{
    private readonly Mock<IQuotaPlanAllowedServerQueryService> _query;
    private readonly Mock<ICommandService<QuotaPlanAllowedServer, int>> _command;
    private readonly QuotaPlanAllowedServerService _sut;

    public QuotaPlanAllowedServerServiceTests()
    {
        _query = new Mock<IQuotaPlanAllowedServerQueryService>(MockBehavior.Strict);
        _command = new Mock<ICommandService<QuotaPlanAllowedServer, int>>(MockBehavior.Strict);
        _sut = new QuotaPlanAllowedServerService(_query.Object, _command.Object);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsPagedResult()
    {
        var entities = new List<QuotaPlanAllowedServer>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }
        };
        var paged = new PagedResponse<QuotaPlanAllowedServer>
        {
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            Items = entities
        };

        _query.Setup(q => q.GetPage(1, 20, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var result = await _sut.GetPageAsync(new GetAllQuotaPlanAllowedServersRequest { Page = 1, PageSize = 20 }, CancellationToken.None);

        result.Should().NotBeNull();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Id.Should().Be(1);
        result.Items[0].QuotaPlanId.Should().Be(10);
        result.Items[0].VpnServerId.Should().Be(5);
        _query.VerifyAll();
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsResponse()
    {
        var entity = new QuotaPlanAllowedServer
        {
            Id = 1,
            QuotaPlanId = 10,
            VpnServerId = 5,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        _query.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);

        var result = await _sut.GetByIdAsync(1, CancellationToken.None);

        result.Should().NotBeNull();
        result!.QuotaPlanAllowedServer.Id.Should().Be(1);
        result.QuotaPlanAllowedServer.QuotaPlanId.Should().Be(10);
        result.QuotaPlanAllowedServer.VpnServerId.Should().Be(5);
        _query.VerifyAll();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotFound_ReturnsNull()
    {
        _query.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((QuotaPlanAllowedServer?)null);

        var result = await _sut.GetByIdAsync(99, CancellationToken.None);

        result.Should().BeNull();
        _query.VerifyAll();
    }

    [Fact]
    public async Task GetListByQuotaPlanIdAsync_ReturnsList()
    {
        var list = new List<QuotaPlanAllowedServer>
        {
            new() { Id = 1, QuotaPlanId = 10, VpnServerId = 5, CreateDate = DateTimeOffset.UtcNow, LastUpdate = DateTimeOffset.UtcNow }
        };
        _query.Setup(q => q.GetListByQuotaPlanId(10, It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var result = await _sut.GetListByQuotaPlanIdAsync(10, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].QuotaPlanId.Should().Be(10);
        _query.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_WhenNotExists_AddsAndReturns()
    {
        _query.Setup(q => q.GetByQuotaPlanIdAndServerId(10, 5, It.IsAny<CancellationToken>())).ReturnsAsync((QuotaPlanAllowedServer?)null);

        var added = new QuotaPlanAllowedServer
        {
            Id = 1,
            QuotaPlanId = 10,
            VpnServerId = 5,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        _command.Setup(c => c.Add(It.Is<QuotaPlanAllowedServer>(e => e.QuotaPlanId == 10 && e.VpnServerId == 5), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(added);

        var result = await _sut.CreateAsync(
            new CreateOrUpdateQuotaPlanAllowedServerRequest { QuotaPlanId = 10, VpnServerId = 5 },
            CancellationToken.None);

        result.Should().NotBeNull();
        result.QuotaPlanAllowedServer.Id.Should().Be(1);
        result.QuotaPlanAllowedServer.QuotaPlanId.Should().Be(10);
        result.QuotaPlanAllowedServer.VpnServerId.Should().Be(5);
        _query.VerifyAll();
        _command.VerifyAll();
    }

    [Fact]
    public async Task CreateAsync_WhenAlreadyExists_ReturnsExisting()
    {
        var existing = new QuotaPlanAllowedServer
        {
            Id = 1,
            QuotaPlanId = 10,
            VpnServerId = 5,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        _query.Setup(q => q.GetByQuotaPlanIdAndServerId(10, 5, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _sut.CreateAsync(
            new CreateOrUpdateQuotaPlanAllowedServerRequest { QuotaPlanId = 10, VpnServerId = 5 },
            CancellationToken.None);

        result.Should().NotBeNull();
        result.QuotaPlanAllowedServer.Id.Should().Be(1);
        _command.Verify(c => c.Add(It.IsAny<QuotaPlanAllowedServer>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_Updates()
    {
        var entity = new QuotaPlanAllowedServer
        {
            Id = 1,
            QuotaPlanId = 10,
            VpnServerId = 5,
            CreateDate = DateTimeOffset.UtcNow,
            LastUpdate = DateTimeOffset.UtcNow
        };
        _query.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(entity);
        _query.Setup(q => q.GetByQuotaPlanIdAndServerId(10, 6, It.IsAny<CancellationToken>())).ReturnsAsync((QuotaPlanAllowedServer?)null);
        _command.Setup(c => c.Update(It.IsAny<QuotaPlanAllowedServer>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.UpdateAsync(
            new CreateOrUpdateQuotaPlanAllowedServerRequest { Id = 1, QuotaPlanId = 10, VpnServerId = 6 },
            CancellationToken.None);

        _command.Verify(c => c.Update(It.Is<QuotaPlanAllowedServer>(e => e.VpnServerId == 6), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WhenNotFound_Throws()
    {
        _query.Setup(q => q.GetById(99, It.IsAny<CancellationToken>())).ReturnsAsync((QuotaPlanAllowedServer?)null);

        var act = () => _sut.UpdateAsync(
            new CreateOrUpdateQuotaPlanAllowedServerRequest { Id = 99, QuotaPlanId = 10, VpnServerId = 5 },
            CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*99*");
    }

    [Fact]
    public async Task DeleteAsync_WhenFound_Deletes()
    {
        _command.Setup(c => c.DeleteById(1, true, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await _sut.DeleteAsync(1, CancellationToken.None);

        _command.Verify(c => c.DeleteById(1, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenNotFound_Throws()
    {
        _command.Setup(c => c.DeleteById(99, true, It.IsAny<CancellationToken>())).ReturnsAsync(0);

        var act = () => _sut.DeleteAsync(99, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("*99*");
    }
}
