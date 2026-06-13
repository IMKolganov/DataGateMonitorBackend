using Google.Apis.Auth;
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
            throw new UnauthorizedAccessException(MapGoogleJwtValidationMessage(ex));
        }

        if (payload == null)
            throw new UnauthorizedAccessException("Invalid Google ID token.");

        return new GoogleUserInfo
        {
            Subject = payload.Subject,
            Email = payload.Email,
            EmailVerified = payload.EmailVerified,
            Name = payload.Name,
            Picture = string.IsNullOrWhiteSpace(payload.Picture) ? null : payload.Picture.Trim()
        };
    }

    private static string MapGoogleJwtValidationMessage(InvalidJwtException ex) =>
        ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase)
            ? "Google ID token has expired."
            : "Invalid Google ID token.";
}