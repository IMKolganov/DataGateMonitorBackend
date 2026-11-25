namespace DataGateMonitor.SharedModels.ClientIdentity;

/// <summary>
/// Every request to the DataGateMonitor backend must say who is calling and which build it is.
/// Requests without a known <see cref="NameHeader"/> or without a <see cref="VersionHeader"/> are rejected with 400.
/// </summary>
public static class ClientIdentityRules
{
    public const string NameHeader = "X-Client-Name";
    public const string VersionHeader = "X-Client-Version";

    /// <summary>
    /// Browser WebSockets cannot send custom headers, so SignalR hub URLs may carry the identity in the query string instead.
    /// </summary>
    public const string NameQueryParameter = "clientName";
    public const string VersionQueryParameter = "clientVersion";

    public const int MaxVersionLength = 64;

    public const string ErrorCodeMissingName = "client_name_missing";
    public const string ErrorCodeUnknownName = "client_name_unknown";
    public const string ErrorCodeMissingVersion = "client_version_missing";
    public const string ErrorCodeInvalidVersion = "client_version_invalid";

    public static IReadOnlyList<string> AllowedNames { get; } =
    [
        ClientNames.Frontend,
        ClientNames.Linux,
        ClientNames.Mac,
        ClientNames.Android,
        ClientNames.Windows,
        ClientNames.Ios,
        ClientNames.OpenVpnNode,
        ClientNames.XrayNode
    ];

    public static bool IsAllowedName(string? name) =>
        name is not null && AllowedNames.Contains(name, StringComparer.Ordinal);

    /// <summary>
    /// Version content is not compared against anything; it only has to be present, short and printable ASCII.
    /// </summary>
    public static bool IsValidVersion(string? version)
    {
        if (string.IsNullOrWhiteSpace(version) || version.Length > MaxVersionLength)
            return false;

        foreach (var c in version)
        {
            if (c < 0x20 || c > 0x7E)
                return false;
        }

        return true;
    }

    public static ClientIdentityValidationResult Validate(string? name, string? version)
    {
        name = name?.Trim();
        version = version?.Trim();

        if (string.IsNullOrEmpty(name))
            return ClientIdentityValidationResult.Fail(ErrorCodeMissingName,
                $"Header '{NameHeader}' is required. Allowed values: {string.Join(", ", AllowedNames)}.");

        if (!IsAllowedName(name))
            return ClientIdentityValidationResult.Fail(ErrorCodeUnknownName,
                $"Header '{NameHeader}' has unknown value '{Sanitize(name)}'. Allowed values: {string.Join(", ", AllowedNames)}.");

        if (string.IsNullOrEmpty(version))
            return ClientIdentityValidationResult.Fail(ErrorCodeMissingVersion,
                $"Header '{VersionHeader}' is required.");

        if (!IsValidVersion(version))
            return ClientIdentityValidationResult.Fail(ErrorCodeInvalidVersion,
                $"Header '{VersionHeader}' must be 1-{MaxVersionLength} printable ASCII characters.");

        return ClientIdentityValidationResult.Ok(new ClientInfo(name, version));
    }

    private static string Sanitize(string value)
    {
        var cut = value.Length > MaxVersionLength ? value[..MaxVersionLength] : value;
        return new string(cut.Select(c => c is < (char)0x20 or > (char)0x7E ? '?' : c).ToArray());
    }
}

public static class ClientNames
{
    public const string Frontend = "frontend";
    public const string Linux = "linux";
    public const string Mac = "mac";
    public const string Android = "android";
    public const string Windows = "windows";
    public const string Ios = "ios";
    public const string OpenVpnNode = "openvpn-node";
    public const string XrayNode = "xray-node";
}

public sealed record ClientInfo(string Name, string Version);

public sealed class ClientIdentityValidationResult
{
    public bool IsValid { get; private init; }
    public ClientInfo? Identity { get; private init; }
    public string? ErrorCode { get; private init; }
    public string? ErrorMessage { get; private init; }

    public static ClientIdentityValidationResult Ok(ClientInfo identity) =>
        new() { IsValid = true, Identity = identity };

    public static ClientIdentityValidationResult Fail(string errorCode, string errorMessage) =>
        new() { IsValid = false, ErrorCode = errorCode, ErrorMessage = errorMessage };
}

/// <summary>
/// <c>Data</c> payload of the 400 <see cref="Responses.ApiResponse{T}"/> returned when the client identity is missing or invalid.
/// </summary>
public sealed class ClientIdentityError
{
    public string Code { get; set; } = string.Empty;
    public string NameHeader { get; set; } = ClientIdentityRules.NameHeader;
    public string VersionHeader { get; set; } = ClientIdentityRules.VersionHeader;
    public IReadOnlyList<string> AllowedNames { get; set; } = ClientIdentityRules.AllowedNames;
}
