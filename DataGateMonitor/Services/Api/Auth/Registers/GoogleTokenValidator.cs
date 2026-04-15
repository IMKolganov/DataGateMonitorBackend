using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;
using DataGateMonitor.SharedModels.Auth.Google;

namespace DataGateMonitor.Services.Api.Auth.Registers;

public sealed class GoogleTokenValidator(IConfiguration configuration) : IGoogleTokenValidator
{
    private readonly string _webClientId = configuration["GoogleAuth:ClientId"]
                                           ?? throw new InvalidOperationException("GoogleAuth:ClientId is not configured.");

    private readonly string _desktopClientId = configuration["GoogleAuth:DesktopClientId"]
                                               ?? throw new InvalidOperationException("GoogleAuth:DesktopClientId is not configured.");

    private readonly string _iosClientId = configuration["GoogleAuth:IosClientId"]
                                           ?? throw new InvalidOperationException("GoogleAuth:IosClientId is not configured.");

    public async Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("IdToken is required.", nameof(idToken));

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [_webClientId, _desktopClientId, _iosClientId]
        };

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            throw new SecurityTokenException("Invalid Google ID token.", ex);
        }

        if (payload == null)
            throw new SecurityTokenException("Invalid Google ID token payload.");

        return new GoogleUserInfo
        {
            Subject = payload.Subject,
            Email = payload.Email,
            EmailVerified = payload.EmailVerified,
            Name = payload.Name
        };
    }
}