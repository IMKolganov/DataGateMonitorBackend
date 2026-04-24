using System.Security.Claims;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;

namespace DataGateMonitor.Services.Api.CurrentUser;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public int UserId =>
        int.Parse(
            httpContextAccessor.HttpContext!
                .User
                .FindFirstValue(ClaimTypes.NameIdentifier)!
        );
}
