using System.Security.Claims;
using OpenVPNGateMonitor.Services.Api.CurrentUser.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.CurrentUser;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public int UserId =>
        int.Parse(
            httpContextAccessor.HttpContext!
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier)!
        );
}
