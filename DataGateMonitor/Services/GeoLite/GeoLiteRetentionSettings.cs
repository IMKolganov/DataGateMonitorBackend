namespace DataGateMonitor.Services.GeoLite;

/// <summary>Settings keys and defaults for GeoLite2 on-disk version retention.</summary>
public static class GeoLiteRetentionSettings
{
    public const string MaxKeptVersionsKey = "GeoIp_Max_Kept_Versions";

    /// <summary>Used when the setting is missing or non-positive.</summary>
    public const int DefaultMaxKeptVersions = 10;

    public const int MinKeptVersions = 1;
    public const int MaxKeptVersionsCeiling = 100;

    public static int ResolveMaxKept(int storedValue)
    {
        if (storedValue <= 0)
            return DefaultMaxKeptVersions;
        return Math.Clamp(storedValue, MinKeptVersions, MaxKeptVersionsCeiling);
    }
}
