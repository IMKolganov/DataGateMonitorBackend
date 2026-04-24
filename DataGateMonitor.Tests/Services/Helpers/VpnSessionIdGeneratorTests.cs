using DataGateMonitor.Services.Helpers;
using Xunit;

namespace DataGateMonitor.Tests.Services.Helpers;

public class VpnSessionIdGeneratorTests
{
    [Fact]
    public void FromCommonNameRemoteConnectedSince_IsDeterministic()
    {
        var since = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var a = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince("u1", "10.0.0.1:443", since);
        var b = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince("u1", "10.0.0.1:443", since);
        Assert.Equal(a, b);
    }

    [Fact]
    public void FromCommonNameRemoteConnectedSince_DiffersWhenInputsChange()
    {
        var since = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
        var a = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince("u1", "10.0.0.1:443", since);
        var b = VpnSessionIdGenerator.FromCommonNameRemoteConnectedSince("u2", "10.0.0.1:443", since);
        Assert.NotEqual(a, b);
    }
}
