using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DataGateMonitor.Services.Api.Privacy;

/// <summary>Masks identifiers and email-like values in API responses for non-privileged callers.</summary>
public static partial class SensitiveDataMasker
{
    private const string MaskedEmailPlaceholder = "***@***";

    [GeneratedRegex(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex EmailRegex();

    public static string MaskIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return MaskAsterisks(value);
    }

    public static string MaskCommonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        if (LooksLikeEmail(trimmed))
            return MaskedEmailPlaceholder;

        return $"cn-{Hash8(trimmed)}";
    }

    public static string MaskFreeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        if (LooksLikeEmail(trimmed))
            return MaskedEmailPlaceholder;

        return MaskEmailsInText(trimmed);
    }

    public static string MaskDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        if (LooksLikeEmail(trimmed))
            return MaskedEmailPlaceholder;

        if (EmailRegex().IsMatch(trimmed))
            return MaskEmailsInText(trimmed);

        return MaskAsterisks(trimmed);
    }

    public static string? MaskTelegramHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return $"@{Hash8(value.Trim())}";
    }

    public static string MaskEmailsInText(string text) =>
        EmailRegex().Replace(text, _ => MaskedEmailPlaceholder);

    public static string MaskIpAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return MaskAsterisks(value.Trim());
    }

    public static string? MaskOptionalIpAddress(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return MaskIpAddress(value);
    }

    private static bool LooksLikeEmail(string value) =>
        value.Contains('@', StringComparison.Ordinal) && EmailRegex().IsMatch(value);

    /// <summary>Stable pseudonym: 5–10 asterisks derived from the source value.</summary>
    public static string MaskAsterisks(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        var length = 5 + (hash[0] % 6);
        return new string('*', length);
    }

    private static string Hash8(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }
}
