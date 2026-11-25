namespace DataGateMonitor.Configurations;

/// <summary>
/// Switch for <see cref="Middlewares.ClientIdentityMiddleware"/>. Env: <c>ClientIdentity__Enforce=true</c>.
/// Off by default so the backend can ship before every client sends X-Client-Name / X-Client-Version.
/// </summary>
public sealed class ClientIdentityOptions
{
    public const string SectionName = "ClientIdentity";

    /// <summary>
    /// true: requests without a valid client identity get 400. false: they pass, the identity is still recorded when present.
    /// </summary>
    public bool Enforce { get; set; }
}
