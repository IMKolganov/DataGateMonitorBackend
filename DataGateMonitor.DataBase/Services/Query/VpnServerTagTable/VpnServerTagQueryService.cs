using Microsoft.EntityFrameworkCore;
using DataGateMonitor.DataBase.UnitOfWork;
using DataGateMonitor.Models;

namespace DataGateMonitor.DataBase.Services.Query.VpnServerTagTable;

public class VpnServerTagQueryService(
    IQueryService<VpnServerTag, int> q,
    IUnitOfWork uow) : IVpnServerTagQueryService
{
    public Task<List<VpnServerTag>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<List<VpnServerTag>> GetListByVpnServerId(int vpnServerId, CancellationToken ct)
        => q.Where(x => x.VpnServerId == vpnServerId, ct: ct);

    public Task<List<VpnServerTag>> GetListByTagId(int tagId, CancellationToken ct)
        => q.Where(x => x.TagId == tagId, ct: ct);

    public async Task<List<string>> GetTagNamesByVpnServerId(int vpnServerId, CancellationToken ct)
    {
        var links = await q.Where(x => x.VpnServerId == vpnServerId, ct: ct);
        if (links.Count == 0)
            return [];

        var tagIds = links.Select(x => x.TagId).Distinct().ToList();
        var names = await uow.GetQuery<Tag>()
            .AsQueryable()
            .Where(t => tagIds.Contains(t.Id))
            .Select(t => t.Name)
            .OrderBy(n => n)
            .ToListAsync(ct);
        return names;
    }

    public async Task<Dictionary<int, List<string>>> GetTagNamesByVpnServerIds(IReadOnlyCollection<int> vpnServerIds, CancellationToken ct)
    {
        if (vpnServerIds.Count == 0)
            return new Dictionary<int, List<string>>();

        var links = await q.Where(x => vpnServerIds.Contains(x.VpnServerId), ct: ct);
        if (links.Count == 0)
            return vpnServerIds.ToDictionary(id => id, _ => new List<string>());

        var tagIds = links.Select(x => x.TagId).Distinct().ToList();
        var tags = await uow.GetQuery<Tag>()
            .AsQueryable()
            .Where(t => tagIds.Contains(t.Id))
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(ct);
        var tagById = tags.ToDictionary(t => t.Id, t => t.Name);

        var result = new Dictionary<int, List<string>>();
        foreach (var id in vpnServerIds)
            result[id] = [];
        foreach (var link in links)
        {
            if (tagById.TryGetValue(link.TagId, out var name))
                result[link.VpnServerId].Add(name);
        }
        foreach (var list in result.Values)
            list.Sort(StringComparer.Ordinal);
        return result;
    }
}
