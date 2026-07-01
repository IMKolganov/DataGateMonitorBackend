using DataGateMonitor.Services.CertExpiry;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryEnvironmentTests
{
    [Theory]
    [InlineData(null, true)]
    [InlineData("false", true)]
    [InlineData("FALSE", true)]
    [InlineData("0", true)]
    [InlineData("true", false)]
    [InlineData("TRUE", false)]
    [InlineData("1", false)]
    public void IsEnabled_RespectsDisableFlag(string? envValue, bool expected)
    {
        var previous = Environment.GetEnvironmentVariable(CertExpiryEnvironment.DisabledVariable);
        try
        {
            Environment.SetEnvironmentVariable(CertExpiryEnvironment.DisabledVariable, envValue);
            Assert.Equal(expected, CertExpiryEnvironment.IsEnabled());
        }
        finally
        {
            Environment.SetEnvironmentVariable(CertExpiryEnvironment.DisabledVariable, previous);
        }
    }
}
