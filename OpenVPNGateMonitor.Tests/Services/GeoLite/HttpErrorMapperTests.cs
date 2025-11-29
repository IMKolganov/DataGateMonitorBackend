using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using OpenVPNGateMonitor.Services.GeoLite;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.GeoLite;

public class HttpErrorMapperTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, "400 Bad Request")]
    [InlineData(HttpStatusCode.Unauthorized, "401 Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "403 Forbidden")]
    [InlineData(HttpStatusCode.NotFound, "404 Not Found")]
    [InlineData((HttpStatusCode)429, "429 Too Many Requests")]
    [InlineData(HttpStatusCode.InternalServerError, "500 Internal Server Error")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "503 Service Unavailable")]
    public void Maps_Known_Status_Codes(HttpStatusCode code, string expectedPart)
    {
        var mapper = new HttpErrorMapper(new NullLogger<HttpErrorMapper>());
        using var response = new HttpResponseMessage(code) { ReasonPhrase = "Test" };

        var msg = mapper.Map(response);

        Assert.Contains(expectedPart, msg);
    }

    [Fact]
    public void Maps_Unknown_Code_With_Reason()
    {
        var mapper = new HttpErrorMapper(new NullLogger<HttpErrorMapper>());
        using var response = new HttpResponseMessage((HttpStatusCode)418) { ReasonPhrase = "I'm a teapot" };

        var msg = mapper.Map(response);
        Assert.Contains("418", msg);
        Assert.Contains("I'm a teapot", msg);
    }
}
