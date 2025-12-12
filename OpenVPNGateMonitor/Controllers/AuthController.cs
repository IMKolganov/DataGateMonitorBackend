using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using OpenVPNGateMonitor.Models.Helpers.Auth;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.Services.Api.Auth.Login;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Requests;
using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.Auth.Responses;
using OpenVPNGateMonitor.SharedModels.Responses;

namespace OpenVPNGateMonitor.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(
    IConfiguration config, 
    IApplicationService appService, 
    IMicroserviceTokenService microserviceTokenService,
    IUserRegistrationService userRegistrationService, 
    IUserLoginService  userLoginService,
    IGoogleAuthService googleAuthService) : BaseController
{
    // [HttpGet("system-secret-status")]
    // public async Task<ActionResult<ApiResponse<SystemSecretStatusResponse>>> GetSystemStatus(CancellationToken cancellationToken)
    // {
    //     var isSet = await appService.IsSystemApplicationSetAsync(cancellationToken);
    //     return Ok(ApiResponse<SystemSecretStatusResponse>.SuccessResponse(
    //         new SystemSecretStatusResponse { SystemSet = isSet }));
    // }

    // [HttpPost("set-system-secret")]
    // public async Task<ActionResult<ApiResponse<AuthResponse>>> SetSystemSecret([FromBody] SetSecretRequest request,
    //     CancellationToken cancellationToken)
    // {
    //     var systemApp = await appService.GetApplicationSystemByClientIdAsync(request.ClientId, 
    //         cancellationToken);
    //
    //     if (systemApp is { ClientSecret: not null })
    //     {
    //         return BadRequest(ApiResponse<AuthResponse>.ErrorResponse("System application is already set"));
    //     }
    //
    //     var hashedSecret = BCrypt.Net.BCrypt.HashPassword(request.ClientSecret);
    //
    //     systemApp ??= new ClientApplication
    //     {
    //         Name = "OpenVPN Gate Monitor Dashboard",
    //         ClientId = request.ClientId,
    //         ClientSecret = hashedSecret,
    //         IsSystem = true
    //     };
    //
    //     await appService.UpdateApplicationAsync(systemApp, cancellationToken);
    //
    //     return Ok(ApiResponse<AuthResponse>.SuccessResponse(
    //         new AuthResponse { Message = "ClientSecret set successfully" }));
    // }

    [HttpPost("token")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> GenerateToken([FromBody] TokenRequest request, CancellationToken cancellationToken)
    {
        var app = await appService.GetApplicationByClientIdAsync(request.ClientId, cancellationToken);
        if (app == null)
        {
            return Unauthorized(ApiResponse<TokenResponse>.ErrorResponse("Invalid credentials"));
        }
    
        var isValid = app.IsSystem
            ? BCrypt.Net.BCrypt.Verify(request.ClientSecret, app.ClientSecret)
            : app.ClientSecret == request.ClientSecret;
    
        if (!isValid)
        {
            return Unauthorized(ApiResponse<TokenResponse>.ErrorResponse("Invalid credentials"));
        }
    
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(config["Jwt:Secret"]
                                          ?? throw new InvalidOperationException("Jwt:Secret"));
        
        var now = DateTimeOffset.UtcNow;
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.Name, request.ClientId),
                new Claim(ClaimTypes.Role, "App")
            ]),
            Issuer    = "OpenVPNGateBackend",
            Audience  = "OpenVPNGateFrontend",
            NotBefore = now.UtcDateTime,
            IssuedAt  = now.UtcDateTime,
            Expires   = now.AddHours(1).UtcDateTime,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
    
        return Ok(ApiResponse<TokenResponse>.SuccessResponse(
            new TokenResponse
            {
                Token = tokenHandler.WriteToken(token),
                Expiration = tokenDescriptor.Expires ?? DateTimeOffset.UtcNow
            }));
    }
    
    [HttpGet("public-key/{pin:int}")]
    public ActionResult<ApiResponse<string>> GetPublicKeyForMicroservice([FromRoute(Name = "pin")] int pin)
    {
        if (pin > 10000)
        {
            var key = microserviceTokenService.GetPublicKeyPem();
            return Ok(ApiResponse<string>.SuccessResponse(key));
        }

        return BadRequest(ApiResponse<string>.ErrorResponse("Invalid pin"));
    }
    
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RegisterUserResponse>>> Register(
        [FromBody] RegisterUserRequest request,
        CancellationToken ct)
    {
        var result = await userRegistrationService.RegisterAsync(request, ct);

        return Ok(ApiResponse<RegisterUserResponse>.SuccessResponse(result));
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(ApiResponse<GoogleLoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GoogleLoginResponse>>> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken ct)
    {
        var result = await googleAuthService.LoginWithGoogleAsync(request.IdToken, ct);
        return Ok(ApiResponse<GoogleLoginResponse>.SuccessResponse(result));
    }
    
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken ct)
    {
        var result = await userLoginService.LoginAsync(request, ct);
        return Ok(ApiResponse<LoginResponse>.SuccessResponse(result));
    }
    
    [Authorize(Policy = "UserOnly")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("UserCookie");
        return Ok();
    }
}