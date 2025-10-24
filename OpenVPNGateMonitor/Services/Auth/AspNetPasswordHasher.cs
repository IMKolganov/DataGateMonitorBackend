using Microsoft.AspNetCore.Identity;
using OpenVPNGateMonitor.Services.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.Auth;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<object> _inner = new();

    public string Hash(object userKey, string password)
        => _inner.HashPassword(userKey, password);

    public PasswordVerificationResult Verify(object userKey, string hash, string password)
        => _inner.VerifyHashedPassword(userKey, hash, password) switch
        {
            PasswordVerificationResult.Failed => PasswordVerificationResult.Failed,
            PasswordVerificationResult.Success => PasswordVerificationResult.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationResult.SuccessRehashNeeded,
            _ => PasswordVerificationResult.Failed
        };
}