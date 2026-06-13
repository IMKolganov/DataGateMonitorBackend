namespace DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Requests;

public class SendAdminEmailRequest
{
    public string Subject { get; set; } = string.Empty;

    /// <summary>Full HTML body (sent as MIME HTML).</summary>
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>When set, send only to this dashboard user. When null, send to every user with a non-empty email.</summary>
    public int? TargetUserId { get; set; }
}
