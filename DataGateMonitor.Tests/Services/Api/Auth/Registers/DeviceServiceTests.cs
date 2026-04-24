using Moq;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.DeviceTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Registers;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.Mobile.Requests;
using Xunit;

namespace DataGateMonitor.Tests.Services.Api.Auth.Registers;

public class DeviceServiceTests
{
    [Fact]
    public async Task AddAndroidInstallationId_When_UserExists_And_InstallationIdNew_AddsDevice()
    {
        var user = new User { Id = 1, DisplayName = "U" };
        var request = new InstallationIdRequest { InstallationId = "inst-123" };

        var userQuery = new Mock<IUserQueryService>();
        userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(1);
        var deviceQuery = new Mock<IDeviceQueryService>();
        deviceQuery.Setup(q => q.GetByInstallationId("inst-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Device?)null);
        Device? captured = null;
        var deviceCommand = new Mock<ICommandService<Device, int>>();
        deviceCommand
            .Setup(c => c.Add(It.IsAny<Device>(), true, It.IsAny<CancellationToken>()))
            .Callback<Device, bool, CancellationToken>((d, _, _) => captured = d)
            .ReturnsAsync((Device d, bool _, CancellationToken _) => { d.Id = 10; return d; });

        var sut = new DeviceService(
            userQuery.Object,
            currentUser.Object,
            deviceQuery.Object,
            deviceCommand.Object);

        var result = await sut.AddAndroidInstallationId(request, 1, CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotNull(captured);
        Assert.Equal("inst-123", captured!.InstallationId);
        Assert.Equal(1, captured.UserId);
    }

    [Fact]
    public async Task AddAndroidInstallationId_When_UserNotFound_Throws()
    {
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(999);
        var userQuery = new Mock<IUserQueryService>();
        userQuery.Setup(q => q.GetById(999, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        var deviceQuery = new Mock<IDeviceQueryService>();
        var deviceCommand = new Mock<ICommandService<Device, int>>();

        var sut = new DeviceService(
            userQuery.Object,
            currentUser.Object,
            deviceQuery.Object,
            deviceCommand.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.AddAndroidInstallationId(new InstallationIdRequest { InstallationId = "x" }, 999, CancellationToken.None));

        Assert.Equal("User not found", ex.Message);
        deviceCommand.Verify(c => c.Add(It.IsAny<Device>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAndroidInstallationId_When_InstallationIdAlreadyExists_Throws()
    {
        var user = new User { Id = 1 };
        var currentUser = new Mock<ICurrentUserService>();
        currentUser.Setup(c => c.UserId).Returns(1);
        var userQuery = new Mock<IUserQueryService>();
        userQuery.Setup(q => q.GetById(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var deviceQuery = new Mock<IDeviceQueryService>();
        deviceQuery.Setup(q => q.GetByInstallationId("dup", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Device { Id = 5, InstallationId = "dup" });
        var deviceCommand = new Mock<ICommandService<Device, int>>();

        var sut = new DeviceService(
            userQuery.Object,
            currentUser.Object,
            deviceQuery.Object,
            deviceCommand.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.AddAndroidInstallationId(new InstallationIdRequest { InstallationId = "dup" }, 1, CancellationToken.None));

        Assert.Contains("already exists", ex.Message);
        deviceCommand.Verify(c => c.Add(It.IsAny<Device>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
