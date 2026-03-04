using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Tags;

public interface ITagService
{
    Task<List<Tag>> GetAllAsync(CancellationToken ct = default);
    Task<Tag?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<Tag> CreateAsync(string name, CancellationToken ct = default);
    Task<int> UpdateAsync(int id, string name, CancellationToken ct = default);
    Task<int> DeleteAsync(int id, CancellationToken ct = default);
}
