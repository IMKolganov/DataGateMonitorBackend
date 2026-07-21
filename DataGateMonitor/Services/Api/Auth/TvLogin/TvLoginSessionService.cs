using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.TvLoginSessionTable;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Hubs;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.Login;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.Auth.Responses;

namespace DataGateMonitor.Services.Api.Auth.TvLogin;

public sealed class TvLoginSessionService(
    ITvLoginSessionQueryService sessionQuery,
    ICommandService<TvLoginSession, Guid> sessionCommand,
    IUserQueryService userQueryService,
    ITokenService tokenService,
    ITvLoginHubNotifier hubNotifier,
    IMemoryCache cache,
    IConfiguration configuration,
    IHttpContextAccessor httpContextAccessor,
    ILogger<TvLoginSessionService> logger) : ITvLoginSessionService
{
    public const string RateLimitMessage = "Too many requests. Try again later.";
    public const string SessionNotFoundMessage = "TV login session not found.";
    public const string SessionExpiredMessage = "TV login session has expired.";
    public const string SessionNotPendingMessage = "TV login session is no longer pending.";

    private const int DefaultTtlMinutes = 5;
    private const int PollIntervalSeconds = 2;
    private const int UserCodeLength = 6;
    private const int CreateRateLimitMax = 10;
    private const int CreateRateLimitWindowMinutes = 10;
    private const int PollRateLimitMax = 200;
    private const int PollRateLimitWindowMinutes = 10;
    private const int ApproveRateLimitMax = 30;
    private const int ApproveRateLimitWindowMinutes = 10;

    private static readonly ConcurrentDictionary<string, object> RateLimitLocks = new();

    public async Task<CreateTvLoginSessionResponse> CreateSessionAsync(
        CreateTvLoginSessionRequest request,
        string? clientIp,
        CancellationToken ct)
    {
        EnsureNotRateLimited($"tvlogin:rl:create:{IpKey(clientIp)}", CreateRateLimitMax, CreateRateLimitWindowMinutes);

        var ttlMinutes = configuration.GetValue<int?>("Auth:TvLoginSessionMinutes") ?? DefaultTtlMinutes;
        if (ttlMinutes <= 0)
            ttlMinutes = DefaultTtlMinutes;

        var (deviceId, userAgent) = GetClientInfo();
        var userCode = await GenerateUniqueUserCodeAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var session = new TvLoginSession
        {
            Id = Guid.NewGuid(),
            UserCode = userCode,
            Status = TvLoginSessionStatus.Pending,
            DeviceName = Truncate(request.DeviceName?.Trim(), 128),
            Client = Truncate(request.Client?.Trim(), 64),
            ExpiresAt = now.AddMinutes(ttlMinutes),
            DeviceId = Truncate(deviceId, 128),
            UserAgent = Truncate(userAgent, 512),
        };

        await sessionCommand.Add(session, ct: ct);

        var formattedCode = FormatUserCode(userCode);
        var baseUrl = GetPublicWebBaseUrl();
        var verificationUrl = $"{baseUrl}/tv/link";
        var qrPayload = $"{verificationUrl}?code={Uri.EscapeDataString(formattedCode)}";

        logger.LogInformation(
            "Created TV login session {SessionId} (device={DeviceName}, client={Client}, expires={ExpiresAt})",
            session.Id,
            session.DeviceName,
            session.Client,
            session.ExpiresAt);

        return new CreateTvLoginSessionResponse
        {
            SessionId = session.Id,
            UserCode = formattedCode,
            VerificationUrl = verificationUrl,
            QrPayload = qrPayload,
            ExpiresAt = session.ExpiresAt,
            PollIntervalSeconds = PollIntervalSeconds,
            SignalRHubPath = TvLoginHub.HubPath,
        };
    }

    public async Task<TvLoginSessionPollResponse> PollSessionAsync(
        Guid sessionId,
        string? clientIp,
        CancellationToken ct)
    {
        EnsureNotRateLimited($"tvlogin:rl:poll:{IpKey(clientIp)}", PollRateLimitMax, PollRateLimitWindowMinutes);

        var session = await sessionQuery.GetById(sessionId, ct)
                      ?? throw new InvalidOperationException(SessionNotFoundMessage);

        session = await EnsureNotStaleOpenAsync(session, ct);

        return session.Status switch
        {
            TvLoginSessionStatus.Pending => StatusOnly(session, "pending"),
            TvLoginSessionStatus.Viewed => StatusOnly(session, "viewed"),
            TvLoginSessionStatus.Denied => StatusOnly(session, "denied"),
            TvLoginSessionStatus.Expired => StatusOnly(session, "expired"),
            TvLoginSessionStatus.Consumed => StatusOnly(session, "consumed"),
            TvLoginSessionStatus.Approved => await DeliverTokensOnceAsync(session, ct),
            _ => StatusOnly(session, "expired"),
        };
    }

    public async Task<TvLoginSessionPreviewResponse> GetByUserCodeAsync(string userCode, CancellationToken ct)
    {
        var normalized = NormalizeUserCode(userCode);
        if (normalized.Length != UserCodeLength)
            throw new InvalidOperationException(SessionNotFoundMessage);

        var session = await sessionQuery.GetActiveByUserCode(normalized, ct);
        if (session is null)
        {
            var latest = await sessionQuery.GetLatestByUserCode(normalized, ct);
            if (latest is null)
                throw new InvalidOperationException(SessionNotFoundMessage);
            throw new InvalidOperationException(SessionExpiredMessage);
        }

        session = await EnsureNotStaleOpenAsync(session, ct);
        if (session.Status is not (TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed))
            throw new InvalidOperationException(SessionExpiredMessage);

        // Phone opened the link / entered the code — mark viewed so TV (hub or poll) can show "continue on phone".
        if (session.Status == TvLoginSessionStatus.Pending)
        {
            var now = DateTimeOffset.UtcNow;
            var affected = await sessionCommand.UpdateWhere(
                s => s.Id == session.Id && s.Status == TvLoginSessionStatus.Pending && s.ExpiresAt > now,
                set => set
                    .SetProperty(x => x.Status, TvLoginSessionStatus.Viewed)
                    .SetProperty(x => x.LastUpdate, now),
                ct);
            if (affected == 1)
            {
                session.Status = TvLoginSessionStatus.Viewed;
                await hubNotifier.NotifyStatusAsync(session.Id, "viewed", session.ExpiresAt, ct);
            }
        }

        return new TvLoginSessionPreviewResponse
        {
            SessionId = session.Id,
            UserCode = FormatUserCode(session.UserCode),
            DeviceName = session.DeviceName,
            ExpiresAt = session.ExpiresAt,
            // Phone approve UI always treats an open session as pending confirmation.
            Status = "pending",
        };
    }

    public async Task<TvLoginSessionActionResponse> ApproveAsync(
        ApproveTvLoginSessionRequest request,
        int approvingUserId,
        string? clientIp,
        CancellationToken ct)
    {
        EnsureNotRateLimited(
            $"tvlogin:rl:approve:{approvingUserId}:{IpKey(clientIp)}",
            ApproveRateLimitMax,
            ApproveRateLimitWindowMinutes);

        var session = await ResolveOpenSessionAsync(request.SessionId, request.UserCode, ct);
        var user = await userQueryService.GetById(approvingUserId, ct)
                   ?? throw new UnauthorizedAccessException("User not found.");

        if (user.IsBlocked)
            throw new UnauthorizedAccessException("User account is blocked.");

        var now = DateTimeOffset.UtcNow;
        var affected = await sessionCommand.UpdateWhere(
            s => s.Id == session.Id
                 && (s.Status == TvLoginSessionStatus.Pending || s.Status == TvLoginSessionStatus.Viewed)
                 && s.ExpiresAt > now,
            set => set
                .SetProperty(x => x.Status, TvLoginSessionStatus.Approved)
                .SetProperty(x => x.ApprovedUserId, approvingUserId)
                .SetProperty(x => x.CompletedAt, now)
                .SetProperty(x => x.LastUpdate, now),
            ct);

        if (affected != 1)
            throw new InvalidOperationException(SessionNotPendingMessage);

        logger.LogInformation(
            "TV login session {SessionId} approved by user {UserId}",
            session.Id,
            approvingUserId);

        await hubNotifier.NotifyStatusAsync(session.Id, "approved", session.ExpiresAt, ct);

        return new TvLoginSessionActionResponse { Status = "approved" };
    }

    public async Task<TvLoginSessionActionResponse> DenyAsync(
        DenyTvLoginSessionRequest request,
        int denyingUserId,
        string? clientIp,
        CancellationToken ct)
    {
        EnsureNotRateLimited(
            $"tvlogin:rl:deny:{denyingUserId}:{IpKey(clientIp)}",
            ApproveRateLimitMax,
            ApproveRateLimitWindowMinutes);

        var session = await ResolveOpenSessionAsync(request.SessionId, request.UserCode, ct);
        var now = DateTimeOffset.UtcNow;
        var affected = await sessionCommand.UpdateWhere(
            s => s.Id == session.Id
                 && (s.Status == TvLoginSessionStatus.Pending || s.Status == TvLoginSessionStatus.Viewed)
                 && s.ExpiresAt > now,
            set => set
                .SetProperty(x => x.Status, TvLoginSessionStatus.Denied)
                .SetProperty(x => x.CompletedAt, now)
                .SetProperty(x => x.LastUpdate, now),
            ct);

        if (affected != 1)
            throw new InvalidOperationException(SessionNotPendingMessage);

        logger.LogInformation(
            "TV login session {SessionId} denied by user {UserId}",
            session.Id,
            denyingUserId);

        await hubNotifier.NotifyStatusAsync(session.Id, "denied", session.ExpiresAt, ct);

        return new TvLoginSessionActionResponse { Status = "denied" };
    }

    private async Task<TvLoginSessionPollResponse> DeliverTokensOnceAsync(
        TvLoginSession session,
        CancellationToken ct)
    {
        if (session.ApprovedUserId is null)
        {
            logger.LogWarning("TV login session {SessionId} is approved but has no ApprovedUserId", session.Id);
            return StatusOnly(session, "expired");
        }

        var user = await userQueryService.GetById(session.ApprovedUserId.Value, ct);
        if (user is null || user.IsBlocked)
        {
            await MarkConsumedAsync(session.Id, ct);
            await hubNotifier.NotifyStatusAsync(session.Id, "consumed", session.ExpiresAt, ct);
            return StatusOnly(session, "expired");
        }

        var now = DateTimeOffset.UtcNow;
        var claimed = await sessionCommand.UpdateWhere(
            s => s.Id == session.Id && s.Status == TvLoginSessionStatus.Approved,
            set => set
                .SetProperty(x => x.Status, TvLoginSessionStatus.Consumed)
                .SetProperty(x => x.LastUpdate, now),
            ct);

        if (claimed != 1)
            return StatusOnly(session, "consumed");

        var tokenPair = await tokenService.IssueAsync(
            userId: user.Id,
            externalId: null,
            deviceId: session.DeviceId,
            userAgent: session.UserAgent ?? session.Client ?? "android-tv",
            ct: ct);

        logger.LogInformation(
            "TV login session {SessionId} tokens issued for user {UserId} (consumed)",
            session.Id,
            user.Id);

        await hubNotifier.NotifyStatusAsync(session.Id, "consumed", session.ExpiresAt, ct);

        return new TvLoginSessionPollResponse
        {
            Status = "approved",
            ExpiresAt = session.ExpiresAt,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Token = tokenPair.AccessToken,
            Expiration = tokenPair.AccessExpiresAt,
            RefreshToken = tokenPair.RefreshToken,
            RefreshExpiration = tokenPair.RefreshExpiresAt,
            RequiresTotp = false,
            LoginChallengeId = null,
            RequiresTotpSetup = false,
        };
    }

    private async Task MarkConsumedAsync(Guid sessionId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        await sessionCommand.UpdateWhere(
            s => s.Id == sessionId && s.Status == TvLoginSessionStatus.Approved,
            set => set
                .SetProperty(x => x.Status, TvLoginSessionStatus.Consumed)
                .SetProperty(x => x.LastUpdate, now),
            ct);
    }

    private async Task<TvLoginSession> ResolveOpenSessionAsync(
        Guid? sessionId,
        string? userCode,
        CancellationToken ct)
    {
        TvLoginSession? session = null;

        if (sessionId is { } id && id != Guid.Empty)
            session = await sessionQuery.GetById(id, ct);

        if (session is null && !string.IsNullOrWhiteSpace(userCode))
        {
            var normalized = NormalizeUserCode(userCode);
            if (normalized.Length == UserCodeLength)
                session = await sessionQuery.GetActiveByUserCode(normalized, ct)
                          ?? await sessionQuery.GetLatestByUserCode(normalized, ct);
        }

        if (session is null)
            throw new InvalidOperationException(SessionNotFoundMessage);

        session = await EnsureNotStaleOpenAsync(session, ct);

        if (session.Status is not (TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed))
            throw new InvalidOperationException(SessionNotPendingMessage);

        return session;
    }

    private async Task<TvLoginSession> EnsureNotStaleOpenAsync(TvLoginSession session, CancellationToken ct)
    {
        if (session.Status is not (TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed))
            return session;

        if (session.ExpiresAt > DateTimeOffset.UtcNow)
            return session;

        var now = DateTimeOffset.UtcNow;
        var previous = session.Status;
        await sessionCommand.UpdateWhere(
            s => s.Id == session.Id
                 && (s.Status == TvLoginSessionStatus.Pending || s.Status == TvLoginSessionStatus.Viewed),
            set => set
                .SetProperty(x => x.Status, TvLoginSessionStatus.Expired)
                .SetProperty(x => x.LastUpdate, now),
            ct);

        session.Status = TvLoginSessionStatus.Expired;
        if (previous is TvLoginSessionStatus.Pending or TvLoginSessionStatus.Viewed)
            await hubNotifier.NotifyStatusAsync(session.Id, "expired", session.ExpiresAt, ct);

        return session;
    }

    private async Task<string> GenerateUniqueUserCodeAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var code = GenerateRawUserCode();
            if (!await sessionQuery.AnyActiveByUserCode(code, ct))
                return code;
        }

        throw new InvalidOperationException("Unable to allocate a unique TV login code. Try again.");
    }

    private static string GenerateRawUserCode()
    {
        // Cryptographically random 6-digit code, including leading zeros (stored as string).
        var value = RandomNumberGenerator.GetInt32(0, 1_000_000);
        return value.ToString("D6");
    }

    internal static string NormalizeUserCode(string? userCode)
    {
        if (string.IsNullOrWhiteSpace(userCode))
            return string.Empty;

        var sb = new StringBuilder(UserCodeLength);
        foreach (var ch in userCode)
        {
            if (ch is '-' or ' ' or '\t')
                continue;
            if (ch is >= '0' and <= '9')
                sb.Append(ch);
        }

        return sb.ToString();
    }

    internal static string FormatUserCode(string normalized) => normalized;

    private string GetPublicWebBaseUrl()
    {
        var configured = configuration["Auth:PublicWebBaseUrl"]
                         ?? configuration["Frontend:BaseUrl"]
                         ?? "https://dash.datagateapp.com";
        return configured.TrimEnd('/');
    }

    private static TvLoginSessionPollResponse StatusOnly(TvLoginSession session, string status) => new()
    {
        Status = status,
        ExpiresAt = session.ExpiresAt,
    };

    private void EnsureNotRateLimited(string cacheKey, int maxRequests, int windowMinutes)
    {
        if (IsRateLimited(cacheKey, maxRequests, windowMinutes))
            throw new InvalidOperationException(RateLimitMessage);
        RecordRateLimit(cacheKey, windowMinutes);
    }

    private bool IsRateLimited(string cacheKey, int maxRequests, int windowMinutes)
    {
        var gate = RateLimitLocks.GetOrAdd(cacheKey, static _ => new object());
        lock (gate)
        {
            if (!cache.TryGetValue(cacheKey, out RateLimitEntry? entry) || entry is null)
                return false;
            return entry.Count >= maxRequests;
        }
    }

    private void RecordRateLimit(string cacheKey, int windowMinutes)
    {
        var gate = RateLimitLocks.GetOrAdd(cacheKey, static _ => new object());
        lock (gate)
        {
            if (!cache.TryGetValue(cacheKey, out RateLimitEntry? entry) || entry is null)
            {
                entry = new RateLimitEntry { Count = 1 };
                cache.Set(cacheKey, entry, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMinutes),
                });
                return;
            }

            entry.Count++;
            cache.Set(cacheKey, entry, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(windowMinutes),
            });
        }
    }

    private static string IpKey(string? clientIp) =>
        string.IsNullOrWhiteSpace(clientIp) ? "unknown" : clientIp.Trim();

    private (string? deviceId, string? userAgent) GetClientInfo()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx is null)
            return (null, null);
        var userAgent = ctx.Request.Headers.UserAgent.ToString();
        var deviceId = ctx.Request.Headers["X-Device-Id"].ToString();
        if (string.IsNullOrWhiteSpace(deviceId))
            deviceId = null;
        if (string.IsNullOrWhiteSpace(userAgent))
            userAgent = null;
        return (deviceId, userAgent);
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return value.Length <= max ? value : value[..max];
    }

    private sealed class RateLimitEntry
    {
        public int Count { get; set; }
    }
}
