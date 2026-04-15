using System.ComponentModel.DataAnnotations;
using DataGateMonitor.DataBase.Services.Command;
using DataGateMonitor.DataBase.Services.Command.Interfaces;
using DataGateMonitor.DataBase.Services.Query.QuotaPlanTable;
using DataGateMonitor.Models;
using DataGateMonitor.SharedModels.Responses;

namespace DataGateMonitor.Services.QuotaPlans;

/// <summary>
/// Application-level service for managing quota plans.
/// Wraps query/command services and adds validation + convenience helpers.
/// </summary>
public class QuotaPlanService(
    ILogger<QuotaPlanService> logger,
    ICommandService<QuotaPlan, int> quotaPlanCommandService,
    IQuotaPlanQueryService quotaPlanQueryService) : IQuotaPlanService
{
    // ---------- Read ----------

    public Task<List<QuotaPlan>> GetAllAsync(CancellationToken ct = default) =>
        quotaPlanQueryService.GetAll(ct);

    public Task<QuotaPlan?> GetByIdAsync(int id, CancellationToken ct = default) =>
        quotaPlanQueryService.GetById(id, ct);

    public Task<IPagedResult<QuotaPlan>> GetPageAsync(int page, int pageSize, CancellationToken ct = default) =>
        quotaPlanQueryService.GetPage(page, pageSize, ct);

    public async Task<QuotaPlan?> GetDefaultAsync(CancellationToken ct = default)
    {
        var all = await quotaPlanQueryService.GetAll(ct);
        return all.FirstOrDefault(x => x.IsDefault);
    }

    // ---------- Create / Update / Delete ----------

    public async Task<QuotaPlan> CreateAsync(QuotaPlan input, bool makeDefault = false, CancellationToken ct = default)
    {
        Validate(input);

        // Ensure new entity
        input.Id = 0;

        if (makeDefault || input.IsDefault)
        {
            await UnsetDefaultForAllAsync(ct);
            input.IsDefault = true;
        }

        var created = await quotaPlanCommandService.Add(input, saveChanges: true, ct);
        logger.LogInformation("QuotaPlan created: {Id} ({Name})", created.Id, created.Name);
        return created;
    }

    public async Task<int> UpdateAsync(QuotaPlan input, CancellationToken ct = default)
    {
        if (input.Id <= 0) throw new ValidationException("Id must be a positive value.");
        Validate(input);

        // If toggled to default, clear previous default(s)
        if (input.IsDefault)
        {
            await UnsetDefaultForAllAsync(ct);
            input.IsDefault = true;
        }

        var affected = await quotaPlanCommandService.Update(input, saveChanges: true, ct);
        logger.LogInformation("QuotaPlan updated: {Id} ({Name}), rows: {Rows}", input.Id, input.Name, affected);
        return affected;
    }

    public Task<int> DeleteAsync(int id, CancellationToken ct = default) =>
        quotaPlanCommandService.DeleteById(id, ct);

    // ---------- State toggles ----------

    public async Task<int> ActivateAsync(int id, CancellationToken ct = default)
    {
        var rows = await quotaPlanCommandService.UpdateWhere(
            p => p.Id == id,
            set => set.SetProperty(x => x.IsActive, true),
            ct);
        logger.LogInformation("QuotaPlan activated: {Id}, rows: {Rows}", id, rows);
        return rows;
    }

    public async Task<int> DeactivateAsync(int id, CancellationToken ct = default)
    {
        var rows = await quotaPlanCommandService.UpdateWhere(
            p => p.Id == id,
            set => set.SetProperty(x => x.IsActive, false),
            ct);
        logger.LogInformation("QuotaPlan deactivated: {Id}, rows: {Rows}", id, rows);
        return rows;
    }

    // ---------- Default plan helpers ----------

    /// <summary>
    /// Sets the provided plan as the only default plan.
    /// </summary>
    public async Task SetDefaultAsync(int id, CancellationToken ct = default)
    {
        // Unset all defaults
        await UnsetDefaultForAllAsync(ct);

        // Set target plan as default
        var rows = await quotaPlanCommandService.UpdateWhere(
            p => p.Id == id,
            set => set.SetProperty(x => x.IsDefault, true),
            ct);

        if (rows == 0)
            throw new KeyNotFoundException($"QuotaPlan not found: {id}");

        logger.LogInformation("QuotaPlan set as default: {Id}", id);
    }

    /// <summary>
    /// Ensures there is at most one default plan by clearing IsDefault for all.
    /// </summary>
    private Task<int> UnsetDefaultForAllAsync(CancellationToken ct)
    {
        return quotaPlanCommandService.UpdateWhere(
            p => p.IsDefault,
            set => set.SetProperty(x => x.IsDefault, false),
            ct);
    }

    // ---------- Validation ----------

    private static void Validate(QuotaPlan plan)
    {
        if (string.IsNullOrWhiteSpace(plan.Name))
            throw new ValidationException("Name is required.");

        if (plan.Name.Length > 64)
            throw new ValidationException("Name length must be ≤ 64.");

        if (plan.Description != null && plan.Description.Length > 256)
            throw new ValidationException("Description length must be ≤ 256.");

        // Bytes: must be null or >= 0 (use long? in the model for bytes)
        if (plan.DailyQuotaBytes is < 0)
            throw new ValidationException("DailyQuotaBytes must be null or non-negative.");

        if (plan.MonthlyQuotaBytes is < 0)
            throw new ValidationException("MonthlyQuotaBytes must be null or non-negative.");

        // Speeds: must be null or >= 0
        if (plan.UpKbps is < 0)
            throw new ValidationException("UpKbps must be null or non-negative.");

        if (plan.DownKbps is < 0)
            throw new ValidationException("DownKbps must be null or non-negative.");

        if (plan.ThrottleUpKbps is < 0)
            throw new ValidationException("ThrottleUpKbps must be null or non-negative.");

        if (plan.ThrottleDownKbps is < 0)
            throw new ValidationException("ThrottleDownKbps must be null or non-negative.");
    }
}
