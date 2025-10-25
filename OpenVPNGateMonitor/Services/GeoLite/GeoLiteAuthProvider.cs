using System.Text;
using OpenVPNGateMonitor.Services.GeoLite.Helpers;
using OpenVPNGateMonitor.Services.GeoLite.Interfaces;

namespace OpenVPNGateMonitor.Services.GeoLite;


public class GeoLiteAuthProvider(IServiceProvider sp) : IGeoLiteAuthProvider
{
    public async Task<string> GetDownloadUrlAsync(CancellationToken ct)
    {
        var url = await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Download_Url", sp, ct);
        return url ?? throw new InvalidOperationException("GeoIp_Download_Url is not configured.");
    }

    public async Task<string> GetBasicAuthHeaderAsync(CancellationToken ct)
    {
        var accountId = await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_Account_ID", sp, ct);
        var licenseKey = await GeoLiteLoadConfigs.GetStringParamFromSettings("GeoIp_License_Key", sp, ct);

        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(licenseKey))
            throw new InvalidOperationException("GeoLite Account ID or License Key is missing.");

        return Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountId}:{licenseKey}"));
    }
}