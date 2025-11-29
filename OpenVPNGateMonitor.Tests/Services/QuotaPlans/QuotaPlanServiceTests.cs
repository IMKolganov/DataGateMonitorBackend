using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Logging;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.QuotaPlans;

namespace OpenVPNGateMonitor.Tests.Services.QuotaPlans;

public class QuotaPlanServiceTests
{
    private readonly Mock<ILogger<QuotaPlanService>> _logger = new();
    private readonly Mock<ICommandService<QuotaPlan, int>> _command = new();
    private readonly Mock<IQuotaPlanQueryService> _query = new();

    private QuotaPlanService CreateService() => new(_logger.Object, _command.Object, _query.Object);

    [Fact]
    public async Task GetAll_DelegatesToQuery()
    {
        var plans = new List<QuotaPlan> { new() { Id = 1, Name = "A" } };
        _query.Setup(q => q.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(plans);

        var svc = CreateService();
        var result = await svc.GetAllAsync();

        Assert.Same(plans, result);
        _query.Verify(q => q.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetById_DelegatesToQuery()
    {
        var plan = new QuotaPlan { Id = 7, Name = "P" };
        _query.Setup(q => q.GetByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

        var svc = CreateService();
        var result = await svc.GetByIdAsync(7);

        Assert.Same(plan, result);
        _query.Verify(q => q.GetByIdAsync(7, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPage_DelegatesToQuery()
    {
        // We don't need a concrete IPagedResult implementation here; just ensure delegation happens.
        var paged = Mock.Of<SharedModels.Responses.IPagedResult<QuotaPlan>>();
        _query.Setup(q => q.GetPageAsync(2, 5, It.IsAny<CancellationToken>())).ReturnsAsync(paged);

        var svc = CreateService();
        var result = await svc.GetPageAsync(2, 5);

        Assert.Same(paged, result);
        _query.Verify(q => q.GetPageAsync(2, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetDefault_ReturnsFirstWithIsDefault()
    {
        var list = new List<QuotaPlan>
        {
            new() { Id = 1, Name = "A", IsDefault = false },
            new() { Id = 2, Name = "B", IsDefault = true },
            new() { Id = 3, Name = "C", IsDefault = true },
        };
        _query.Setup(q => q.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(list);

        var svc = CreateService();
        var result = await svc.GetDefaultAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result!.Id);
    }

    [Fact]
    public async Task GetDefault_ReturnsNullWhenNone()
    {
        _query.Setup(q => q.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<QuotaPlan>());
        var svc = CreateService();
        var result = await svc.GetDefaultAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task Create_SetsIdToZero_AndCallsAdd()
    {
        var input = new QuotaPlan { Id = 123, Name = "Plan" };
        var created = new QuotaPlan { Id = 10, Name = "Plan" };
        _command.Setup(c => c.AddAsync(It.IsAny<QuotaPlan>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        var svc = CreateService();
        var result = await svc.CreateAsync(input);

        Assert.Same(created, result);
        _command.Verify(c => c.AddAsync(It.Is<QuotaPlan>(p => p.Id == 0 && p.Name == "Plan"), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_WhenMakeDefault_UnsetsAllDefaults_AndSetsFlag()
    {
        int unsetCalls = 0;
        _command
            .Setup(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlan, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>, Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>>>>(), It.IsAny<CancellationToken>()))
            .Callback(() => unsetCalls++)
            .ReturnsAsync(1);

        _command.Setup(c => c.AddAsync(It.IsAny<QuotaPlan>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuotaPlan p, bool _, CancellationToken _) => new QuotaPlan
            {
                Id = 42,
                Name = p.Name,
                Description = p.Description,
                DailyQuotaBytes = p.DailyQuotaBytes,
                MonthlyQuotaBytes = p.MonthlyQuotaBytes,
                UpKbps = p.UpKbps,
                DownKbps = p.DownKbps,
                OverlimitAction = p.OverlimitAction,
                ThrottleUpKbps = p.ThrottleUpKbps,
                ThrottleDownKbps = p.ThrottleDownKbps,
                IsActive = p.IsActive,
                IsDefault = true
            });

        var input = new QuotaPlan { Name = "D", IsDefault = false };
        var svc = CreateService();
        var result = await svc.CreateAsync(input, makeDefault: true);

        Assert.True(result.IsDefault);
        Assert.Equal(42, result.Id);
        Assert.Equal(1, unsetCalls);
    }

    [Fact]
    public async Task Update_Throws_WhenIdInvalid()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ValidationException>(() => svc.UpdateAsync(new QuotaPlan { Id = 0, Name = "N" }));
    }

    [Fact]
    public async Task Update_CallsUpdate_AndUnsetsDefaults_WhenIsDefaultTrue()
    {
        int unsetCalls = 0;
        _command
            .Setup(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlan, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>, Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>>>>(), It.IsAny<CancellationToken>()))
            .Callback(() => unsetCalls++)
            .ReturnsAsync(3);

        _command.Setup(c => c.UpdateAsync(It.IsAny<QuotaPlan>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var svc = CreateService();
        var affected = await svc.UpdateAsync(new QuotaPlan { Id = 5, Name = "N", IsDefault = true });

        Assert.Equal(1, affected);
        Assert.Equal(1, unsetCalls);
        _command.Verify(c => c.UpdateAsync(It.Is<QuotaPlan>(p => p.Id == 5 && p.IsDefault), true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activate_CallsUpdateWhere_WithIdPredicate()
    {
        System.Linq.Expressions.Expression<Func<QuotaPlan, bool>>? capturedPredicate = null;
        _command
            .Setup(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlan, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>, Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>>>>(), It.IsAny<CancellationToken>()))
            .Callback((System.Linq.Expressions.Expression<Func<QuotaPlan, bool>> pred, object _, CancellationToken __) => capturedPredicate = pred)
            .ReturnsAsync(1);

        var svc = CreateService();
        var rows = await svc.ActivateAsync(9);

        Assert.Equal(1, rows);
        Assert.NotNull(capturedPredicate);
        var compiled = capturedPredicate!.Compile();
        Assert.True(compiled(new QuotaPlan { Id = 9 }));
        Assert.False(compiled(new QuotaPlan { Id = 8 }));
    }

    [Fact]
    public async Task Deactivate_CallsUpdateWhere_WithIdPredicate()
    {
        System.Linq.Expressions.Expression<Func<QuotaPlan, bool>>? capturedPredicate = null;
        _command
            .Setup(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlan, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>, Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>>>>(), It.IsAny<CancellationToken>()))
            .Callback((System.Linq.Expressions.Expression<Func<QuotaPlan, bool>> pred, object _, CancellationToken __) => capturedPredicate = pred)
            .ReturnsAsync(2);

        var svc = CreateService();
        var rows = await svc.DeactivateAsync(3);

        Assert.Equal(2, rows);
        var compiled = capturedPredicate!.Compile();
        Assert.True(compiled(new QuotaPlan { Id = 3 }));
        Assert.False(compiled(new QuotaPlan { Id = 4 }));
    }

    [Fact]
    public async Task Delete_DelegatesToCommand()
    {
        _command.Setup(c => c.DeleteByIdAsync(11, It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var svc = CreateService();
        var rows = await svc.DeleteAsync(11);
        Assert.Equal(1, rows);
        _command.Verify(c => c.DeleteByIdAsync(11, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetDefault_UnsetsAll_ThenSetsTarget()
    {
        int calls = 0;
        // First call: unset all defaults, second call: set specific id
        _command
            .Setup(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlan, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>, Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>>>>(), It.IsAny<CancellationToken>()))
            .Callback(() => calls++)
            .ReturnsAsync(() => calls == 1 ? 5 : 1);

        var svc = CreateService();
        await svc.SetDefaultAsync(77);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task SetDefault_Throws_WhenTargetNotFound()
    {
        int calls = 0;
        _command
            .Setup(c => c.UpdateWhereAsync(It.IsAny<System.Linq.Expressions.Expression<Func<QuotaPlan, bool>>>(), It.IsAny<System.Linq.Expressions.Expression<Func<Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>, Microsoft.EntityFrameworkCore.Query.SetPropertyCalls<QuotaPlan>>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => calls++ == 0 ? 5 : 0); // second call returns 0 rows

        var svc = CreateService();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => svc.SetDefaultAsync(123));
    }

    [Fact]
    public async Task Create_Throws_WhenNameInvalid()
    {
        var svc = CreateService();
        await Assert.ThrowsAsync<ValidationException>(() => svc.CreateAsync(new QuotaPlan { Name = "" }));
    }
}
