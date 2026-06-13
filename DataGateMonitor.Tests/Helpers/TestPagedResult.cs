using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Helpers;

public class TestPagedResult<T> : IPagedResult<T>
{
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public List<T> Items { get; init; } = new();
}
