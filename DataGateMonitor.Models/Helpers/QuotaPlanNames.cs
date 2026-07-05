namespace DataGateMonitor.Models.Helpers;

public static class QuotaPlanNames
{
    public const string Free = "Free";
    public const string Default = "Default";

    public static bool IsFreeOrDefault(string? planName)
    {
        if (string.IsNullOrWhiteSpace(planName))
            return false;

        return string.Equals(planName, Free, StringComparison.OrdinalIgnoreCase)
               || string.Equals(planName, Default, StringComparison.OrdinalIgnoreCase);
    }
}
