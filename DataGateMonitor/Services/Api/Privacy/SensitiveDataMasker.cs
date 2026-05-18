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

        return $"user-{Hash8(value)}";
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

        return $"User {Hash8(trimmed)}";
    }

    public static string? MaskTelegramHandle(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;

        return $"@{Hash8(value.Trim())}";
    }

    public static string MaskEmailsInText(string text) =>
        EmailRegex().Replace(text, _ => MaskedEmailPlaceholder);

    private static bool LooksLikeEmail(string value) =>
        value.Contains('@', StringComparison.Ordinal) && EmailRegex().IsMatch(value);

    private static string Hash8(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }
}
