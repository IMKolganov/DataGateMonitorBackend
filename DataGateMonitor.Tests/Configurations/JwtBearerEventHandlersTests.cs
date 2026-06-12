using DataGateMonitor.Configurations;
using Microsoft.IdentityModel.Tokens;

namespace DataGateMonitor.Tests.Configurations;

public sealed class JwtBearerEventHandlersTests
{
    [Fact]
    public void IsExpectedClientTokenFailure_ReturnsTrue_ForExpiredToken() =>
        Assert.True(JwtBearerEventHandlers.IsExpectedClientTokenFailure(new SecurityTokenExpiredException("expired")));

    [Fact]
    public void IsExpectedClientTokenFailure_ReturnsTrue_ForNotYetValidToken() =>
        Assert.True(JwtBearerEventHandlers.IsExpectedClientTokenFailure(new SecurityTokenNotYetValidException("nbf")));

    [Fact]
    public void IsExpectedClientTokenFailure_ReturnsTrue_WhenWrappedInInnerException()
    {
        var inner = new SecurityTokenExpiredException("IDX10223: Lifetime validation failed.");
        var outer = new InvalidOperationException("validate failed", inner);

        Assert.True(JwtBearerEventHandlers.IsExpectedClientTokenFailure(outer));
    }

    [Fact]
    public void IsExpectedClientTokenFailure_ReturnsTrue_ForIdx10223Message()
    {
        var ex = new Exception("IDX10223: Lifetime validation failed. The token is expired.");

        Assert.True(JwtBearerEventHandlers.IsExpectedClientTokenFailure(ex));
    }
}
