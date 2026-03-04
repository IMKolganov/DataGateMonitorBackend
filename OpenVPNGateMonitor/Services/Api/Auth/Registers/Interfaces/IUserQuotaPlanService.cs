using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

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