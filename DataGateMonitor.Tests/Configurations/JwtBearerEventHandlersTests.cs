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
    public void IsExpectedClientTokenFailure_ReturnsFalse_ForInvalidSignature() =>
        Assert.False(JwtBearerEventHandlers.IsExpectedClientTokenFailure(new SecurityTokenInvalidSignatureException("bad sig")));
}
