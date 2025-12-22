using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Mobile.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Mobile.Responses;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IDeviceService
{
    Task<InstallationIdResponse> AddAndroidInstallationId(InstallationIdRequest request, int userId,
        CancellationToken ct);
}