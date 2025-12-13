using OpenVPNGateMonitor.Models;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IUserQuotaPlanService
{
    Task<UserQuotaPlan> AssignQuotaPlanAsync(int userId, int quotaPlanId, CancellationToken ct);
}