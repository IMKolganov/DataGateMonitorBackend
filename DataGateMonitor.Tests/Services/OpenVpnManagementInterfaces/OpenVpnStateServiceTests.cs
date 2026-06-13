using DataGateMonitor.Services.OpenVpnManagementInterfaces;

namespace DataGateMonitor.Tests.Services.OpenVpnManagementInterfaces;

public sealed class OpenVpnStateServiceTests
{
    [Theory]
    [InlineData(null, "<null>")]
    [InlineData("", "<empty>")]
    [InlineData("END", "END")]
    public void FormatRawResponseForLog_ReturnsReadableValue(string? raw, string expected) =>
        Assert.Equal(expected, OpenVpnStateService.FormatRawResponseForLog(raw));

    [Fact]
    public void FormatRawResponseForLog_TruncatesLongPayload()
    {
        var raw = new string('x', 2000);
        var formatted = OpenVpnStateService.FormatRawResponseForLog(raw, maxLength: 100);

        Assert.StartsWith(new string('x', 100), formatted);
        Assert.Contains("truncated", formatted);
        Assert.Contains("2000 chars", formatted);
    }
}
