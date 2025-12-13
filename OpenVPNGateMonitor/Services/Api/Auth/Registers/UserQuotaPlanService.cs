using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using OpenVPNGateMonitor.DataBase.Services.Query.UserQuotaPlanTable;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers;

public class UserQuotaPlanService(
    IUserQuotaPlanQueryService userQuotaPlanQueryService, 
    IQuotaPlanQueryService quotaPlanQueryService, 
    ICommandService<UserQuotaPlan, int> userQuotaPlanCommandService) : IUserQuotaPlanService
{
    public async Task<UserQuotaPlan> AssignQuotaPlanAsync(int userId, int quotaPlanId, CancellationToken ct)
    {
        var exists = await userQuotaPlanQueryService.GetByUserIdAndQuotaPlanId(userId, quotaPlanId, ct);

        if (exists is not null)
            return exists;

        var userQuotaPlan = new UserQuotaPlan()
        {
            UserId = userId,
            QuotaPlanId = quotaPlanId,
        };

        userQuotaPlan = await userQuotaPlanCommandService.AddAsync(userQuotaPlan, true, ct);

        return userQuotaPlan;
    }
}