using DataGateMonitor.Services.CertExpiry;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Tests.Services.CertExpiry;

public class CertExpiryRunMapperTests
{
    [Fact]
    public void ToProfileOutcome_MapsAllOutcomes()
    {
        Assert.Equal(CertExpiryProfileOutcome.Healthy, CertExpiryRunMapper.ToProfileOutcome(CertExpiryCheckOutcome.None));
        Assert.Equal(CertExpiryProfileOutcome.ExpiringSoon, CertExpiryRunMapper.ToProfileOutcome(CertExpiryCheckOutcome.ExpiringSoon));
        Assert.Equal(CertExpiryProfileOutcome.Expired, CertExpiryRunMapper.ToProfileOutcome(CertExpiryCheckOutcome.Expired));
        Assert.Equal(CertExpiryProfileOutcome.MissingOnNode, CertExpiryRunMapper.ToProfileOutcome(CertExpiryCheckOutcome.MissingOnNode));
    }

    [Fact]
    public void BuildSummary_CountsProfilesAndServerFailures()
    {
        var summary = CertExpiryRunMapper.BuildSummary(
        [
            new CertExpiryServerResultDto
            {
                FetchStatus = CertExpiryServerFetchStatus.Success,
                Profiles =
                [
                    new CertExpiryProfileResultDto { Outcome = CertExpiryProfileOutcome.Healthy },
                    new CertExpiryProfileResultDto { Outcome = CertExpiryProfileOutcome.Expired }
                ]
            },
            new CertExpiryServerResultDto
            {
                FetchStatus = CertExpiryServerFetchStatus.Failed,
                Profiles = []
            }
        ]);

        Assert.Equal(2, summary.ServersChecked);
        Assert.Equal(2, summary.ProfilesChecked);
        Assert.Equal(1, summary.Healthy);
        Assert.Equal(1, summary.Expired);
        Assert.Equal(1, summary.ServerFailures);
    }

    [Fact]
    public void ToSummary_CopiesRunFields()
    {
        var runId = Guid.NewGuid();
        var run = new CertExpiryCheckRunResponse
        {
            RunId = runId,
            StartedAtUtc = DateTimeOffset.UtcNow,
            Status = CertExpiryRunStatus.Completed,
            ScopeLabel = "All eligible servers",
            Summary = new CertExpiryCheckSummaryDto
            {
                ServersChecked = 2,
                ProfilesChecked = 5,
                Expired = 1
            }
        };

        var summary = CertExpiryRunMapper.ToSummary(run);

        Assert.Equal(runId, summary.RunId);
        Assert.Equal(CertExpiryRunStatus.Completed, summary.Status);
        Assert.Equal("All eligible servers", summary.ScopeLabel);
        Assert.Equal(5, summary.ProfilesChecked);
        Assert.Equal(1, summary.Expired);
    }
}
