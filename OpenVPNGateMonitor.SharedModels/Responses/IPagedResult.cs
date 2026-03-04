namespace OpenVPNGateMonitor.SharedModels.Responses;

public interface IPagedResult<T>
{
    int Page { get; }
    int PageSize { get; }
    int TotalCount { get; }
    List<T> Items { get; }
}