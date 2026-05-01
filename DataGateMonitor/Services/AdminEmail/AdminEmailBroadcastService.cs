using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.DataBase.Services.Query.UserTable;
using DataGateMonitor.Models;
using DataGateMonitor.Services.Api.Auth.EmailConfirmation;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;
using MapsterMapper;

namespace DataGateMonitor.Services.AdminEmail;

public sealed class AdminEmailBroadcastService(
    IEmailSenderService emailSender,
    ICommandService<SentEmailLog, int> sentEmailCommand,
    IQueryService<SentEmailLog, int> sentEmailQuery,
    IUserQueryService userQuery,
    IMapper mapper,
    ILogger<AdminEmailBroadcastService> logger
) : IAdminEmailBroadcastService
{
    private const int MaxHtmlLength = 512_000;
    private const int MaxPageSize = 100;

    public async Task<GetSentEmailHistoryResponse> GetHistoryAsync(GetSentEmailHistoryRequest request,
        CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, MaxPageSize);

        var paged = await sentEmailQuery.Page(
            page,
            pageSize,
            predicate: null,
            orderBy: q => q.OrderByDescending(x => x.CreateDate).ThenByDescending(x => x.Id),
            asNoTracking: true,
            ct: ct);

        var items = mapper.Map<List<SentEmailLogDto>>(paged.Items);

        return new GetSentEmailHistoryResponse
        {
            Page = paged.Page,
            PageSize = paged.PageSize,
            TotalCount = paged.TotalCount,
            Items = items
        };
    }

    public async Task<SendAdminEmailResponse> SendAsync(SendAdminEmailRequest request, int? sentByUserId,
        CancellationToken ct)
    {
        var subject = (request.Subject ?? string.Empty).Trim();
        var html = request.HtmlBody ?? string.Empty;
        if (string.IsNullOrEmpty(subject))
            throw new ArgumentException("Subject is required.");
        if (string.IsNullOrWhiteSpace(html))
            throw new ArgumentException("HTML body is required.");
        if (html.Length > MaxHtmlLength)
            throw new ArgumentException($"HTML body exceeds {MaxHtmlLength} characters.");

        List<(User? User, string Email)> recipients = new();

        var targetUserId = request.TargetUserId;
        if (targetUserId.HasValue && targetUserId.Value > 0)
        {
            var tid = targetUserId.Value;
            var user = await userQuery.GetById(tid, ct)
                       ?? throw new ArgumentException($"User {tid} not found.");
            if (string.IsNullOrWhiteSpace(user.Email))
                throw new ArgumentException("Target user has no email address.");
            recipients.Add((user, user.Email.Trim()));
        }
        else
        {
            var users = await userQuery.GetUsersWithNonEmptyEmailAsync(ct);
            foreach (var u in users)
            {
                if (string.IsNullOrWhiteSpace(u.Email))
                    continue;
                recipients.Add((u, u.Email.Trim()));
            }

            if (recipients.Count == 0)
                throw new ArgumentException("No users with an email address.");
        }

        var attempted = 0;
        var succeeded = 0;
        var failed = 0;

        foreach (var (user, email) in recipients)
        {
            attempted++;
            var now = DateTimeOffset.UtcNow;
            var log = new SentEmailLog
            {
                RecipientUserId = user?.Id,
                RecipientEmail = email,
                Subject = subject,
                BodyHtml = html,
                Success = false,
                ErrorMessage = null,
                SentByUserId = sentByUserId,
                CreateDate = now,
                LastUpdate = now
            };

            try
            {
                await emailSender.SendAsync(email, subject, html, ct);
                log.Success = true;
                succeeded++;
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (msg.Length > 4000)
                    msg = msg[..4000];
                log.ErrorMessage = msg;
                failed++;
                logger.LogWarning(ex, "Admin email send failed for {Email}", email);
            }

            await sentEmailCommand.Add(log, saveChanges: true, ct);
        }

        return new SendAdminEmailResponse
        {
            Attempted = attempted,
            Succeeded = succeeded,
            Failed = failed
        };
    }
}
