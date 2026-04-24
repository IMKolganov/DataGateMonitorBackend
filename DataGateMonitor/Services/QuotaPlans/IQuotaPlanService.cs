using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Services.QuotaPlans;

public interface IQuotaPlanService
{
    Task<List<QuotaPlan>> GetAllAsync(CancellationToken ct = default);
    Task<QuotaPlan?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IPagedResult<QuotaPlan>> GetPageAsync(int page, int pageSize, CancellationToken ct = default);
    Task<QuotaPlan?> GetDefaultAsync(CancellationToken ct = default);
    Task<QuotaPlan> CreateAsync(QuotaPlan input, bool makeDefault = false, CancellationToken ct = default);
    Task<int> UpdateAsync(QuotaPlan input, CancellationToken ct = default);
    Task<int> DeleteAsync(int id, CancellationToken ct = default);
    Task<int> ActivateAsync(int id, CancellationToken ct = default);
    Task<int> DeactivateAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Sets the provided plan as the only default plan.
    /// </summary>
    Task SetDefaultAsync(int id, CancellationToken ct = default);
}