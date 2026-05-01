using DataGateMonitor.DataBase.Contexts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DataGateMonitor.Tests.Controllers;

public class WindowsCrashIngestControllerIntegrationTests
{
    private static HttpRequestMessage CreateRequest(string body, string process = "com.imkolganov.datagate.win")
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/windows/crash-ingest");
        req.Content = new StringContent(body);
        req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain")
        {
            CharSet = "utf-8"
        };
        req.Headers.TryAddWithoutValidation("X-Crash-Filename", "win_crash_2026-05-01T00-00-00.000Z.txt");
        req.Headers.TryAddWithoutValidation("X-Crash-Process", process);
        return req;
    }

    [Fact]
    public async Task Ingest_WithValidPayload_ReturnsNoContentAndPersists()
    {
        await using var factory = new WindowsCrashIngestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var payload = """
                      process=com.imkolganov.datagate.win
                      kind=fatal

                      java.lang.RuntimeException: boom
                      """;
        using var request = CreateRequest(payload);

        var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(1, await db.WindowsCrashReports.CountAsync());
    }

    [Fact]
    public async Task Ingest_WithEmptyBody_ReturnsBadRequest()
    {
        await using var factory = new WindowsCrashIngestWebApplicationFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var request = CreateRequest(string.Empty);
        var response = await client.SendAsync(request);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Ingest_WhenRateLimitExceeded_ReturnsTooManyRequests()
    {
        await using var factory = new WindowsCrashIngestWebApplicationFactory(rateLimitMaxRequests: 1, rateLimitWindowSeconds: 300);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var payload = """
                      process=com.imkolganov.datagate.win

                      stacktrace
                      """;

        using var request1 = CreateRequest(payload, process: "com.imkolganov.datagate.win");
        using var response1 = await client.SendAsync(request1);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, response1.StatusCode);

        using var request2 = CreateRequest(payload, process: "com.imkolganov.datagate.win");
        using var response2 = await client.SendAsync(request2);
        Assert.Equal((System.Net.HttpStatusCode)429, response2.StatusCode);
    }
}

file sealed class WindowsCrashIngestWebApplicationFactory(
    int maxPayloadBytes = 1024 * 1024,
    int rateLimitMaxRequests = 10,
    int rateLimitWindowSeconds = 60)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            var overrides = new Dictionary<string, string?>
            {
                ["WindowsCrashIngest:MaxPayloadBytes"] = maxPayloadBytes.ToString(),
                ["WindowsCrashIngest:RateLimitMaxRequests"] = rateLimitMaxRequests.ToString(),
                ["WindowsCrashIngest:RateLimitWindowSeconds"] = rateLimitWindowSeconds.ToString(),
                ["WindowsCrashIngest:RequireHttps"] = "true",
                ["WindowsCrashIngest:RecentMaxLimit"] = "100"
            };
            configBuilder.AddInMemoryCollection(overrides);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(ApplicationDbContext));
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(IDbContextFactory<ApplicationDbContext>));

            var databaseName = $"windows-crash-tests-{Guid.NewGuid()}";
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            services.AddDbContextFactory<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}
