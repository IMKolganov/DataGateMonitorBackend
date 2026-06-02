namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

public class MergeTelegramGoogleUsersResponse
{
    public bool DryRun { get; set; }

    public int SurvivorUserId { get; set; }

    public int MergedUserId { get; set; }

    public int? ArchiveRecordId { get; set; }

    public string TelegramExternalId { get; set; } = string.Empty;

    public string GoogleExternalId { get; set; } = string.Empty;

    public MergeUserStatsDto Stats { get; set; } = new();

    public List<string> Warnings { get; set; } = [];
}

public class MergeUserStatsDto
{
    public int IdentityLinksReassigned { get; set; }
    public int IssuedOvpnFilesExternalIdUpdated { get; set; }
    public int IssuedXrayClientLinksExternalIdUpdated { get; set; }
    public int VpnServerClientsUserIdUpdated { get; set; }
    public int VpnServerClientsExternalIdUpdated { get; set; }
    public int VpnServerClientTrafficsUserIdUpdated { get; set; }
    public int VpnServerClientTrafficsExternalIdUpdated { get; set; }
    public int VpnServerClientTrafficDailiesUserIdUpdated { get; set; }
    public int VpnServerClientTrafficDailiesExternalIdUpdated { get; set; }
    public int UserCredentialsReassigned { get; set; }
    public int UserCredentialsRemoved { get; set; }
    public int UserRefreshTokensRemoved { get; set; }
    public int UserQuotaPlansReassigned { get; set; }
    public int UserQuotaPlansClosed { get; set; }
    public int UserRolesReassigned { get; set; }
    public int UserRolesRemoved { get; set; }
    public int DevicesReassigned { get; set; }
    public int NotificationsActorUserIdUpdated { get; set; }
    public int NotificationRecipientsAdminUserIdUpdated { get; set; }
    public int SentEmailLogsUpdated { get; set; }
    public int EmailBroadcastTemplatesUpdated { get; set; }
}
