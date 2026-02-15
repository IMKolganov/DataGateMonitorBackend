using Mapster;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanAllowedServerTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Dto;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.QuotaPlanAllowedServers.Responses;

namespace OpenVPNGateMonitor.Services.QuotaPlans;

public class QuotaPlanAllowedServerService(
    IQuotaPlanAllowedServerQueryService queryService,
    ICommandService<QuotaPlanAllowedServer, int> commandService) : IQuotaPlanAllowedServerService
{
    public async Task<GetAllQuotaPlanAllowedServersResponse> GetPageAsync(GetAllQuotaPlanAllowedServersRequest request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
        if (pageSize > 500)
            pageSize = 500;

        var quotaPlanId = request.QuotaPlanId is > 0 ? request.QuotaPlanId : null;
        var vpnServerId = request.VpnServerId is > 0 ? request.VpnServerId : null;

        var paged = await queryService.GetPage(page, pageSize, quotaPlanId, vpnServerId, ct);
        var items = paged.Items.Adapt<List<QuotaPlanAllowedServerDto>>();

        return new GetAllQuotaPlanAllowedServersResponse
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = items
        };
    }

    public async Task<QuotaPlanAllowedServerResponse?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await queryService.GetById(id, ct);
        if (entity is null)
            return null;

        return new QuotaPlanAllowedServerResponse
        {
            QuotaPlanAllowedServer = entity.Adapt<QuotaPlanAllowedServerDto>()
        };
    }

    public async Task<List<QuotaPlanAllowedServerDto>> GetListByQuotaPlanIdAsync(int quotaPlanId, CancellationToken ct)
    {
        var list = await queryService.GetListByQuotaPlanId(quotaPlanId, ct);
        return list.Adapt<List<QuotaPlanAllowedServerDto>>();
    }

    public async Task<List<QuotaPlanAllowedServerDto>> GetListByVpnServerIdAsync(int vpnServerId, CancellationToken ct)
    {
        var list = await queryService.GetListByVpnServerId(vpnServerId, ct);
        return list.Adapt<List<QuotaPlanAllowedServerDto>>();
    }

    public async Task<QuotaPlanAllowedServerResponse> CreateAsync(CreateOrUpdateQuotaPlanAllowedServerRequest request, CancellationToken ct)
    {
        var existing = await queryService.GetByQuotaPlanIdAndServerId(request.QuotaPlanId, request.VpnServerId, ct);
        if (existing is not null)
            return new QuotaPlanAllowedServerResponse { QuotaPlanAllowedServer = existing.Adapt<QuotaPlanAllowedServerDto>() };

        var now = DateTimeOffset.UtcNow;
        var entity = new QuotaPlanAllowedServer
        {
            QuotaPlanId = request.QuotaPlanId,
            VpnServerId = request.VpnServerId,
            CreateDate = now,
            LastUpdate = now
        };

        entity = await commandService.Add(entity, true, ct);

        return new QuotaPlanAllowedServerResponse
        {
            QuotaPlanAllowedServer = entity.Adapt<QuotaPlanAllowedServerDto>()
        };
    }

    public async Task UpdateAsync(CreateOrUpdateQuotaPlanAllowedServerRequest request, CancellationToken ct)
    {
        if (request.Id <= 0)
            throw new ArgumentException("Id is required for update.", nameof(request));

        var entity = await queryService.GetById(request.Id, ct)
            ?? throw new KeyNotFoundException($"QuotaPlanAllowedServer {request.Id} not found.");

        var existing = await queryService.GetByQuotaPlanIdAndServerId(request.QuotaPlanId, request.VpnServerId, ct);
        if (existing is not null && existing.Id != request.Id)
            throw new InvalidOperationException("Another assignment already exists for this QuotaPlanId and VpnServerId.");

        entity.QuotaPlanId = request.QuotaPlanId;
        entity.VpnServerId = request.VpnServerId;
        entity.LastUpdate = DateTimeOffset.UtcNow;

        await commandService.Update(entity, true, ct);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var deleted = await commandService.DeleteById(id, true, ct);
        if (deleted == 0)
            throw new KeyNotFoundException($"QuotaPlanAllowedServer {id} not found.");
    }
}
