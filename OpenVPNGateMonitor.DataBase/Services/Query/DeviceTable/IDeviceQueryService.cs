using System.Linq.Expressions;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.DataBase.Services.Query.DeviceTable;

public interface IDeviceQueryService
{
    Task<List<Device>> GetAll(CancellationToken ct);
    Task<Device?> GetById(int id, CancellationToken ct);
    Task<Device?> GetByInstallationId(string installationId, CancellationToken ct);
    Task<IPagedResult<Device>> GetPage(int page, int pageSize, CancellationToken ct);
    public Task<List<Device>> Search(
        Expression<Func<Device, bool>> predicate,
        CancellationToken ct);
}