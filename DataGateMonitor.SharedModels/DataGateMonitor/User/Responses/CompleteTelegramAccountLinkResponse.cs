namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Responses;

public sealed class CompleteTelegramAccountLinkResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>Present when merge completed successfully.</summary>
    public MergeTelegramGoogleUsersResponse? Merge { get; set; }
}
