using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.IssuedXrayClientLinkTokenTable;

public interface IIssuedXrayClientLinkTokenQueryService
{
    Task<List<IssuedXrayClientLinkToken>> GetByIssuedLinkIds(IEnumerable<int> linkIds, CancellationToken ct);
    Task<IssuedXrayClientLinkToken?> GetByToken(string token, CancellationToken ct);
}
