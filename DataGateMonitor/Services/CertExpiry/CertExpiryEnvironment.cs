namespace DataGateMonitor.Services.CertExpiry;

public static class CertExpiryEnvironment
{
    public const string DisabledVariable = "OVPN_CERT_EXPIRY_CHECK_DISABLED";

    public static bool IsEnabled()
    {
        var raw = Environment.GetEnvironmentVariable(DisabledVariable);
        return !string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase)
               && raw != "1";
    }
}
