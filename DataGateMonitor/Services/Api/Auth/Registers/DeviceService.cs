using Mapster;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.DeviceTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Responses;

namespace DataGateMonitor.Services.Api.Auth.Registers;

public sealed class DeviceService(IUserQueryService userQueryService, ICurrentUserService currentUserService,
    IDeviceQueryService deviceQueryService, ICommandService<Device, int> deviceCommandService) : IDeviceService
{
    public async Task<InstallationIdResponse> AddAndroidInstallationId(InstallationIdRequest request, int userId,
        CancellationToken ct)
    {

        if ((await userQueryService.GetById(currentUserService.UserId, ct)) is null)
        {
            throw new InvalidOperationException("User not found");
        }
        
        var existing = await deviceQueryService
            .GetByInstallationId(request.InstallationId, ct);

        if (existing is not null)
            throw new InvalidOperationException(
                $"Device with InstallationId {request.InstallationId} already exists");

        var device = await deviceCommandService.Add(
            new Device
            {
                InstallationId = request.InstallationId,
                UserId = currentUserService.UserId
            },
            true,
            ct);

        return device.Adapt<InstallationIdResponse>();
    }
}