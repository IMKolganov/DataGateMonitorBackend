using Microsoft.AspNetCore.Identity;

namespace OpenVPNGateMonitor.Services.Auth.Interfaces;


public interface IPasswordHasher
{
    string Hash(object userKey, string password);
    PasswordVerificationResult Verify(object userKey, string hash, string password);
}