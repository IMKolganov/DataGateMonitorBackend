using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Requests;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses;
using DataGateMonitor.SharedModels.DataGateMonitor.EmailBroadcast.Responses.Dto;
using MapsterMapper;

namespace DataGateMonitor.Services.AdminEmail;

public sealed class AdminEmailTemplateService(
    IQueryService<EmailBroadcastTemplate, int> templateQuery,
    ICommandService<EmailBroadcastTemplate, int> templateCommand,
    IMapper mapper
) : IAdminEmailTemplateService
{
    private const int MaxHtmlLength = 512_000;

    public async Task<GetEmailTemplatesResponse> ListSummariesAsync(CancellationToken ct)
    {
        var list = await templateQuery.Where(
            predicate: _ => true,
            orderBy: q => q.OrderBy(t => t.Name),
            asNoTracking: true,
            ct: ct);

        return new GetEmailTemplatesResponse
        {
            Items = mapper.Map<List<EmailBroadcastTemplateSummaryDto>>(list)
        };
    }

    public async Task<EmailBroadcastTemplateDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await templateQuery.FindById(id, asNoTracking: true, ct: ct);
        return entity == null ? null : mapper.Map<EmailBroadcastTemplateDto>(entity);
    }

    public async Task<EmailBroadcastTemplateDto> CreateAsync(CreateEmailTemplateRequest request,
        int? createdByUserId, CancellationToken ct)
    {
        Validate(request.Name, request.Subject, request.HtmlBody);
        var name = request.Name.Trim();
        if (await templateQuery.Any(t => t.Name == name, ct))
            throw new ArgumentException($"A template named '{name}' already exists.");

        var now = DateTimeOffset.UtcNow;
        var entity = new EmailBroadcastTemplate
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Subject = request.Subject.Trim(),
            BodyHtml = request.HtmlBody,
            CreatedByUserId = createdByUserId,
            CreateDate = now,
            LastUpdate = now
        };

        entity = await templateCommand.Add(entity, saveChanges: true, ct);
        return mapper.Map<EmailBroadcastTemplateDto>(entity);
    }

    public async Task<EmailBroadcastTemplateDto> UpdateAsync(int id, UpdateEmailTemplateRequest request,
        CancellationToken ct)
    {
        Validate(request.Name, request.Subject, request.HtmlBody);
        var name = request.Name.Trim();
        if (await templateQuery.Any(t => t.Name == name && t.Id != id, ct))
            throw new ArgumentException($"Another template is already named '{name}'.");

        var entity = await templateQuery.FindById(id, asNoTracking: false, ct: ct)
                     ?? throw new KeyNotFoundException($"Template {id} not found.");

        entity.Name = name;
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.Subject = request.Subject.Trim();
        entity.BodyHtml = request.HtmlBody;
        entity.LastUpdate = DateTimeOffset.UtcNow;

        await templateCommand.Update(entity, saveChanges: true, ct);
        return mapper.Map<EmailBroadcastTemplateDto>(entity);
    }

    public async Task DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await templateQuery.FindById(id, asNoTracking: false, ct: ct)
                     ?? throw new KeyNotFoundException($"Template {id} not found.");
        await templateCommand.Delete(entity, saveChanges: true, ct);
    }

    private static void Validate(string name, string subject, string html)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required.");
        if (name.Trim().Length > 128)
            throw new ArgumentException("Name must be at most 128 characters.");
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required.");
        if (string.IsNullOrWhiteSpace(html))
            throw new ArgumentException("HTML body is required.");
        if (html.Length > MaxHtmlLength)
            throw new ArgumentException($"HTML body exceeds {MaxHtmlLength} characters.");
    }
}
