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
    public async Task<IReadOnlyList<JournalDto>> GetJournalsAsync(long companyId, CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        return await db.Journals.AsNoTracking()
            .Where(journal => journal.CompanyId == companyId)
            .OrderBy(journal => journal.Code)
            .Select(journal => new JournalDto(
                journal.Id, journal.CompanyId, journal.Code, journal.Name, journal.JournalType, journal.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<JournalDto>> GetJournalsPageAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        ValidatePagination(page, pageSize);
        var journals = db.Journals.AsNoTracking().Where(journal => journal.CompanyId == companyId);
        var totalCount = await journals.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var items = await journals
            .OrderBy(journal => journal.Code)
            .ThenBy(journal => journal.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(journal => new JournalDto(
                journal.Id, journal.CompanyId, journal.Code, journal.Name, journal.JournalType, journal.IsActive))
            .ToListAsync(cancellationToken);
        return new PagedResultDto<JournalDto>(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    public async Task<JournalDto?> GetJournalAsync(
        long companyId,
        long journalId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var journal = await db.Journals.AsNoTracking().SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == journalId, cancellationToken);
        return journal is null ? null : ToDto(journal);
    }

    public async Task<JournalDto?> UpdateJournalAsync(
        long companyId,
        long journalId,
        UpdateJournalRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var journal = await db.Journals.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == journalId, cancellationToken);
        if (journal is null) return null;
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        var type = Normalize(request.JournalType);
        var errors = new Dictionary<string, string[]>();
        ValidateCode(errors, "code", code, 30);
        ValidateName(errors, "name", name, 120);
        if (!JournalTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
            errors["journalType"] = ["Journal type must be General, Sales, Purchase, Bank, or Cash."];
        ThrowIfInvalid(errors);
        var hasEntries = await db.JournalEntries.AnyAsync(
            entry => entry.CompanyId == companyId && entry.JournalId == journalId, cancellationToken);
        if (hasEntries && (journal.Code != code || journal.Name != name ||
                           !journal.JournalType.Equals(type, StringComparison.OrdinalIgnoreCase)))
            throw new AccountingConflictException("Journal details cannot change after journal entries have been recorded.");
        journal.Code = code;
        journal.Name = name;
        journal.JournalType = JournalTypes.Single(candidate => candidate.Equals(type, StringComparison.OrdinalIgnoreCase));
        journal.IsActive = request.IsActive;
        await SaveAsync(cancellationToken);
        return ToDto(journal);
    }

    public async Task<AccountDto> CreateAccountAsync(
        long companyId,
        CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        var errors = new Dictionary<string, string[]>();
        ValidateCode(errors, "code", code, 40);
        ValidateName(errors, "name", name, 160);
        ThrowIfInvalid(errors);

        var groupExists = await db.AccountGroups.AnyAsync(
            group => group.CompanyId == companyId && group.Id == request.AccountGroupId,
            cancellationToken);
        if (!groupExists)
        {
            throw Validation("accountGroupId", "The account group does not belong to this company.");
        }

        if (request.ParentId is not null)
        {
            var parent = await db.ChartOfAccounts.SingleOrDefaultAsync(
                account => account.CompanyId == companyId && account.Id == request.ParentId,
                cancellationToken);
            if (parent is null || parent.AllowPosting || parent.AccountGroupId != request.AccountGroupId)
            {
                throw Validation("parentId", "A parent must be a non-posting account in the same account group.");
            }
        }

        var account = new ChartOfAccount
        {
            TenantId = tenantId,
            CompanyId = companyId,
            AccountGroupId = request.AccountGroupId,
            ParentId = request.ParentId,
            Code = code,
            Name = name,
            AllowPosting = request.AllowPosting
        };
        db.ChartOfAccounts.Add(account);
        await SaveAsync(cancellationToken);
        return ToDto(account);
    }

    public async Task<AccountDto?> UpdateAccountAsync(
        long companyId,
        long accountId,
        UpdateAccountRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var account = await db.ChartOfAccounts.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == accountId,
            cancellationToken);
        if (account is null) return null;

        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        var errors = new Dictionary<string, string[]>();
        ValidateCode(errors, "code", code, 40);
        ValidateName(errors, "name", name, 160);
        ThrowIfInvalid(errors);

        var groupExists = await db.AccountGroups.AnyAsync(
            group => group.CompanyId == companyId && group.Id == request.AccountGroupId,
            cancellationToken);
        if (!groupExists)
        {
            throw Validation("accountGroupId", "The account group does not belong to this company.");
        }

        ChartOfAccount? parentToDemote = null;
        if (request.ParentId is not null)
        {
            if (request.ParentId == accountId)
            {
                throw Validation("parentId", "An account cannot be its own parent.");
            }

            var parent = await db.ChartOfAccounts.SingleOrDefaultAsync(
                item => item.CompanyId == companyId && item.Id == request.ParentId,
                cancellationToken);
            if (parent is null || parent.AccountGroupId != request.AccountGroupId)
            {
                throw Validation("parentId", "A parent must belong to the same account group.");
            }
            if (parent.AllowPosting && !request.DisableParentPosting)
            {
                throw Validation("parentId", "A parent must be a non-posting account.");
            }
            parentToDemote = parent;

            var descendantIds = new HashSet<long>();
            var parentIds = new[] { accountId };
            while (parentIds.Length > 0)
            {
                var childIds = await db.ChartOfAccounts.AsNoTracking()
                    .Where(item => item.CompanyId == companyId &&
                        item.ParentId.HasValue && parentIds.Contains(item.ParentId.Value))
                    .Select(item => item.Id)
                    .ToListAsync(cancellationToken);
                parentIds = childIds.Where(descendantIds.Add).ToArray();
            }

            if (descendantIds.Contains(parent.Id))
            {
                throw Validation("parentId", "An account cannot be moved beneath one of its descendants.");
            }
        }

        if (request.AllowPosting && await db.ChartOfAccounts.AnyAsync(
                item => item.CompanyId == companyId && item.ParentId == accountId,
                cancellationToken))
        {
            throw Validation("allowPosting", "An account with child accounts cannot allow direct posting.");
        }

        if (account.AccountGroupId != request.AccountGroupId)
        {
            var descendants = new List<ChartOfAccount>();
            var visitedIds = new HashSet<long> { accountId };
            var parentIds = new[] { accountId };
            while (parentIds.Length > 0)
            {
                var children = await db.ChartOfAccounts
                    .Where(item => item.CompanyId == companyId &&
                        item.ParentId.HasValue && parentIds.Contains(item.ParentId.Value))
                    .ToListAsync(cancellationToken);
                var newChildren = children.Where(child => visitedIds.Add(child.Id)).ToList();
                descendants.AddRange(newChildren);
                parentIds = newChildren.Select(child => child.Id).ToArray();
            }

            foreach (var descendant in descendants)
            {
                descendant.AccountGroupId = request.AccountGroupId;
            }
        }

        if (parentToDemote is not null && request.DisableParentPosting)
        {
            parentToDemote.AllowPosting = false;
        }
        account.AccountGroupId = request.AccountGroupId;
        account.ParentId = request.ParentId;
        account.Code = code;
        account.Name = name;
        account.AllowPosting = request.AllowPosting;
        account.IsActive = request.IsActive;
        await SaveAsync(cancellationToken);
        return ToDto(account);
    }

    public async Task<FiscalYearDto> CreateFiscalYearAsync(
        long companyId,
        CreateFiscalYearRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        ValidateDateRange(request.StartsOn, request.EndsOn, "Fiscal year");
        var overlaps = await db.FiscalYears.AnyAsync(
            year => year.CompanyId == companyId &&
                    year.StartsOn <= request.EndsOn &&
                    year.EndsOn >= request.StartsOn,
            cancellationToken);
        if (overlaps)
        {
            throw Validation("startsOn", "Fiscal years for a company must not overlap.");
        }

        var year = new FiscalYear
        {
            TenantId = tenantId,
            CompanyId = companyId,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn
        };
        db.FiscalYears.Add(year);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await SaveAsync(cancellationToken);

        var periodStart = year.StartsOn;
        var periodNumber = 0;
        while (true)
        {
            var periodEnd = periodStart.Year == year.EndsOn.Year && periodStart.Month == year.EndsOn.Month
                ? year.EndsOn
                : year.StartsOn.AddMonths(++periodNumber).AddDays(-1);
            if (periodEnd > year.EndsOn)
                periodEnd = year.EndsOn;

            db.AccountingPeriods.Add(new AccountingPeriod
            {
                TenantId = tenantId,
                CompanyId = companyId,
                FiscalYearId = year.Id,
                StartsOn = periodStart,
                EndsOn = periodEnd
            });

            if (periodEnd == year.EndsOn)
                break;
            periodStart = periodEnd.AddDays(1);
        }

        await SaveAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new FiscalYearDto(year.Id, companyId, year.StartsOn, year.EndsOn, year.Status);
    }

    public async Task<AccountingPeriodDto> CreatePeriodAsync(
        long companyId,
        CreateAccountingPeriodRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var year = await db.FiscalYears.SingleOrDefaultAsync(
            fiscalYear => fiscalYear.CompanyId == companyId && fiscalYear.Id == request.FiscalYearId,
            cancellationToken);
        if (year is null)
        {
            throw Validation("fiscalYearId", "The fiscal year does not belong to this company.");
        }
        if (year.Status != "Open")
            throw new AccountingConflictException("Accounting periods cannot be added to a closed fiscal year.");

        ValidateDateRange(request.StartsOn, request.EndsOn, "Accounting period");
        if (request.StartsOn < year.StartsOn || request.EndsOn > year.EndsOn)
        {
            throw Validation("startsOn", "The accounting period must be within its fiscal year.");
        }

        var overlaps = await db.AccountingPeriods.AnyAsync(
            period => period.CompanyId == companyId &&
                      period.FiscalYearId == year.Id &&
                      period.StartsOn <= request.EndsOn &&
                      period.EndsOn >= request.StartsOn,
            cancellationToken);
        if (overlaps)
        {
            throw Validation("startsOn", "Accounting periods in a fiscal year must not overlap.");
        }

        var period = new AccountingPeriod
        {
            TenantId = tenantId,
            CompanyId = companyId,
            FiscalYearId = year.Id,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn
        };
        db.AccountingPeriods.Add(period);
        await SaveAsync(cancellationToken);
        return new AccountingPeriodDto(period.Id, period.FiscalYearId, period.StartsOn, period.EndsOn, period.Status);
    }

    public async Task<JournalDto> CreateJournalAsync(
        long companyId,
        CreateJournalRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        var type = Normalize(request.JournalType);
        var errors = new Dictionary<string, string[]>();
        ValidateCode(errors, "code", code, 30);
        ValidateName(errors, "name", name, 120);
        if (!JournalTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            errors["journalType"] = ["Journal type must be General, Sales, Purchase, Bank, or Cash."];
        }

        ThrowIfInvalid(errors);
        var journal = new Journal
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            Name = name,
            JournalType = JournalTypes.Single(candidate => candidate.Equals(type, StringComparison.OrdinalIgnoreCase))
        };
        db.Journals.Add(journal);
        await SaveAsync(cancellationToken);
        return ToDto(journal);
    }
}
