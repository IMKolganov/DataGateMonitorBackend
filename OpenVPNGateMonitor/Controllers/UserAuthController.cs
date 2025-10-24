using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenVPNGateMonitor.DataBase.Services.Query.UserTable;
using OpenVPNGateMonitor.Services.Auth.Interfaces;

namespace OpenVPNGateMonitor.Controllers;

[Route("[controller]")]
[ApiController]
public sealed class UserAuthController(IUserAuthService userAuth, IUserQueryService users) : ControllerBase
{
    public sealed record LoginRequest(string Login, string Password);//todo: move to shared models
    public sealed record LoginResponse(string DisplayName, bool IsAdmin);//todo: move to shared models

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        var result = await userAuth.VerifyAsync(req.Login, req.Password, ct);
        if (!result.Ok) return Unauthorized(new { message = result.Reason ?? "invalid_credentials" });

        var user = await users.GetByIdAsync(result.UserId, ct);
        if (user is null || user.IsBlocked) return Forbid();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, "User"),
            new("is_admin", user.IsAdmin ? "1" : "0")
        };

        var identity = new ClaimsIdentity(claims, "UserCookie");
        await HttpContext.SignInAsync("UserCookie", new ClaimsPrincipal(identity));
        return Ok(new LoginResponse(user.DisplayName, user.IsAdmin));
    }

    [Authorize(Policy = "UserOnly")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("UserCookie");
        return Ok();
    }
}