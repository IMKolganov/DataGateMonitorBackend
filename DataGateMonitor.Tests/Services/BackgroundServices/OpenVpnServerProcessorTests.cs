using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.Models;
using DataGateMonitor.Services.BackgroundServices;
using DataGateMonitor.Services.BackgroundServices.Interfaces;
using DataGateMonitor.Services.DataGateOpenVpnManager.Interfaces;
using Xunit;

namespace DataGateMonitor.Tests.Services.BackgroundServices;

public class OpenVpnServerProcessorTests
{
    [Fact]
    public async Task ProcessServerAsync_CallsConflogService_FetchAndSaveIfChangedByServerId()
    {
        var conflogService = new Mock<IVpnServerConflogService>();
        conflogService.Setup(s => s.FetchAndSaveIfChangedByServerIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((VpnServerConflog?)null);

        var openVpnServerService = new Mock<IVpnServerService>();
        openVpnServerService.Setup(s => s.SaveVpnServerStatusLogAsync(It.IsAny<VpnServer>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        openVpnServerService.Setup(s => s.SaveConnectedClientsAsync(It.IsAny<VpnServer>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var serverCmd = new Mock<ICommandService<VpnServer, int>>();
        serverCmd.Setup(c => c.UpdateWhere(It.IsAny<System.Linq.Expressions.Expression<Func<VpnServer, bool>>>(), It.IsAny<Action<Microsoft.EntityFrameworkCore.Query.UpdateSettersBuilder<VpnServer>>>(), It.IsAny<CancellationToken>())).Returns(Task.FromResult(1));

        var services = new ServiceCollection();
        services.AddSingleton(openVpnServerService.Object);
        services.AddSingleton(serverCmd.Object);
        services.AddSingleton(conflogService.Object);
        var sp = services.BuildServiceProvider();

        var logger = new Mock<Microsoft.Extensions.Logging.ILogger<OpenVpnServerProcessor>>();
        var processor = new OpenVpnServerProcessor(logger.Object, sp);

        var server = new VpnServer { Id = 42, ServerName = "Test", ApiUrl = "https://test" };

        await processor.ProcessServerAsync(server, CancellationToken.None);

        conflogService.Verify(s => s.FetchAndSaveIfChangedByServerIdAsync(42, It.IsAny<CancellationToken>()), Times.Once);
        openVpnServerService.Verify(s => s.SaveVpnServerStatusLogAsync(server, It.IsAny<CancellationToken>()), Times.Once);
        openVpnServerService.Verify(s => s.SaveConnectedClientsAsync(server, It.IsAny<CancellationToken>()), Times.Once);
    }
}
