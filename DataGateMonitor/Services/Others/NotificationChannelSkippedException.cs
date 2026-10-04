namespace DataGateMonitor.Services.Others;

/// <summary>
/// Delivery was skipped because the channel is not available for this recipient
/// (e.g. admin has no Telegram identity link). Not an unexpected failure.
/// </summary>
public sealed class NotificationChannelSkippedException(string message) : Exception(message);
