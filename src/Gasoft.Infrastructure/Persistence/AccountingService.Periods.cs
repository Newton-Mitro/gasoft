using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class AccountingService
{
    public async Task<IReadOnlyList<AccountingPeriodDto>> GetPeriodsAsync(long companyId, CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        return await db.AccountingPeriods.AsNoTracking()
            .Where(period => period.CompanyId == companyId)
            .OrderByDescending(period => period.StartsOn)
            .Select(period => new AccountingPeriodDto(
                period.Id, period.FiscalYearId, period.StartsOn, period.EndsOn, period.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<AccountingPeriodDto>> GetPeriodsPageAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        ValidatePagination(page, pageSize);
        var periods = db.AccountingPeriods.AsNoTracking().Where(period => period.CompanyId == companyId);
        var totalCount = await periods.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var items = await periods
            .OrderByDescending(period => period.StartsOn)
            .ThenByDescending(period => period.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(period => new AccountingPeriodDto(
                period.Id, period.FiscalYearId, period.StartsOn, period.EndsOn, period.Status))
            .ToListAsync(cancellationToken);
        return new PagedResultDto<AccountingPeriodDto>(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    public async Task<AccountingPeriodDto?> GetPeriodAsync(
        long companyId,
        long periodId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var period = await db.AccountingPeriods.AsNoTracking().SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == periodId, cancellationToken);
        return period is null ? null : new AccountingPeriodDto(
            period.Id, period.FiscalYearId, period.StartsOn, period.EndsOn, period.Status);
    }

    public async Task<AccountingPeriodDto?> UpdatePeriodAsync(
        long companyId,
        long periodId,
        UpdateAccountingPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var period = await db.AccountingPeriods.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == periodId, cancellationToken);
        if (period is null) return null;
        if (period.Status != "Open")
            throw new AccountingConflictException("A closed accounting period cannot be edited.");
        var currentYear = await db.FiscalYears.SingleAsync(
            item => item.CompanyId == companyId && item.Id == period.FiscalYearId,
            cancellationToken);
        if (currentYear.Status != "Open")
            throw new AccountingConflictException("Accounting periods in a closed fiscal year cannot be edited.");
        if (await db.JournalEntries.AnyAsync(entry => entry.AccountingPeriodId == periodId, cancellationToken))
            throw new AccountingConflictException("An accounting period with journal entries cannot be edited.");
        var year = await db.FiscalYears.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == request.FiscalYearId, cancellationToken);
        if (year is null)
            throw Validation("fiscalYearId", "The fiscal year does not belong to this company.");
        if (year.Status != "Open")
            throw new AccountingConflictException("Accounting periods cannot be moved into a closed fiscal year.");
        ValidateDateRange(request.StartsOn, request.EndsOn, "Accounting period");
        if (request.StartsOn < year.StartsOn || request.EndsOn > year.EndsOn)
            throw Validation("startsOn", "The accounting period must be within its fiscal year.");
        if (await db.AccountingPeriods.AnyAsync(
                item => item.CompanyId == companyId && item.Id != periodId &&
                        item.FiscalYearId == year.Id &&
                        item.StartsOn <= request.EndsOn && item.EndsOn >= request.StartsOn,
                cancellationToken))
            throw Validation("startsOn", "Accounting periods in a fiscal year must not overlap.");
        period.FiscalYearId = year.Id;
        period.StartsOn = request.StartsOn;
        period.EndsOn = request.EndsOn;
        await SaveAsync(cancellationToken);
        return new AccountingPeriodDto(period.Id, period.FiscalYearId, period.StartsOn, period.EndsOn, period.Status);
    }

    public async Task<AccountingPeriodDto?> ClosePeriodAsync(
        long companyId,
        long periodId,
        ChangeAccountingPeriodStatusRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var userId = currentTenant.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required.");
        var reason = ValidatePeriodStatusReason(request.Reason);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var fiscalYearId = await db.AccountingPeriods
            .Where(period => period.CompanyId == companyId && period.Id == periodId)
            .Select(period => (long?)period.FiscalYearId)
            .SingleOrDefaultAsync(cancellationToken);
        if (!fiscalYearId.HasValue) return null;
        if (!await db.FiscalYears.AnyAsync(
                year => year.CompanyId == companyId &&
                        year.Id == fiscalYearId.Value &&
                        year.Status == "Open",
                cancellationToken))
            throw new AccountingConflictException("Reopen the fiscal year before reopening one of its accounting periods.");

        var changed = await db.AccountingPeriods
            .Where(period => period.CompanyId == companyId && period.Id == periodId && period.Status == "Open")
            .ExecuteUpdateAsync(setters => setters.SetProperty(period => period.Status, "Closed"), cancellationToken);
        if (changed == 0)
        {
            var exists = await db.AccountingPeriods.AnyAsync(
                period => period.CompanyId == companyId && period.Id == periodId,
                cancellationToken);
            if (!exists) return null;
            throw new AccountingConflictException("Only open accounting periods can be closed.");
        }

        var period = await db.AccountingPeriods.SingleAsync(
            item => item.CompanyId == companyId && item.Id == periodId,
            cancellationToken);
        var checklist = await BuildPeriodCloseChecklistAsync(period, cancellationToken);
        if (!checklist.CanClose)
            throw new AccountingConflictException(PeriodCloseBlockerMessage(checklist));

        db.AccountingPeriodStatusChanges.Add(new AccountingPeriodStatusChange
        {
            TenantId = tenantId,
            CompanyId = companyId,
            AccountingPeriodId = periodId,
            ChangedByUserId = userId,
            FromStatus = "Open",
            ToStatus = "Closed",
            Reason = reason,
            ChangedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AccountingPeriodDto(period.Id, period.FiscalYearId, period.StartsOn, period.EndsOn, period.Status);
    }

    public async Task<AccountingPeriodCloseChecklistDto?> GetPeriodCloseChecklistAsync(
        long companyId,
        long periodId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var period = await db.AccountingPeriods.AsNoTracking().SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == periodId,
            cancellationToken);
        return period is null ? null : await BuildPeriodCloseChecklistAsync(period, cancellationToken);
    }

    public async Task<AccountingPeriodDto?> ReopenPeriodAsync(
        long companyId,
        long periodId,
        ChangeAccountingPeriodStatusRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var userId = currentTenant.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required.");
        var reason = ValidatePeriodStatusReason(request.Reason);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var changed = await db.AccountingPeriods
            .Where(period => period.CompanyId == companyId && period.Id == periodId && period.Status == "Closed")
            .ExecuteUpdateAsync(setters => setters.SetProperty(period => period.Status, "Open"), cancellationToken);
        if (changed == 0)
        {
            var exists = await db.AccountingPeriods.AnyAsync(
                period => period.CompanyId == companyId && period.Id == periodId,
                cancellationToken);
            if (!exists) return null;
            throw new AccountingConflictException("Only closed accounting periods can be reopened.");
        }

        var period = await db.AccountingPeriods.SingleAsync(
            item => item.CompanyId == companyId && item.Id == periodId,
            cancellationToken);
        db.AccountingPeriodStatusChanges.Add(new AccountingPeriodStatusChange
        {
            TenantId = tenantId,
            CompanyId = companyId,
            AccountingPeriodId = periodId,
            ChangedByUserId = userId,
            FromStatus = "Closed",
            ToStatus = "Open",
            Reason = reason,
            ChangedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AccountingPeriodDto(period.Id, period.FiscalYearId, period.StartsOn, period.EndsOn, period.Status);
    }

    public async Task<IReadOnlyList<AccountingPeriodStatusChangeDto>?> GetPeriodStatusHistoryAsync(
        long companyId,
        long periodId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        if (!await db.AccountingPeriods.AnyAsync(
                period => period.CompanyId == companyId && period.Id == periodId,
                cancellationToken))
            return null;

        return await (
            from change in db.AccountingPeriodStatusChanges.AsNoTracking()
            join user in db.Users.AsNoTracking() on change.ChangedByUserId equals user.Id
            where change.CompanyId == companyId && change.AccountingPeriodId == periodId
            orderby change.ChangedAt descending, change.Id descending
            select new AccountingPeriodStatusChangeDto(
                change.Id,
                change.FromStatus,
                change.ToStatus,
                change.Reason,
                change.ChangedAt,
                change.ChangedByUserId,
                user.DisplayName ?? user.Email ?? user.Id.ToString()))
            .ToListAsync(cancellationToken);
    }
}
