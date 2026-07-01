namespace DataGateMonitor.Services.DataGateOpenVpnManager.OpenVpnProxy;

internal static class VpnServerApiUrlNormalizer
{
    public static string Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return string.Empty;
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            var left = uri.GetLeftPart(UriPartial.Authority) + uri.AbsolutePath;
            return left.TrimEnd('/').ToLowerInvariant();
        }
        catch
        {
            return url.Trim().TrimEnd('/').ToLowerInvariant();
        }
    }

    public static bool Equals(string? a, string? b) => Normalize(a) == Normalize(b);
}
