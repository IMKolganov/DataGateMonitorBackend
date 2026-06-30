using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Dto;
using DataGateMonitor.SharedModels.DataGateMonitor.CertExpiry.Responses;
using DataGateMonitor.SharedModels.Enums;

namespace DataGateMonitor.Services.CertExpiry;

internal static class CertExpiryRunMapper
{
    public static CertExpiryProfileOutcome ToProfileOutcome(CertExpiryCheckOutcome outcome) =>
        outcome switch
        {
            CertExpiryCheckOutcome.ExpiringSoon => CertExpiryProfileOutcome.ExpiringSoon,
            CertExpiryCheckOutcome.Expired => CertExpiryProfileOutcome.Expired,
            CertExpiryCheckOutcome.MissingOnNode => CertExpiryProfileOutcome.MissingOnNode,
            _ => CertExpiryProfileOutcome.Healthy
        };

    public static CertExpiryCheckSummaryDto BuildSummary(IEnumerable<CertExpiryServerResultDto> servers)
    {
        var serverList = servers.ToList();
        var profiles = serverList.SelectMany(s => s.Profiles).ToList();

        return new CertExpiryCheckSummaryDto
        {
            ServersChecked = serverList.Count(s => s.FetchStatus != CertExpiryServerFetchStatus.Skipped),
            ProfilesChecked = profiles.Count,
            Healthy = profiles.Count(p => p.Outcome == CertExpiryProfileOutcome.Healthy),
            ExpiringSoon = profiles.Count(p => p.Outcome == CertExpiryProfileOutcome.ExpiringSoon),
            Expired = profiles.Count(p => p.Outcome == CertExpiryProfileOutcome.Expired),
            MissingOnNode = profiles.Count(p => p.Outcome == CertExpiryProfileOutcome.MissingOnNode),
            ServerFailures = serverList.Count(s => s.FetchStatus == CertExpiryServerFetchStatus.Failed)
        };
    }

    public static CertExpiryRunSummaryDto ToSummary(CertExpiryCheckRunResponse run) =>
        new()
        {
            RunId = run.RunId,
            StartedAtUtc = run.StartedAtUtc,
            FinishedAtUtc = run.FinishedAtUtc,
            DurationMs = run.DurationMs,
            Status = run.Status,
            VpnServerId = run.VpnServerId,
            ScopeLabel = run.ScopeLabel,
            SendNotifications = run.SendNotifications,
            IsScheduled = run.IsScheduled,
            ServersChecked = run.Summary.ServersChecked,
            ProfilesChecked = run.Summary.ProfilesChecked,
            Expired = run.Summary.Expired,
            ExpiringSoon = run.Summary.ExpiringSoon,
            MissingOnNode = run.Summary.MissingOnNode,
            ServerFailures = run.Summary.ServerFailures,
            ErrorMessage = run.ErrorMessage
        };
}
