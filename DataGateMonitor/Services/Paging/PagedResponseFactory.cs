using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Services.Paging;

/// <summary>Builds <see cref="PagedResponse{T}"/> for in-memory or already-sliced pages.</summary>
public static class PagedResponseFactory
{
    public static PagedResponse<T> FromItems<T>(
        IReadOnlyList<T> allItems,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Max(1, pageSize);
        var total = allItems.Count;
        var items = allItems
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<T>
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Items = items
        };
    }

    public static PagedResponse<T> Create<T>(
        IReadOnlyList<T> pageItems,
        int page,
        int pageSize,
        int totalCount)
    {
        return new PagedResponse<T>
        {
            Page = Math.Max(1, page),
            PageSize = Math.Max(1, pageSize),
            TotalCount = Math.Max(0, totalCount),
            Items = pageItems.ToList()
        };
    }
}
