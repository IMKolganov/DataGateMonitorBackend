// using Mapster;
// using Microsoft.AspNetCore.Mvc;
// using Moq;
// using OpenVPNGateMonitor.Controllers;
// using OpenVPNGateMonitor.DataBase.Services.Query.OpenVpnServerEventLogTable;
// using OpenVPNGateMonitor.Mapping.OpenVpnServerEvent.Mappings;
// using OpenVPNGateMonitor.Models;
// using OpenVPNGateMonitor.Services.DataGateOpenVpnManager.Events;
// using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Dto;
// using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Requests;
// using OpenVPNGateMonitor.SharedModels.DataGateMonitorBackend.OpenVpnServerEvent.Responses;
// using OpenVPNGateMonitor.SharedModels.Responses;
//
// namespace OpenVPNGateMonitor.Tests.Controllers;
//
// public class OpenVpnServerEventControllerTests
// {
//     private readonly Mock<IOpenVpnServerEventLogQueryService> _logQuery = new();
//     private readonly Mock<IOpenVpnEventClientFactory> _factory = new();
//     private readonly OpenVpnServerEventController _controller;
//
//     public OpenVpnServerEventControllerTests()
//     {
//         TypeAdapterConfig.GlobalSettings.Scan(typeof(OpenVpnServerEventMapping).Assembly);
//
//         _controller = new OpenVpnServerEventController(_logQuery.Object, _factory.Object);
//     }
//
//     [Fact]
//     public void GetAllClientStatuses_ReturnsOk()
//     {
//         var statuses = new ConnectionStatusesResponse();
//
//         _factory
//             .Setup(f => f.GetAllClientStatuses())
//             .Returns(statuses);
//
//         var result = _controller.GetAllClientStatuses();
//
//         var ok = Assert.IsType<OkObjectResult>(result.Result);
//         var response = Assert.IsType<ApiResponse<ConnectionStatusesResponse>>(ok.Value);
//
//         Assert.True(response.Success);
//         Assert.Same(statuses, response.Data);
//
//         _factory.Verify(f => f.GetAllClientStatuses(), Times.Once);
//     }
//
//     [Fact]
//     public void GetClientStatus_ReturnsNotFound_WhenNoClient()
//     {
//         var request = new GetClientStatusRequest { VpnServerId = 123 };
//
//         ConnectionStatusResponse? status = null;
//
//         _factory
//             .Setup(f => f.TryGetClientStatus(request.VpnServerId, out status))
//             .Returns(false);
//
//         var result = _controller.GetClientStatus(request);
//
//         var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
//         var response = Assert.IsType<ApiResponse<ConnectionStatusResponse>>(notFound.Value);
//
//         Assert.False(response.Success);
//         Assert.Null(response.Data);
//
//         _factory.Verify(f => f.TryGetClientStatus(request.VpnServerId, out status), Times.Once);
//     }
//
//     [Fact]
//     public void GetClientStatus_ReturnsOk_WhenClientFound()
//     {
//         // Arrange
//         var request = new GetClientStatusRequest { VpnServerId = 555 };
//
//         var expected = new ConnectionStatusResponse
//         {
//             ConnectionStatus = new ConnectionStatusDto
//             {
//                 ServerId = 555,
//                 Url = "wss://vpn.example.com",
//                 Host = "vpn.example.com",
//                 Port = 443,
//                 ConnectionId = "conn-123",
//                 LastStateChangedUtc = DateTimeOffset.UtcNow
//             }
//         };
//
//         ConnectionStatusResponse? outValue = expected;
//
//         _factory
//             .Setup(f => f.TryGetClientStatus(request.VpnServerId, out outValue))
//             .Returns(true);
//
//         // Act
//         var result = _controller.GetClientStatus(request);
//
//         // Assert
//         var ok = Assert.IsType<OkObjectResult>(result.Result);
//         var response = Assert.IsType<ApiResponse<ConnectionStatusResponse>>(ok.Value);
//
//         Assert.True(response.Success);
//         Assert.NotNull(response.Data);
//
//         var dto = response.Data!.ConnectionStatus;
//
//         Assert.Equal(555, dto.ServerId);
//         Assert.Equal("vpn.example.com", dto.Host);
//         Assert.Equal(443, dto.Port);
//         Assert.Equal("conn-123", dto.ConnectionId);
//
//         _factory.Verify(
//             f => f.TryGetClientStatus(request.VpnServerId, out outValue),
//             Times.Once);
//     }
//
//     [Fact]
//     public async Task GetEventByVpnServerId_ReturnsOk_And_PassesParams()
//     {
//         var request = new GetVpnServerEventRequest
//         {
//             VpnServerId = 42,
//             Page = 2,
//             PageSize = 5
//         };
//
//         var page = new PagedResponse<OpenVpnServerEventLog>
//         {
//             Page = request.Page,
//             PageSize = request.PageSize,
//             TotalCount = 1,
//             Items = new List<OpenVpnServerEventLog> { new() }
//         };
//
//         _logQuery
//             .Setup(s => s.GetByVpnServerIdAsync(
//                 request.VpnServerId,
//                 request.Page,
//                 request.PageSize,
//                 It.IsAny<CancellationToken>()))
//             .ReturnsAsync(page);
//
//         var result = await _controller.GetEventByVpnServerId(request, CancellationToken.None);
//
//         var ok = Assert.IsType<OkObjectResult>(result.Result);
//         var response = Assert.IsType<ApiResponse<VpnServerEventResponse>>(ok.Value);
//
//         Assert.True(response.Success);
//         Assert.NotNull(response.Data);
//
//         var events = response.Data!.Events;
//         Assert.Equal(request.Page, events.Page);
//         Assert.Equal(request.PageSize, events.PageSize);
//         Assert.Equal(1, events.TotalCount);
//         Assert.Single(events.Items);
//
//         _logQuery.Verify(
//             s => s.GetByVpnServerIdAsync(42, 2, 5, It.IsAny<CancellationToken>()),
//             Times.Once);
//     }
//
//     [Fact]
//     public async Task GetEventByVpnServerId_Throws_On_ServiceException()
//     {
//         var request = new GetVpnServerEventRequest
//         {
//             VpnServerId = 7,
//             Page = 1,
//             PageSize = 10
//         };
//
//         _logQuery
//             .Setup(s => s.GetByVpnServerIdAsync(
//                 request.VpnServerId,
//                 request.Page,
//                 request.PageSize,
//                 It.IsAny<CancellationToken>()))
//             .ThrowsAsync(new Exception("err"));
//
//         await Assert.ThrowsAsync<Exception>(() =>
//             _controller.GetEventByVpnServerId(request, CancellationToken.None));
//     }
// }
