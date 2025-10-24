namespace OpenVPNGateMonitor.Services.Auth.Models;

public sealed class UserAuthResult//todo: move to shared models
{
    public bool Ok { get; init; }
    public int UserId { get; init; }
    public string? Reason { get; init; }

    public static UserAuthResult Success(int userId) => new() { Ok = true, UserId = userId };
    public static UserAuthResult Fail(string reason) => new() { Ok = false, Reason = reason };
}