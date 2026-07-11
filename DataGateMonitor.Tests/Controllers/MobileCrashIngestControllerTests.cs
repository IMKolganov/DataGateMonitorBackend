using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DataGateMonitor.Configurations;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Api.MobileCrashIngest;

namespace DataGateMonitor.Tests.Controllers;

public class MobileCrashIngestControllerTests
{
    private static void AttachHttpsRequest(
        MobileCrashIngestController controller,
        string body,
        string process = "com.imkolganov.datagate.dev")
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Scheme = "https",
                ContentType = "text/plain; charset=utf-8",
                Body = new MemoryStream(Encoding.UTF8.GetBytes(body)),
            },
        };
        context.Request.Headers["X-Crash-Filename"] = "fatal_2026-05-01T00-00-00.000Z.txt";
        context.Request.Headers["X-Crash-Process"] = process;
        controller.ControllerContext = new ControllerContext { HttpContext = context };
    }

    private static MobileCrashIngestController CreateController(
        CrashIngestOptions options,
        out Mock<IMobileCrashIngestService> ingest,
        ICrashIngestRateLimiter? rateLimiter = null)
    {
        ingest = new Mock<IMobileCrashIngestService>();
        var metrics = new Mock<ICrashIngestMetrics>();
        var logger = new Mock<ILogger<MobileCrashIngestController>>();
        var limiter = rateLimiter ?? CreatePermissiveRateLimiter();
        return new MobileCrashIngestController(
            ingest.Object,
            limiter,
            metrics.Object,
            Options.Create(options),
            logger.Object);
    }

    private static ICrashIngestRateLimiter CreatePermissiveRateLimiter()
    {
        var mock = new Mock<ICrashIngestRateLimiter>();
        mock.Setup(r => r.TryConsume(It.IsAny<string>(), out It.Ref<int>.IsAny)).Returns(true);
        return mock.Object;
    }

    [Fact]
    public async Task Ingest_WithValidPayload_ReturnsNoContentAndCallsService()
    {
        var options = new CrashIngestOptions { RequireHttps = true, MaxPayloadBytes = 1024 };
        var controller = CreateController(options, out var ingest);
        var payload = """
                      process=com.imkolganov.datagate.dev
                      kind=fatal

                      java.lang.RuntimeException: boom
                      """;
        AttachHttpsRequest(controller, payload);

        var result = await controller.Ingest(CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        ingest.Verify(
            s => s.SaveAsync("com.imkolganov.datagate.dev", "fatal_2026-05-01T00-00-00.000Z.txt", It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Ingest_WithEmptyBody_ReturnsBadRequest()
    {
        var controller = CreateController(new CrashIngestOptions(), out _);
        AttachHttpsRequest(controller, string.Empty);

        var result = await controller.Ingest(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Ingest_WhenPayloadTooLarge_ReturnsPayloadTooLarge()
    {
        var controller = CreateController(new CrashIngestOptions { MaxPayloadBytes = 32 }, out _);
        AttachHttpsRequest(controller, new string('A', 64));

        var result = await controller.Ingest(CancellationToken.None);

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status413PayloadTooLarge, status.StatusCode);
    }

    [Fact]
    public async Task Ingest_WhenRateLimitExceeded_ReturnsTooManyRequests()
    {
        var options = new CrashIngestOptions
        {
            RateLimitMaxRequests = 1,
            RateLimitWindowSeconds = 300,
        };
        using var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(
            new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var rateLimiter = new MemoryCrashIngestRateLimiter(cache, Options.Create(options));
        var controller = CreateController(options, out _, rateLimiter);

        var payload = """
                      process=com.imkolganov.datagate.dev

                      stacktrace
                      """;
        AttachHttpsRequest(controller, payload);
        var first = await controller.Ingest(CancellationToken.None);
        Assert.IsType<NoContentResult>(first);

        AttachHttpsRequest(controller, payload);
        var second = await controller.Ingest(CancellationToken.None);
        var status = Assert.IsType<ObjectResult>(second);
        Assert.Equal(StatusCodes.Status429TooManyRequests, status.StatusCode);
    }
}
