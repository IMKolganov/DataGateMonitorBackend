using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.DeviceTable;

public class DeviceQueryService(IQueryService<Device, int> q) : IDeviceQueryService
{
    public Task<List<Device>> GetAll(CancellationToken ct)
        => q.GetAll(ct: ct);

    public Task<Device?> GetById(int id, CancellationToken ct)
        => q.FindById(id, ct: ct);

    public Task<Device?> GetByInstallationId(string installationId, CancellationToken ct)
        => q.Query()
            .FirstOrDefaultAsync(x => x.InstallationId == installationId, ct);

    public Task<IPagedResult<Device>> GetPage(int page, int pageSize, CancellationToken ct)
        => q.Page(page, pageSize, ct: ct);

    public Task<List<Device>> Search(
        Expression<Func<Device, bool>> predicate,
        CancellationToken ct)
        => q.Where(predicate, ct: ct);
}