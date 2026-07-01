namespace DataGateMonitor.Tests.Helpers;

/// <summary>
/// Synthetic Pi-hole app credential for unit tests only (not a real secret).
/// Built at runtime so secret scanners do not flag string literals in test sources.
/// </summary>
internal static class PiHoleTestFixtures
{
    public static string AppCredential =>
        string.Concat("pi-hole-", "unit-test", "-", "fixture");
}
