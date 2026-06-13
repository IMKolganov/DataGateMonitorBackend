using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.TagTable;
using DataGateMonitor.Models;

namespace DataGateMonitor.Services.Tags;

public class TagService(
    ICommandService<Tag, int> tagCommandService,
    ITagQueryService tagQueryService) : ITagService
{
    public Task<List<Tag>> GetAllAsync(CancellationToken ct = default)
        => tagQueryService.GetAll(ct);

    public Task<Tag?> GetByIdAsync(int id, CancellationToken ct = default)
        => tagQueryService.GetById(id, ct);

    public async Task<Tag> CreateAsync(string name, CancellationToken ct = default)
    {
        var tag = new Tag { Name = name.Trim() };
        return await tagCommandService.Add(tag, saveChanges: true, ct);
    }

    public async Task<int> UpdateAsync(int id, string name, CancellationToken ct = default)
    {
        var tag = await tagQueryService.GetById(id, ct) ?? throw new InvalidOperationException("Tag not found.");
        tag.Name = name.Trim();
        return await tagCommandService.Update(tag, saveChanges: true, ct);
    }

    public Task<int> DeleteAsync(int id, CancellationToken ct = default)
        => tagCommandService.DeleteById(id, ct);
}
