using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using DataGateMonitor.DataBase.Services.Query.DeviceTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;
using DataGateMonitor.Tests.Helpers;

namespace DataGateMonitor.Tests.DataBase.Services.Query.DeviceTable;

public class DeviceQueryServiceTests
{
    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }
        public DbSet<Device> Devices => Set<Device>();
    }

    private static (Mock<IQueryService<Device, int>> q, TestDbContext ctx) CreateEfBackedQuery(IEnumerable<Device> data)
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new TestDbContext(options);
        ctx.Devices.AddRange(data);
        ctx.SaveChanges();

        var mock = new Mock<IQueryService<Device, int>>();
        mock.Setup(q => q.Query(It.IsAny<bool>(), It.IsAny<Expression<Func<Device, object>>[]>()))
            .Returns(ctx.Devices);
        return (mock, ctx);
    }

    [Fact]
    public async Task GetAll_Delegates_To_IQueryService()
    {
        var items = new List<Device> { new() { Id = 1, UserId = 1, InstallationId = "a" } };
        var q = new Mock<IQueryService<Device, int>>();
        q.Setup(x => x.GetAll(true, It.IsAny<CancellationToken>())).ReturnsAsync(items);

        var sut = new DeviceQueryService(q.Object);
        var result = await sut.GetAll(CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetById_Delegates_To_FindById()
    {
        var item = new Device { Id = 3, UserId = 1, InstallationId = "inst" };
        var q = new Mock<IQueryService<Device, int>>();
        q.Setup(x => x.FindById(3, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Device, object>>[]>()))
            .ReturnsAsync(item);

        var sut = new DeviceQueryService(q.Object);
        var result = await sut.GetById(3, CancellationToken.None);

        Assert.Same(item, result);
    }

    [Fact]
    public async Task GetByInstallationId_Filters_By_InstallationId()
    {
        var data = new List<Device>
        {
            new() { Id = 1, UserId = 1, InstallationId = "wanted" },
            new() { Id = 2, UserId = 2, InstallationId = "other" },
        };
        var (q, ctx) = CreateEfBackedQuery(data);
        var sut = new DeviceQueryService(q.Object);

        var result = await sut.GetByInstallationId("wanted", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Id);
        await ctx.DisposeAsync();
    }

    [Fact]
    public async Task GetPage_Delegates_To_Page()
    {
        var paged = new TestPagedResult<Device>
        {
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            Items = [new Device { Id = 1, UserId = 1, InstallationId = "x" }],
        } as IPagedResult<Device>;

        var q = new Mock<IQueryService<Device, int>>();
        q.Setup(x => x.Page(1, 10, null, null, true, It.IsAny<CancellationToken>(), It.IsAny<Expression<Func<Device, object>>[]>()))
            .ReturnsAsync(paged);

        var sut = new DeviceQueryService(q.Object);
        var result = await sut.GetPage(1, 10, CancellationToken.None);

        Assert.Same(paged, result);
    }

    [Fact]
    public async Task Search_Delegates_To_Where()
    {
        var rows = new List<Device> { new() { Id = 1, UserId = 9, InstallationId = "x" } };
        var q = new Mock<IQueryService<Device, int>>();
        q.Setup(x => x.Where(It.IsAny<Expression<Func<Device, bool>>>(), null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rows);

        var sut = new DeviceQueryService(q.Object);
        var result = await sut.Search(d => d.UserId == 9, CancellationToken.None);

        Assert.Same(rows, result);
    }
}
