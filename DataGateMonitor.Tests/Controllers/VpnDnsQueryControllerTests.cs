using System.Security.Claims;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.VpnDnsQueryLogTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Responses;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace DataGateMonitor.Tests.Controllers;

public class VpnDnsQueryControllerTests
{
    [Fact]
    public async Task Search_ReturnsPagedResponse()
    {
        var queryService = new Mock<IVpnDnsQueryLogQueryService>();
        queryService.Setup(x => x.SearchAsync(It.IsAny<GetVpnDnsQueryRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResponse<VpnDnsQueryLog>
            {
                Page = 1,
                PageSize = 50,
                TotalCount = 1,
                Items =
                [
                    new VpnDnsQueryLog
                    {
                        Id = 5,
                        VpnServerId = 2,
                        PiHoleQueryId = 77,
                        ClientIp = "10.51.30.4",
                        Domain = "example.com",
                        Status = "FORWARDED",
                        QueriedAtUtc = DateTimeOffset.UtcNow,
                        CreateDate = DateTimeOffset.UtcNow,
                        LastUpdate = DateTimeOffset.UtcNow
                    }
                ]
            });

        var controller = new VpnDnsQueryController(queryService.Object);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim(ClaimTypes.Role, "Admin")],
                    "mock"))
            }
        };

        var result = await controller.Search(
            new GetVpnDnsQueryRequest { VpnServerId = 2, DomainContains = "example" },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<VpnDnsQueryPageResponse>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.Single(envelope.Data!.Items);
        Assert.Equal("example.com", envelope.Data.Items[0].Domain);
    }
}
