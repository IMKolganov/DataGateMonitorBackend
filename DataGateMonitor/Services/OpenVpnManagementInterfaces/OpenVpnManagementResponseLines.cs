namespace DataGateMonitor.Services.OpenVpnManagementInterfaces;

/// <summary>
/// Filters OpenVPN management-interface response lines that are not payload data.
/// </summary>
public static class OpenVpnManagementResponseLines
{
    /// <summary>
    /// True for protocol control lines (e.g. <c>END</c>, <c>&gt;INFO:</c>, <c>&gt;STATE:</c>) that should not be parsed as CSV payload.
    /// </summary>
    public static bool IsProtocolLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return true;

        var trimmed = line.Trim();
        if (trimmed.Equals("END", StringComparison.OrdinalIgnoreCase))
            return true;

        return trimmed.StartsWith('>');
    }

    /// <summary>
    /// Strips a leading <c>&gt;STATE:</c> prefix from a state CSV line when present.
    /// </summary>
    public static string NormalizeStateCsvLine(string line)
    {
        const string statePrefix = ">STATE:";
        if (line.StartsWith(statePrefix, StringComparison.OrdinalIgnoreCase))
            return line[statePrefix.Length..];

        return line;
    }
}
