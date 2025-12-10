using Google.Apis.Auth;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;
using OpenVPNGateMonitor.SharedModels.Auth.Google;
using Microsoft.IdentityModel.Tokens;

namespace OpenVPNGateMonitor.Services.Api.Auth;

public sealed class GoogleTokenValidator(IConfiguration configuration) : IGoogleTokenValidator
{
    private readonly string clientId = configuration["GoogleAuth:ClientId"]
                                       ?? throw new InvalidOperationException("GoogleAuth:ClientId is not configured.");

    public async Task<GoogleUserInfo> ValidateAsync(string idToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            throw new ArgumentException("IdToken is required.", nameof(idToken));

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = [clientId]
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