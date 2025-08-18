namespace OpenVPNGateMonitor.SharedModels.Responses;

public sealed class PagedResponse<T> : IPagedResult<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public List<T> Items { get; set; } = new();

    // Optional helpers
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page * PageSize < TotalCount;
    public bool HasPrev => Page > 1;
}
