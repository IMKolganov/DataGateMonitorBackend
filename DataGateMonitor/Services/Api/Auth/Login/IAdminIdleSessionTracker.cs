namespace DataGateMonitor.Services.Api.Auth.Login;

/// <summary>Tracks last activity for administrator sessions (sliding idle timeout).</summary>
public interface IAdminIdleSessionTracker
{
    TimeSpan IdleTimeout { get; }

    void Touch(int userId);

    bool IsExpired(int userId);

    void Clear(int userId);
}
