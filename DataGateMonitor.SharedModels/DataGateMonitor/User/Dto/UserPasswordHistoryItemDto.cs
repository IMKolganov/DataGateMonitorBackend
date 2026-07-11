namespace DataGateMonitor.SharedModels.DataGateMonitor.User.Dto;

public sealed class UserPasswordHistoryItemDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string PasswordAlgo { get; set; } = "AspNetCoreV3";
    public DateTimeOffset RecordedAtUtc { get; set; }
    public global::DataGateMonitor.SharedModels.DataGateMonitor.User.PasswordSetActorKind SetByActor { get; set; }
    public int? SetByUserId { get; set; }
    public string? SetByDisplayName { get; set; }
    public string? Reason { get; set; }
    /// <summary>True when this hash is the one currently on the credential.</summary>
    public bool IsCurrent { get; set; }
}
