using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.UserQuotaPlans.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers;

public class UserQuotaPlanService(
    IUserQuotaPlanQueryService userQuotaPlanQueryService,
    ICommandService<UserQuotaPlan, int> userQuotaPlanCommandService) : IUserQuotaPlanService
{
    public async Task<UserQuotaPlan> AssignQuotaPlanAsync(int userId, int quotaPlanId, CancellationToken ct)
    {
        var exists = await userQuotaPlanQueryService.GetByUserIdAndQuotaPlanId(userId, quotaPlanId, ct);

        if (exists is not null)
            return exists;

        var userQuotaPlan = new UserQuotaPlan
        {
            UserId = userId,
            QuotaPlanId = quotaPlanId,
        };

        userQuotaPlan = await userQuotaPlanCommandService.Add(userQuotaPlan, true, ct);

        return userQuotaPlan;
    }

    public async Task<GetAllUserQuotaPlansResponse> GetPageAsync(GetAllUserQuotaPlansRequest request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        if (pageSize > 500)
            pageSize = 500;

        var userIdFilter = request.UserId is > 0 ? request.UserId : null;
        var paged = await userQuotaPlanQueryService.GetPage(page, pageSize, userIdFilter, ct);

        var items = paged.Items.Adapt<List<UserQuotaPlanDto>>();

        return new GetAllUserQuotaPlansResponse
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = items
        };
    }

    public async Task<UserQuotaPlanResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await userQuotaPlanQueryService.GetById(id, ct);
        if (entity is null)
            return null;

        return new UserQuotaPlanResponse
        {
            UserQuotaPlan = entity.Adapt<UserQuotaPlanDto>()
        };
    }

    public async Task<List<UserQuotaPlanDto>> GetListByUserIdAsync(int userId, CancellationToken ct)
    {
        var list = await userQuotaPlanQueryService.GetListByUserId(userId, ct);
        return list.Adapt<List<UserQuotaPlanDto>>();
    }

    public async Task<UserQuotaPlanResponse> CreateAsync(CreateOrUpdateUserQuotaPlanRequest request, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var effectiveFrom = request.EffectiveFrom == default ? now : request.EffectiveFrom;

        var entity = new UserQuotaPlan
        {
            UserId = request.UserId,
            QuotaPlanId = request.QuotaPlanId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = request.EffectiveTo,
            AssignedBy = request.AssignedBy,
            Note = request.Note,
            CreateDate = now,
            LastUpdate = now
        };

        entity = await userQuotaPlanCommandService.Add(entity, true, ct);

        return new UserQuotaPlanResponse
        {
            UserQuotaPlan = entity.Adapt<UserQuotaPlanDto>()
        };
    }

    public async Task UpdateAsync(CreateOrUpdateUserQuotaPlanRequest request, CancellationToken ct)
    {
        if (request.Id <= 0)
            throw new ArgumentException("Id is required for update.", nameof(request));

        var entity = await userQuotaPlanQueryService.GetById(request.Id, ct)
            ?? throw new KeyNotFoundException($"UserQuotaPlan {request.Id} not found.");

        var now = DateTimeOffset.UtcNow;
        entity.UserId = request.UserId;
        entity.QuotaPlanId = request.QuotaPlanId;
        entity.EffectiveFrom = request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.AssignedBy = request.AssignedBy;
        entity.Note = request.Note;
        entity.LastUpdate = now;

        await userQuotaPlanCommandService.Update(entity, true, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await userQuotaPlanCommandService.DeleteById(id, ct);
        if (deleted == 0)
            throw new KeyNotFoundException($"UserQuotaPlan {id} not found.");
    }
}