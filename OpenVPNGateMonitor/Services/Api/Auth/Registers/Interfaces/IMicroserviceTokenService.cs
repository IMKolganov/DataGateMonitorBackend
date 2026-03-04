using System.Security.Claims;

namespace OpenVPNGateMonitor.Services.Api.Auth.Registers.Interfaces;

public interface IMicroserviceTokenService
{
    string GenerateToken(string subject, string purpose, string role, string audience);
    bool ValidateToken(string token, out ClaimsPrincipal? principal);
    string GetPublicKeyPem();
}