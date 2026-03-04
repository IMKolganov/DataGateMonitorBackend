namespace OpenVPNGateMonitor.Services.GeoLite.Interfaces;

public interface IGeoLiteAuthProvider
{
    Task<string> GetDownloadUrlAsync(CancellationToken ct);
    Task<string> GetBasicAuthHeaderAsync(CancellationToken ct);
}