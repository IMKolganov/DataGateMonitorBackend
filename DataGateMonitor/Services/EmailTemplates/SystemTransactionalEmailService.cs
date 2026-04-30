using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.Models;
using DataGateMonitor.Models.EmailTemplates;

namespace DataGateMonitor.Services.EmailTemplates;

public sealed class SystemTransactionalEmailService(IQueryService<EmailBroadcastTemplate, int> templateQuery)
    : ISystemTransactionalEmailService
{
    public async Task<(string Subject, string BodyHtml)> GetEmailConfirmationAsync(string code, int ttlMinutes,
        CancellationToken ct)
    {
        var entity = await templateQuery.FirstOrDefault(
            t => t.Name == SystemEmailTemplateNames.EmailConfirmation,
            orderBy: q => q.OrderBy(t => t.Id),
            asNoTracking: true,
            ct: ct);

        if (entity is { BodyHtml: { Length: > 0 } body })
        {
            var subject = string.IsNullOrWhiteSpace(entity.Subject)
                ? TransactionalEmailHtml.DefaultConfirmationSubject
                : entity.Subject.Trim();
            return (subject, TransactionalEmailHtml.ApplyConfirmationPlaceholders(body, code, ttlMinutes));
        }

        return (TransactionalEmailHtml.DefaultConfirmationSubject, TransactionalEmailHtml.BuildEmailConfirmation(code, ttlMinutes));
    }

    public async Task<(string Subject, string BodyHtml)> GetAdminPasswordResetAsync(string code, int ttlMinutes,
        CancellationToken ct)
    {
        var entity = await templateQuery.FirstOrDefault(
            t => t.Name == SystemEmailTemplateNames.AdminPasswordReset,
            orderBy: q => q.OrderBy(t => t.Id),
            asNoTracking: true,
            ct: ct);

        if (entity is { BodyHtml: { Length: > 0 } body })
        {
            var subject = string.IsNullOrWhiteSpace(entity.Subject)
                ? TransactionalEmailHtml.DefaultAdminPasswordResetSubject
                : entity.Subject.Trim();
            return (subject, TransactionalEmailHtml.ApplyPasswordResetPlaceholders(body, code, ttlMinutes));
        }

        return (TransactionalEmailHtml.DefaultAdminPasswordResetSubject,
            TransactionalEmailHtml.BuildAdminPasswordReset(code, ttlMinutes));
    }
}
