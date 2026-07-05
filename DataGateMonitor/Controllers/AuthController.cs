using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using DataGateMonitor.Services.Api.Auth.ForgotPassword;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.Services.Api.Auth.TelegramLogin;
using DataGateMonitor.Services.Api.Auth.Totp;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController(
    IConfiguration config,
    IApplicationService appService,
    IMicroserviceTokenService microserviceTokenService,
    IUserRegistrationService userRegistrationService,
    IUserLoginService userLoginService,
    IUserQueryService userQueryService,
    IEmailConfirmationService emailConfirmationService,
    ITelegramAccountLinkService telegramAccountLinkService,
    IGoogleAuthCodeExchangeService exchange,
    ITokenService tokenService,
    IAdminForgotPasswordService adminForgotPasswordService,
    ITelegramLoginCodeService telegramLoginCodeService,
    IAdminTotpService adminTotpService,
    ICurrentUserService currentUserService,
    IAdminIdleSessionTracker adminIdleSessionTracker,
    IUserSessionService userSessionService) : BaseController
{
    [AllowAnonymous]
    [HttpGet("session-policy")]
    [ProducesResponseType(typeof(ApiResponse<AuthSessionPolicyResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<AuthSessionPolicyResponse>> GetSessionPolicy()
    {
        var minutes = config.GetValue<int?>("Jwt:AdminIdleTimeoutMinutes") ?? 15;
        if (minutes <= 0)
            minutes = 15;

        return Ok(ApiResponse<AuthSessionPolicyResponse>.SuccessResponse(new AuthSessionPolicyResponse
        {
            AdminIdleTimeoutMinutes = minutes,
        }));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("activity")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult RecordAdminActivity()
    {
        adminIdleSessionTracker.Touch(currentUserService.UserId);
        return NoContent();
    }

    [HttpPost("token")]
    public async Task<ActionResult<ApiResponse<TokenResponse>>> GenerateToken([FromBody] TokenRequest request,
        CancellationToken cancellationToken)
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
            Issuer = "OpenVPNGateBackend",
            Audience = "OpenVPNGateFrontend",
            NotBefore = now.UtcDateTime,
            IssuedAt = now.UtcDateTime,
            Expires = now.AddHours(1).UtcDateTime,
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

    /// <summary>
    /// For the client app / dashboard: request a one-time code to enter in the Telegram bot to link accounts.
    /// Requires a signed-in user with Google or password (local) identity, not yet linked to Telegram.
    /// </summary>
    [Authorize]
    [HttpPost("telegram/request-account-link-code")]
    [ProducesResponseType(typeof(ApiResponse<RequestTelegramAccountLinkCodeResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RequestTelegramAccountLinkCodeResponse>>> RequestTelegramAccountLinkCode(
        CancellationToken ct)
    {
        var result = await telegramAccountLinkService.RequestLinkCodeAsync(currentUserService.UserId, ct);
        return Ok(ApiResponse<RequestTelegramAccountLinkCodeResponse>.SuccessResponse(result));
    }

    [AllowAnonymous]
    [HttpPost("email/request-confirmation")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> RequestEmailConfirmation(
        [FromBody] RequestEmailConfirmationRequest request,
        CancellationToken ct)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(ApiResponse<string>.ErrorResponse("Email is required."));

        // Prevent account enumeration: always return success message.
        try
        {
            var user = await userQueryService.GetByEmail(request.Email.Trim(), ct);
            if (user is { IsEmailConfirmed: false } && !string.IsNullOrWhiteSpace(user.Email))
                await emailConfirmationService.SendConfirmationAsync(user.Id, user.Email, ct);
        }
        catch
        {
            // Intentionally ignored to avoid leaking account existence.
        }

        return Ok(ApiResponse<string>.SuccessResponse("If this email exists, confirmation code has been sent."));
    }

    [AllowAnonymous]
    [HttpPost("email/confirm")]
    [ProducesResponseType(typeof(ApiResponse<ConfirmEmailResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ConfirmEmailResponse>>> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken ct)
    {
        var result = await emailConfirmationService.ConfirmAsync(request.Email, request.Code, ct);
        return Ok(ApiResponse<ConfirmEmailResponse>.SuccessResponse(result));
    }

    [AllowAnonymous]
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(ApiResponse<GoogleLoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GoogleLoginResponse>>> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken ct)
    {
        var result = await userLoginService.LoginWithGoogleAsync(request.IdToken, ct);
        return Ok(ApiResponse<GoogleLoginResponse>.SuccessResponse(result));
    }
    
    [AllowAnonymous]
    [HttpPost("google-code-login")]
    [ProducesResponseType(typeof(ApiResponse<GoogleLoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GoogleLoginResponse>>> GoogleCodeLogin(
        [FromBody] GoogleCodeLoginRequest request,
        CancellationToken ct)
    {
        if (request == null)
            return BadRequest(ApiResponse<GoogleLoginResponse>.ErrorResponse("Request body is required."));

        var idToken = await exchange.ExchangeCodeForIdTokenAsync(
            request.Code,
            request.CodeVerifier,
            request.RedirectUri,
            ct);

        var result = await userLoginService.LoginWithGoogleAsync(idToken, ct);

        return Ok(ApiResponse<GoogleLoginResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// For the Telegram bot: request a one-time login code for a user. Bot shows this code to the user to enter on the dashboard.
    /// Requires App or Admin token.
    /// </summary>
    [Authorize(Roles = "Admin,App")]
    [HttpPost("telegram/request-login-code")]
    [ProducesResponseType(typeof(ApiResponse<TelegramRequestLoginCodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TelegramRequestLoginCodeResponse>>> TelegramRequestLoginCode(
        [FromBody] TelegramRequestLoginCodeRequest request,
        CancellationToken ct)
    {
        if (request == null)
            return BadRequest(ApiResponse<TelegramRequestLoginCodeResponse>.ErrorResponse("Request body is required."));

        var result = await telegramLoginCodeService.RequestLoginCodeAsync(request, ct);
        if (result == null)
            return NotFound(ApiResponse<TelegramRequestLoginCodeResponse>.ErrorResponse("User not found or blocked."));

        return Ok(ApiResponse<TelegramRequestLoginCodeResponse>.SuccessResponse(result));
    }

    /// <summary>
    /// Log in on the dashboard using the code received from the Telegram bot.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("telegram-code-login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> TelegramCodeLogin(
        [FromBody] TelegramCodeLoginRequest request,
        CancellationToken ct)
    {
        var result = await telegramLoginCodeService.LoginWithCodeAsync(request, ct);
        return Ok(ApiResponse<LoginResponse>.SuccessResponse(result));
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

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse<AdminForgotPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminForgotPasswordResponse>>> ForgotPassword(
        [FromBody] AdminForgotPasswordRequest request,
        CancellationToken ct)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await adminForgotPasswordService.RequestResetCodeAsync(request, clientIp, ct);
        return Ok(ApiResponse<AdminForgotPasswordResponse>.SuccessResponse(result));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse<AdminResetPasswordResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AdminResetPasswordResponse>>> ResetPassword(
        [FromBody] AdminResetPasswordRequest request,
        CancellationToken ct)
    {
        var result = await adminForgotPasswordService.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse<AdminResetPasswordResponse>.SuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(ApiResponse<GetUserSessionsResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<GetUserSessionsResponse>>> GetSessions(CancellationToken ct)
    {
        var refreshToken = Request.Headers["X-Refresh-Token"].ToString();
        if (string.IsNullOrWhiteSpace(refreshToken))
            refreshToken = null;

        var sessions = await userSessionService.GetActiveSessionsAsync(
            currentUserService.UserId,
            refreshToken,
            ct);

        return Ok(ApiResponse<GetUserSessionsResponse>.SuccessResponse(sessions));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("sessions/{sessionId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeSession([FromRoute] int sessionId, CancellationToken ct)
    {
        await userSessionService.RevokeSessionAsync(currentUserService.UserId, sessionId, ct);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("sessions/revoke-all")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> RevokeAllSessions(
        [FromBody] RevokeUserSessionsRequest? request,
        CancellationToken ct)
    {
        var count = await userSessionService.RevokeSessionsAsync(
            currentUserService.UserId,
            keepRefreshToken: null,
            ct);

        return Ok(ApiResponse<int>.SuccessResponse(count));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("sessions/revoke-others")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<int>>> RevokeOtherSessions(
        [FromBody] RevokeUserSessionsRequest request,
        CancellationToken ct)
    {
        var count = await userSessionService.RevokeSessionsAsync(
            currentUserService.UserId,
            request.KeepRefreshToken,
            ct);

        return Ok(ApiResponse<int>.SuccessResponse(count));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshRequest? request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
            await userSessionService.RevokeByRefreshTokenAsync(request.RefreshToken, ct);

        await HttpContext.SignOutAsync("UserCookie");
        return Ok();
    }

    [AllowAnonymous]
    [HttpPost("totp/verify-login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> VerifyTotpLogin(
        [FromBody] TotpVerifyLoginRequest request,
        CancellationToken ct)
    {
        var result = await adminTotpService.VerifyLoginChallengeAsync(request, ct);
        return Ok(ApiResponse<LoginResponse>.SuccessResponse(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("totp/status")]
    [ProducesResponseType(typeof(ApiResponse<TotpStatusResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TotpStatusResponse>>> GetTotpStatus(CancellationToken ct)
    {
        var status = await adminTotpService.GetStatusAsync(currentUserService.UserId, ct);
        return Ok(ApiResponse<TotpStatusResponse>.SuccessResponse(status));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("totp/setup")]
    [ProducesResponseType(typeof(ApiResponse<TotpSetupResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TotpSetupResponse>>> BeginTotpSetup(CancellationToken ct)
    {
        var setup = await adminTotpService.BeginSetupAsync(currentUserService.UserId, ct);
        return Ok(ApiResponse<TotpSetupResponse>.SuccessResponse(setup));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("totp/confirm")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> ConfirmTotpSetup(
        [FromBody] TotpConfirmRequest request,
        CancellationToken ct)
    {
        await adminTotpService.ConfirmSetupAsync(currentUserService.UserId, request, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Two-factor authentication enabled."));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("totp/disable")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> DisableTotp(
        [FromBody] TotpDisableRequest request,
        CancellationToken ct)
    {
        await adminTotpService.DisableAsync(currentUserService.UserId, request, ct);
        return Ok(ApiResponse<string>.SuccessResponse("Two-factor authentication disabled."));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<RefreshResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RefreshResponse>>> Refresh([FromBody] RefreshRequest request,
        CancellationToken ct)
    {
        var tokens = await tokenService.RefreshAsync(
            request.RefreshToken,
            request.DeviceId,
            request.UserAgent,
            ct);

        var response = new RefreshResponse
        {
            Token = tokens.AccessToken,
            Expiration = tokens.AccessExpiresAt,
            RefreshToken = tokens.RefreshToken,
            RefreshExpiration = tokens.RefreshExpiresAt
        };

        return Ok(ApiResponse<RefreshResponse>.SuccessResponse(response));
    }
}