using System.Security.Claims;
using DataGateMonitor.Controllers;
using DataGateMonitor.DataBase.Services.Query.IssuedOvpnFileTable;
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
        queryService.Setup(x => x.SearchAsync(
                It.IsAny<GetVpnDnsQueryRequest>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<IReadOnlyList<string>>()))
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

        var issued = new Mock<IIssuedOvpnFileQueryService>();
        var controller = new VpnDnsQueryController(queryService.Object, issued.Object);
        controller.ControllerContext = AdminContext();

        var result = await controller.Search(
            new GetVpnDnsQueryRequest { VpnServerId = 2, DomainContains = "example" },
            matchUserProfiles: false,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<VpnDnsQueryPageResponse>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.Single(envelope.Data!.Items);
        Assert.Equal("example.com", envelope.Data.Items[0].Domain);
    }

    [Fact]
    public async Task Search_WithMatchUserProfiles_ResolvesCommonNames()
    {
        var queryService = new Mock<IVpnDnsQueryLogQueryService>();
        queryService.Setup(x => x.SearchAsync(
                It.IsAny<GetVpnDnsQueryRequest>(),
                It.IsAny<CancellationToken>(),
                It.Is<IReadOnlyList<string>?>(cns => cns != null && cns.Contains("cn-1"))))
            .ReturnsAsync(new PagedResponse<VpnDnsQueryLog> { Page = 1, PageSize = 50, TotalCount = 0, Items = [] });

        var issued = new Mock<IIssuedOvpnFileQueryService>();
        issued.Setup(x => x.GetAllByExternalId("ext-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync([new IssuedOvpnFile { CommonName = "cn-1", VpnServerId = 75, ExternalId = "ext-1" }]);

        var controller = new VpnDnsQueryController(queryService.Object, issued.Object);
        controller.ControllerContext = AdminContext();

        await controller.Search(
            new GetVpnDnsQueryRequest { ExternalId = "ext-1" },
            matchUserProfiles: true,
            CancellationToken.None);

        queryService.Verify(x => x.SearchAsync(
            It.IsAny<GetVpnDnsQueryRequest>(),
            It.IsAny<CancellationToken>(),
            It.Is<IReadOnlyList<string>?>(cns => cns != null && cns.Contains("cn-1"))), Times.Once);
    }

    [Fact]
    public async Task TopDomains_ReturnsAggregatedRows()
    {
        var queryService = new Mock<IVpnDnsQueryLogQueryService>();
        queryService.Setup(x => x.GetTopDomainsAsync(It.IsAny<GetVpnDnsTopDomainsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataGateMonitor.SharedModels.DataGateMonitor.VpnDnsQuery.Dto.VpnDnsTopDomainDto>
            {
                new()
                {
                    Domain = "netflix.com",
                    UniqueUsersCount = 12,
                    QueryCount = 340
                }
            });

        var issued = new Mock<IIssuedOvpnFileQueryService>();
        var controller = new VpnDnsQueryController(queryService.Object, issued.Object);
        controller.ControllerContext = AdminContext();

        var result = await controller.TopDomains(
            new GetVpnDnsTopDomainsRequest { Limit = 100 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var envelope = Assert.IsType<ApiResponse<VpnDnsTopDomainsResponse>>(ok.Value);
        Assert.True(envelope.Success);
        Assert.Single(envelope.Data!.Items);
        Assert.Equal(12, envelope.Data.Items[0].UniqueUsersCount);
    }

    private static ControllerContext AdminContext() => new()
    {
        HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Role, "Admin")],
                "mock"))
        }
    };
}
