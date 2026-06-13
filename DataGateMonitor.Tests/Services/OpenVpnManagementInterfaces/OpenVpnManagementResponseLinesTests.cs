using DataGateMonitor.Services.OpenVpnManagementInterfaces;

namespace DataGateMonitor.Tests.Services.OpenVpnManagementInterfaces;

public sealed class OpenVpnManagementResponseLinesTests
{
    [Theory]
    [InlineData("END")]
    [InlineData("end")]
    [InlineData(" END ")]
    [InlineData(">INFO:OpenVPN Management Interface Version 5")]
    [InlineData(">STATE:1740000000,CONNECTED,SUCCESS,10.8.0.1,1.2.3.4,")]
    [InlineData("")]
    [InlineData("   ")]
    public void IsProtocolLine_ReturnsTrue_ForManagementControlLines(string line) =>
        Assert.True(OpenVpnManagementResponseLines.IsProtocolLine(line));

    [Fact]
    public void IsProtocolLine_ReturnsFalse_ForPlainStateCsvLine() =>
        Assert.False(OpenVpnManagementResponseLines.IsProtocolLine("1740000000,CONNECTED,SUCCESS,10.8.0.1,1.2.3.4,"));

    [Fact]
    public void NormalizeStateCsvLine_StripsStatePrefix() =>
        Assert.Equal(
            "1740000000,CONNECTED,SUCCESS,10.8.0.1,1.2.3.4,",
            OpenVpnManagementResponseLines.NormalizeStateCsvLine(
                ">STATE:1740000000,CONNECTED,SUCCESS,10.8.0.1,1.2.3.4,"));
}
