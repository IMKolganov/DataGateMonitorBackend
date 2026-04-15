using System.Reflection;
using Microsoft.AspNetCore.SignalR;
using Moq;
using DataGateMonitor.Hubs;

namespace DataGateMonitor.Tests.Hubs;

public class GeoLiteHubTests
{
    private static FieldInfo GetStartedField()
        => typeof(GeoLiteHub).GetField("_started", BindingFlags.NonPublic | BindingFlags.Static)!
           ?? throw new InvalidOperationException("_started field not found via reflection");

    private static void SetStarted(bool value)
    {
        var field = GetStartedField();
        field.SetValue(null, value);
    }

    private static bool GetStarted()
    {
        var field = GetStartedField();
        return (bool)field.GetValue(null)!;
    }

    [Fact]
    public void GeoLiteHub_Has_Authorize_Attribute()
    {
        // Act
        var attr = typeof(GeoLiteHub).GetCustomAttributes(inherit: true)
            .FirstOrDefault(a => a.GetType().Name.Contains("Authorize"));

        // Assert
        Assert.NotNull(attr);
    }

    [Fact]
    public async Task OnConnectedAsync_Writes_To_Console_And_Sets_Started_Flag()
    {
        // Arrange: reset static flag
        SetStarted(false);

        var connectionId = "conn-geo-1";
        var context = new Mock<HubCallerContext>();
        context.SetupGet(c => c.ConnectionId).Returns(connectionId);

        var hub = new GeoLiteHub
        {
            Context = context.Object
        };

        // Capture console output
        var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);
        try
        {
            // Act
            await hub.OnConnectedAsync();

            // Assert
            var output = sw.ToString();
            Assert.Contains($"Client connected: {connectionId}", output);
            Assert.True(GetStarted());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }
}
