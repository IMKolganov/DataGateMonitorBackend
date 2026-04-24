using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.UserQuotaPlans.Responses;

namespace DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IUserQuotaPlanService
{
    Task<UserQuotaPlan> AssignQuotaPlanAsync(int userId, int quotaPlanId, CancellationToken ct);

    Task<GetAllUserQuotaPlansResponse> GetPageAsync(GetAllUserQuotaPlansRequest request, CancellationToken ct);
    Task<UserQuotaPlanResponse?> GetByIdAsync(int id, CancellationToken ct);
    Task<List<UserQuotaPlanDto>> GetListByUserIdAsync(int userId, CancellationToken ct);
    Task<UserQuotaPlanResponse> CreateAsync(CreateOrUpdateUserQuotaPlanRequest request, CancellationToken ct);
    Task UpdateAsync(CreateOrUpdateUserQuotaPlanRequest request, CancellationToken ct);
    Task DeleteAsync(int id, CancellationToken ct);
}