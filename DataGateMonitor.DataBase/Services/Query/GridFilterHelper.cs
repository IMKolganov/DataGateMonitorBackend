namespace DataGateMonitor.DataBase.Services.Query;

public static class GridFilterHelper
{
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    /// <summary>Escapes PostgreSQL ILIKE metacharacters so user input is matched literally.</summary>
    public static string EscapeIlikeLiteral(string value)
        => value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    /// <summary>Case-insensitive contains pattern for <c>EF.Functions.ILike</c>.</summary>
    public static string? ContainsPattern(string? value)
    {
        var normalized = Normalize(value);
        return normalized == null ? null : $"%{EscapeIlikeLiteral(normalized)}%";
    }

    /// <summary>Case-insensitive exact-match pattern for <c>EF.Functions.ILike</c>.</summary>
    public static string? ExactMatchPattern(string? value)
    {
        var normalized = Normalize(value);
        return normalized == null ? null : EscapeIlikeLiteral(normalized);
    }
}
