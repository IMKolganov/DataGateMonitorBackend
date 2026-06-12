namespace DataGateMonitor.Services.Helpers;

public static class DotVersionComparer
{
    public static int Compare(string left, string right)
    {
        var leftParts = ParseParts(left);
        var rightParts = ParseParts(right);
        var length = Math.Max(leftParts.Count, rightParts.Count);

        for (var i = 0; i < length; i++)
        {
            var leftPart = i < leftParts.Count ? leftParts[i] : 0;
            var rightPart = i < rightParts.Count ? rightParts[i] : 0;
            if (leftPart > rightPart)
                return 1;
            if (leftPart < rightPart)
                return -1;
        }

        return 0;
    }

    public static bool IsAtLeast(string version, string minimum) =>
        Compare(version, minimum) >= 0;

    private static List<int> ParseParts(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Trim()
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => int.TryParse(part, out var value) ? value : 0)
            .ToList();
    }
}
