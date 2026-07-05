using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;

namespace DataGateMonitor.Tests.DataBase.Services.Query.UserTable;

public partial class UserQueryServiceTests
{
    [Fact]
    public async Task GetPage_Filters_By_IsAdmin()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.Users.AddRangeAsync(
            new User { Id = 1, DisplayName = "admin", IsAdmin = true },
            new User { Id = 2, DisplayName = "user", IsAdmin = false }
        );
        await ctx.SaveChangesAsync();

        var page = await sut.GetPage(new GetAllUsersRequest { Page = 1, PageSize = 10, IsAdmin = true }, CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(1, page.Items[0].Id);
    }

    [Fact]
    public async Task GetPage_Filters_By_IsBlocked()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.Users.AddRangeAsync(
            new User { Id = 1, DisplayName = "active", IsBlocked = false },
            new User { Id = 2, DisplayName = "blocked", IsBlocked = true }
        );
        await ctx.SaveChangesAsync();

        var page = await sut.GetPage(new GetAllUsersRequest { Page = 1, PageSize = 10, IsBlocked = true }, CancellationToken.None);

        Assert.Equal(1, page.TotalCount);
        Assert.Single(page.Items);
        Assert.Equal(2, page.Items[0].Id);
    }

    [Fact]
    public async Task GetPage_Clamps_PageSize_To_500()
    {
        var (sut, ctx) = CreateSutWithContext();
        await ctx.Users.AddAsync(new User { Id = 1, DisplayName = "u1" });
        await ctx.SaveChangesAsync();

        var page = await sut.GetPage(new GetAllUsersRequest { Page = 1, PageSize = 9999 }, CancellationToken.None);

        Assert.Equal(500, page.PageSize);
    }
}
