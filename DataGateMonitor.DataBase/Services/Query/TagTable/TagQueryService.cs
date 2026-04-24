using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.DataBase.Services.Query.TagTable;

public class TagQueryService(IQueryService<Tag, int> q) : ITagQueryService
{
    public Task<List<Tag>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<Tag?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<Tag?> GetByName(string name, CancellationToken ct)
        => q.FirstOrDefault(
            predicate: x => x.Name == name,
            orderBy: s => s.OrderBy(x => x.Id),
            ct: ct);

    public Task<IPagedResult<Tag>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);
}
