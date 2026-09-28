using DataGateMonitor.Controllers;
using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Enums;
using DataGateMonitor.SharedModels.Responses;
using Microsoft.AspNetCore.Mvc;

namespace DataGateMonitor.Tests.Controllers;

public class CertExpiryV2ControllerTests
{
    [Fact]
    public void GetRuns_ReturnsPagedResponse()
    {
        var store = new CertExpiryRunHistoryStore();
        for (var i = 0; i < 5; i++)
        {
            store.Save(new CertExpiryCheckRunResponse
            {
                RunId = Guid.NewGuid(),
                StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-i),
                Status = CertExpiryRunStatus.Completed,
                ScopeLabel = $"Run {i}",
                Summary = new()
            });
        }

        var controller = new CertExpiryV2Controller(store);
        var result = controller.GetRuns(new GetCertExpiryRunsV2Request { Page = 1, PageSize = 2 });

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<GetCertExpiryRunsV2Response>>(ok.Value);
        Assert.True(payload.Success);
        Assert.Equal(5, payload.Data!.Runs.TotalCount);
        Assert.Equal(2, payload.Data.Runs.Items.Count);
        Assert.Equal(1, payload.Data.Runs.Page);
        Assert.Equal(2, payload.Data.Runs.PageSize);
    }
}
