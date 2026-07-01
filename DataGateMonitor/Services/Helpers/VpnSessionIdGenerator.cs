using System.Linq;
using System.Security.Cryptography;
using System.Text;
using DataGateMonitor.Services.OpenVpnManagementInterfaces;

namespace DataGateMonitor.Services.Helpers;

/// <summary>
/// Deterministic session id for <see cref="Models.VpnServerClient"/> (OpenVPN and Xray).
/// </summary>
public static class VpnSessionIdGenerator
{
    public static Guid FromCommonNameRemoteConnectedSince(
        string commonName,
        string remoteAddress,
        DateTimeOffset connectedSince)
    {
        var normalizedRemote = OpenVpnRealAddressParser.NormalizeRemoteIp(remoteAddress);
        var sessionString = $"{commonName}-{normalizedRemote}-{connectedSince:o}";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sessionString));
        return new Guid(hashBytes.Take(16).ToArray());
    }
}
