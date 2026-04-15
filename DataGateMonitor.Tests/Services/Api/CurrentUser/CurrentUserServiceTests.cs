using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Moq;
using DataGateMonitor.Services.Api.CurrentUser;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.CurrentUser;

public class CurrentUserServiceTests
{
    [Fact]
    public void UserId_Returns_Parsed_NameIdentifier_Claim()
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "42") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);

        var sut = new CurrentUserService(accessor.Object);

        Assert.Equal(42, sut.UserId);
    }

    [Fact]
    public void UserId_When_Claim_Is_Invalid_Throws()
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, "not-a-number") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var context = new DefaultHttpContext { User = principal };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);

        var sut = new CurrentUserService(accessor.Object);

        Assert.Throws<FormatException>(() => _ = sut.UserId);
    }
}
