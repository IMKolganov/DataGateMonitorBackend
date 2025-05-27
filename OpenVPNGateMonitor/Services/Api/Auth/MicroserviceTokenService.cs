using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using OpenVPNGateMonitor.Services.Api.Auth.Interfaces;

namespace OpenVPNGateMonitor.Services.Api.Auth;

public class MicroserviceTokenService : IMicroserviceTokenService
{
    private readonly RSA _privateRsa;
    private readonly RsaSecurityKey _rsaSecurityKey;
    private readonly string _publicKeyPem;

    public MicroserviceTokenService(IConfiguration config)
    {
        var privateKeyPath = config["Jwt:PrivateKeyPath"];
        var publicKeyPath = config["Jwt:PublicKeyPath"];

        var privateKeyText = File.ReadAllText(privateKeyPath);
        var publicKeyText = File.ReadAllText(publicKeyPath);

        _publicKeyPem = publicKeyText;

        _privateRsa = RSA.Create();
        _privateRsa.ImportFromPem(privateKeyText.ToCharArray());

        _rsaSecurityKey = new RsaSecurityKey(_privateRsa);
    }

    public string GenerateToken(string subject)
    {
        var handler = new JwtSecurityTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, subject)
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
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
}
