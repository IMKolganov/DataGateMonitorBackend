using DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Responses;

namespace DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IDeviceService
{
    Task<InstallationIdResponse> AddAndroidInstallationId(InstallationIdRequest request, int userId,
        CancellationToken ct);
}