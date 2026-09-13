using DataGateMonitor.Services.Paging;
using Xunit;

namespace DataGateMonitor.Tests.Services.Paging;

public class PagedResponseFactoryTests
{
    [Fact]
    public void FromItems_SlicesAndSetsTotalCount()
    {
        var all = Enumerable.Range(1, 25).ToList();
        var page = PagedResponseFactory.FromItems(all, page: 2, pageSize: 10);

        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(25, page.TotalCount);
        Assert.Equal(Enumerable.Range(11, 10), page.Items);
    }

    [Fact]
    public void Create_UsesProvidedTotalCount()
    {
        var page = PagedResponseFactory.Create([1, 2, 3], page: 1, pageSize: 3, totalCount: 100);
        Assert.Equal(100, page.TotalCount);
        Assert.Equal([1, 2, 3], page.Items);
    }
}
