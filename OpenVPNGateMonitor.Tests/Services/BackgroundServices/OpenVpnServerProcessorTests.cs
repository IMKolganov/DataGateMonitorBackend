using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenVPNGateMonitor.DataBase.Services.Command.Interfaces;
using OpenVPNGateMonitor.Models;
using OpenVPNGateMonitor.Services.BackgroundServices;
using OpenVPNGateMonitor.Services.BackgroundServices.Interfaces;
using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using Xunit;

namespace OpenVPNGateMonitor.Tests.Services.BackgroundServices;

public class OpenVpnServerProcessorTests
{
    [Fact]
    public async Task ProcessServerAsync_CallsConflogService_FetchAndSaveIfChangedByServerId()
    {
        var conflogService = new Mock<IOpenVpnServerConflogService>();
        conflogService.Setup(s => s.FetchAndSaveIfChangedByServerIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenVpnServerConflog?)null);

        var openVpnServerService = new Mock<IOpenVpnServerService>();
        openVpnServerService.Setup(s => s.SaveOpenVpnServerStatusLogAsync(It.IsAny<OpenVpnServer>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        openVpnServerService.Setup(s => s.SaveConnectedClientsAsync(It.IsAny<OpenVpnServer>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var serverCmd = new Mock<ICommandService<OpenVpnServer, int>>();
        serverCmd.Setup(c => c.UpdateWhere(It.IsAny<System.Linq.Expressions.Expression<Func<OpenVpnServer, bool>>>(), It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<OpenVpnServer>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

        var services = new ServiceCollection();
        services.AddSingleton(openVpnServerService.Object);
        services.AddSingleton(serverCmd.Object);
        services.AddSingleton(conflogService.Object);
        var sp = services.BuildServiceProvider();

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<OpenVpnServerProcessor>>();
        var processor = new OpenVpnServerProcessor(logger.Object, sp);

        var server = new OpenVpnServer { Id = 42, ServerName = "Test", ApiUrl = "https://test" };

        await processor.ProcessServerAsync(server, CancellationToken.None);

        conflogService.Verify(s => s.FetchAndSaveIfChangedByServerIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        openVpnServerService.Verify(s => s.SaveOpenVpnServerStatusLogAsync(server, It.IsAny<CancellationToken>()), Times.Once);
        openVpnServerService.Verify(s => s.SaveConnectedClientsAsync(server, It.IsAny<CancellationToken>()), Times.Once);
    }
}
