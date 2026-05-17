using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using DataGateMonitor.Services.StatusStreamLogs;

namespace DataGateMonitor.Tests.Services.StatusStreamLogs;

public class StatusStreamLogStoreTests
{
    [Fact]
    public async Task AppendAsync_StoresLatestEntriesInMemory_WhenRedisUnavailable()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var logger = new Mock<ILogger<StatusStreamLogStore>>();
        var store = new StatusStreamLogStore(config, logger.Object);

        for (var i = 1; i <= 5; i++)
        {
            await store.AppendAsync(new StatusStreamLogEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow.AddSeconds(i),
                PayloadJson = $"{{\"seq\":{i}}}",
                Source = "memory"
            });
        }

        var latest = await store.GetLatestAsync(3);

        Assert.Equal(3, latest.Count);
        Assert.Contains("\"seq\":5", latest[0].PayloadJson);
        Assert.Contains("\"seq\":4", latest[1].PayloadJson);
        Assert.Contains("\"seq\":3", latest[2].PayloadJson);
    }

    [Fact]
    public async Task AppendAsync_RespectsConfiguredMaxEntries()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["StatusStreamLogs:MaxEntries"] = "3"
            })
            .Build();
        var logger = new Mock<ILogger<StatusStreamLogStore>>();
        var store = new StatusStreamLogStore(config, logger.Object);

        for (var i = 1; i <= 6; i++)
        {
            await store.AppendAsync(new StatusStreamLogEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow.AddSeconds(i),
                PayloadJson = $"{{\"seq\":{i}}}",
                Source = "memory"
            });
        }

        var latest = await store.GetLatestAsync(10);

        Assert.Equal(3, latest.Count);
        Assert.Contains("\"seq\":6", latest[0].PayloadJson);
        Assert.Contains("\"seq\":5", latest[1].PayloadJson);
        Assert.Contains("\"seq\":4", latest[2].PayloadJson);
    }
}
