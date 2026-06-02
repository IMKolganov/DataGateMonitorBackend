using Microsoft.AspNetCore.Mvc;
using Moq;
using DataGateMonitor.Controllers;
using DataGateMonitor.Services.Api.CurrentUser.Interfaces;
using DataGateMonitor.Services.Users.Interfaces;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Tests.Controllers;

public class UserMergeControllerTests
{
    private readonly Mock<IUserService> _userServiceMock = new(MockBehavior.Strict);
    private readonly Mock<IUserMergeService> _userMergeServiceMock = new(MockBehavior.Strict);
    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new(MockBehavior.Strict);
    private readonly UserController _controller;

    public UserMergeControllerTests()
    {
        _currentUserServiceMock.Setup(s => s.UserId).Returns(1);
        _controller = new UserController(
            _userServiceMock.Object,
            _userMergeServiceMock.Object,
            _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task MergeTelegramGoogle_ReturnsOk_WithDryRunResponse()
    {
        var req = new MergeTelegramGoogleUsersRequest
        {
            TelegramUserId = 10,
            GoogleUserId = 20,
            DryRun = true,
            Note = "preview",
        };

        var expected = new MergeTelegramGoogleUsersResponse
        {
            DryRun = true,
            SurvivorUserId = 10,
            MergedUserId = 20,
            TelegramExternalId = "123456789",
            GoogleExternalId = "google-sub",
        };

        _userMergeServiceMock
            .Setup(s => s.MergeTelegramGoogleAsync(
                It.Is<MergeTelegramGoogleUsersRequest>(r => r.TelegramUserId == 10 && r.GoogleUserId == 20),
                1,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _controller.MergeTelegramGoogle(req, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<ApiResponse<MergeTelegramGoogleUsersResponse>>(ok.Value);

        Assert.True(response.Success);
        Assert.True(response.Data!.DryRun);
        Assert.Equal(10, response.Data.SurvivorUserId);

        _userMergeServiceMock.VerifyAll();
    }
}
