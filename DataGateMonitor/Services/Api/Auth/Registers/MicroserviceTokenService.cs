using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using DataGateMonitor.Services.Api.Auth.Registers.Interfaces;

namespace DataGateMonitor.Services.Api.Auth.Registers;

public class MicroserviceTokenService : IMicroserviceTokenService
{
    private readonly RsaSecurityKey _rsaSecurityKey;
    private readonly string _publicKeyPem;

    public MicroserviceTokenService(IConfiguration config)
    {
        var basePath = AppContext.BaseDirectory;

        var privateKeyConfigPath = config["MicroserviceJwt:PrivateKeyPath"]
                                   ?? Path.Combine("resources", "certs", "private-microservice.key");
        var publicKeyConfigPath = config["MicroserviceJwt:PublicKeyPath"]
                                  ?? Path.Combine("resources", "certs", "public-microservice.key");

        var privateKeyPath = Path.GetFullPath(Path.Combine(basePath, privateKeyConfigPath));
        var publicKeyPath = Path.GetFullPath(Path.Combine(basePath, publicKeyConfigPath));

        var privateKeyText = File.ReadAllText(privateKeyPath);
        var publicKeyText = File.ReadAllText(publicKeyPath);

        _publicKeyPem = publicKeyText;

        var privateRsa = RSA.Create();
        privateRsa.ImportFromPem(privateKeyText.ToCharArray());

        _rsaSecurityKey = new RsaSecurityKey(privateRsa);
    }

    public string GenerateToken(string subject, string purpose, string role, string audience)
    {
        var handler = new JwtSecurityTokenHandler();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, subject),
            new Claim("purpose", purpose),
            new Claim(ClaimTypes.Role, role)
        };

        var now = DateTimeOffset.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = "OpenVPNGateBackend",
            Audience = audience,
            NotBefore = now.UtcDateTime,
            IssuedAt  = now.UtcDateTime,
            Expires   = now.AddMinutes(10).UtcDateTime,
            SigningCredentials = new SigningCredentials(_rsaSecurityKey, SecurityAlgorithms.RsaSha256)
        };

        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }

    public bool ValidateToken(string token, out ClaimsPrincipal? principal)
    {
        var handler = new JwtSecurityTokenHandler();
        var rsa = RSA.Create();
        rsa.ImportFromPem(_publicKeyPem.ToCharArray());

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };

        try
        {
            principal = handler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch
        {
            principal = null;
            return false;
        }
    }

    public string GetPublicKeyPem() => _publicKeyPem;

    private static string ExportPrivateKeyToPem(RSA rsa)
    {
        var privateKeyBytes = rsa.ExportPkcs8PrivateKey();
        var pemChars = PemEncoding.Write("PRIVATE KEY", privateKeyBytes);
        return new string(pemChars);
    }

    private static string ExportPublicKeyToPem(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportSubjectPublicKeyInfo();
        var pemChars = PemEncoding.Write("PUBLIC KEY", publicKeyBytes);
        return new string(pemChars);
    }
}