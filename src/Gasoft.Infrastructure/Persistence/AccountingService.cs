using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class AccountingService(AppDbContext db, ICurrentTenant currentTenant) : IAccountingService
{
    private static readonly string[] AccountTypes = ["Asset", "Liability", "Equity", "Revenue", "Expense"];
    private static readonly string[] JournalTypes = ["General", "Sales", "Purchase", "Bank", "Cash"];
    private const decimal MaximumAmount = 99_999_999_999_999.999999m;

    public async Task<IReadOnlyList<AccountGroupDto>> GetAccountGroupsAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        return await db.AccountGroups.AsNoTracking()
            .Where(group => group.CompanyId == companyId)
            .OrderBy(group => group.Code)
            .Select(group => new AccountGroupDto(
                group.Id, group.CompanyId, group.Code, group.Name, group.AccountType, group.NormalBalance))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedAccountGroupsDto> GetAccountGroupsPageAsync(
        long companyId,
        int page,
        int pageSize,
        string? search,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw Validation("pagination", "Page must be positive and page size must be between 1 and 100.");
        }

        var groups = db.AccountGroups.AsNoTracking().Where(group => group.CompanyId == companyId);
        var normalizedSearch = Normalize(search).ToLowerInvariant();
        if (normalizedSearch.Length > 0)
        {
            groups = groups.Where(group =>
                group.Code.ToLower().Contains(normalizedSearch) ||
                group.Name.ToLower().Contains(normalizedSearch) ||
                group.AccountType.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await groups.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var items = await groups
            .OrderBy(group => group.Code)
            .ThenBy(group => group.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(group => new AccountGroupDto(
                group.Id, group.CompanyId, group.Code, group.Name, group.AccountType, group.NormalBalance))
            .ToListAsync(cancellationToken);
        return new PagedAccountGroupsDto(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    public async Task<AccountGroupDto?> GetAccountGroupAsync(
        long companyId,
        long groupId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var group = await db.AccountGroups.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == groupId, cancellationToken);
        return group is null ? null : ToDto(group);
    }

    public async Task<AccountGroupDto> CreateAccountGroupAsync(
        long companyId,
        CreateAccountGroupRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        var type = Normalize(request.AccountType);
        var errors = new Dictionary<string, string[]>();
        ValidateCode(errors, "code", code, 30);
        ValidateName(errors, "name", name, 120);
        if (!AccountTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            errors["accountType"] = ["Account type must be Asset, Liability, Equity, Revenue, or Expense."];
        }

        ThrowIfInvalid(errors);
        var normalizedType = AccountTypes.Single(candidate => candidate.Equals(type, StringComparison.OrdinalIgnoreCase));
        var group = new AccountGroup
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = code,
            Name = name,
            AccountType = normalizedType,
            NormalBalance = normalizedType is "Asset" or "Expense" ? "Debit" : "Credit"
        };
        db.AccountGroups.Add(group);
        await SaveAsync(cancellationToken);
        return ToDto(group);
    }

    public async Task<AccountGroupDto?> UpdateAccountGroupAsync(
        long companyId,
        long groupId,
        UpdateAccountGroupRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var group = await db.AccountGroups.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == groupId,
            cancellationToken);
        if (group is null) return null;

        var code = Normalize(request.Code).ToUpperInvariant();
        var name = Normalize(request.Name);
        var type = Normalize(request.AccountType);
        var errors = new Dictionary<string, string[]>();
        ValidateCode(errors, "code", code, 30);
        ValidateName(errors, "name", name, 120);
        if (!AccountTypes.Contains(type, StringComparer.OrdinalIgnoreCase))
        {
            errors["accountType"] = ["Account type must be Asset, Liability, Equity, Revenue, or Expense."];
        }
        ThrowIfInvalid(errors);

        var normalizedType = AccountTypes.Single(candidate => candidate.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (normalizedType != group.AccountType &&
            await db.ChartOfAccounts.AnyAsync(
                account => account.CompanyId == companyId && account.AccountGroupId == groupId,
                cancellationToken))
        {
            throw Validation("accountType", "Account type cannot be changed while accounts use this group.");
        }

        group.Code = code;
        group.Name = name;
        group.AccountType = normalizedType;
        group.NormalBalance = normalizedType is "Asset" or "Expense" ? "Debit" : "Credit";
        await SaveAsync(cancellationToken);
        return ToDto(group);
    }

    public async Task<IReadOnlyList<AccountDto>> GetAccountsAsync(long companyId, CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        return await db.ChartOfAccounts.AsNoTracking()
            .Where(account => account.CompanyId == companyId)
            .OrderBy(account => account.Code)
            .Select(account => new AccountDto(
                account.Id,
                account.CompanyId,
                account.AccountGroupId,
                account.ParentId,
                account.Code,
                account.Name,
                account.AllowPosting,
                account.IsActive))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedAccountsDto> GetAccountsPageAsync(
        long companyId,
        int page,
        int pageSize,
        long? accountGroupId,
        string? search,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw Validation("pagination", "Page must be positive and page size must be between 1 and 100.");
        }

        var roots = db.ChartOfAccounts.AsNoTracking()
            .Where(account => account.CompanyId == companyId && account.ParentId == null);
        if (accountGroupId is not null)
        {
            roots = roots.Where(account => account.AccountGroupId == accountGroupId);
        }

        var normalizedSearch = Normalize(search);
        if (normalizedSearch.Length > 0)
        {
            roots = roots.Where(account =>
                account.Code.ToLower().Contains(normalizedSearch.ToLower()) ||
                account.Name.ToLower().Contains(normalizedSearch.ToLower()));
        }

        var totalCount = await roots.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var pageRoots = await roots
            .OrderBy(account => account.Code)
            .ThenBy(account => account.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(account => new AccountDto(
                account.Id,
                account.CompanyId,
                account.AccountGroupId,
                account.ParentId,
                account.Code,
                account.Name,
                account.AllowPosting,
                account.IsActive))
            .ToListAsync(cancellationToken);

        var items = new List<AccountDto>(pageRoots);
        var seenIds = pageRoots.Select(account => account.Id).ToHashSet();
        var parentIds = pageRoots.Select(account => account.Id).ToArray();
        while (parentIds.Length > 0)
        {
            var children = await db.ChartOfAccounts.AsNoTracking()
                .Where(account => account.CompanyId == companyId &&
                    account.ParentId.HasValue && parentIds.Contains(account.ParentId.Value))
                .OrderBy(account => account.Code)
                .ThenBy(account => account.Id)
                .Select(account => new AccountDto(
                    account.Id,
                    account.CompanyId,
                    account.AccountGroupId,
                    account.ParentId,
                    account.Code,
                    account.Name,
                    account.AllowPosting,
                    account.IsActive))
                .ToListAsync(cancellationToken);

            var unseenChildren = children.Where(account => seenIds.Add(account.Id)).ToList();
            items.AddRange(unseenChildren);
            parentIds = unseenChildren.Select(account => account.Id).ToArray();
        }

        var totalAccounts = await db.ChartOfAccounts.CountAsync(
            account => account.CompanyId == companyId,
            cancellationToken);
        return new PagedAccountsDto(items, normalizedPage, pageSize, totalCount, totalPages, totalAccounts);
    }

    public async Task<AccountDto?> GetAccountAsync(
        long companyId,
        long accountId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var account = await db.ChartOfAccounts.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CompanyId == companyId && item.Id == accountId,
                cancellationToken);
        return account is null ? null : ToDto(account);
    }

    public async Task<IReadOnlyList<FiscalYearDto>> GetFiscalYearsAsync(long companyId, CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        return await db.FiscalYears.AsNoTracking()
            .Where(year => year.CompanyId == companyId)
            .OrderByDescending(year => year.StartsOn)
            .Select(year => new FiscalYearDto(year.Id, year.CompanyId, year.StartsOn, year.EndsOn, year.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<FiscalYearDto>> GetFiscalYearsPageAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        ValidatePagination(page, pageSize);
        var years = db.FiscalYears.AsNoTracking().Where(year => year.CompanyId == companyId);
        var totalCount = await years.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var items = await years
            .OrderByDescending(year => year.StartsOn)
            .ThenByDescending(year => year.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(year => new FiscalYearDto(year.Id, year.CompanyId, year.StartsOn, year.EndsOn, year.Status))
            .ToListAsync(cancellationToken);
        return new PagedResultDto<FiscalYearDto>(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    public async Task<FiscalYearDto?> GetFiscalYearAsync(
        long companyId,
        long fiscalYearId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var year = await db.FiscalYears.AsNoTracking().SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == fiscalYearId, cancellationToken);
        return year is null ? null : new FiscalYearDto(year.Id, companyId, year.StartsOn, year.EndsOn, year.Status);
    }

    public async Task<FiscalYearDto?> UpdateFiscalYearAsync(
        long companyId,
        long fiscalYearId,
        UpdateFiscalYearRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var year = await db.FiscalYears.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == fiscalYearId, cancellationToken);
        if (year is null) return null;
        if (year.Status != "Open")
            throw new AccountingConflictException("A closed fiscal year cannot be edited.");
        ValidateDateRange(request.StartsOn, request.EndsOn, "Fiscal year");
        if (await db.FiscalYears.AnyAsync(
                item => item.CompanyId == companyId && item.Id != fiscalYearId &&
                        item.StartsOn <= request.EndsOn && item.EndsOn >= request.StartsOn,
                cancellationToken))
            throw Validation("startsOn", "Fiscal years for a company must not overlap.");
        if (await db.AccountingPeriods.AnyAsync(
                period => period.CompanyId == companyId && period.FiscalYearId == fiscalYearId &&
                          (period.StartsOn < request.StartsOn || period.EndsOn > request.EndsOn),
                cancellationToken))
            throw Validation("startsOn", "The fiscal year must continue to contain all of its accounting periods.");
        year.StartsOn = request.StartsOn;
        year.EndsOn = request.EndsOn;
        await SaveAsync(cancellationToken);
        return new FiscalYearDto(year.Id, companyId, year.StartsOn, year.EndsOn, year.Status);
    }

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
        if (await db.JournalEntries.AnyAsync(entry => entry.AccountingPeriodId == periodId, cancellationToken))
            throw new AccountingConflictException("An accounting period with journal entries cannot be edited.");
        var year = await db.FiscalYears.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == request.FiscalYearId, cancellationToken);
        if (year is null)
            throw Validation("fiscalYearId", "The fiscal year does not belong to this company.");
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
            if (parent is null || parent.AllowPosting)
            {
                throw Validation("parentId", "A parent must be an existing non-posting account in this company.");
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

        if (request.ParentId is not null)
        {
            if (request.ParentId == accountId)
            {
                throw Validation("parentId", "An account cannot be its own parent.");
            }

            var parent = await db.ChartOfAccounts.SingleOrDefaultAsync(
                item => item.CompanyId == companyId && item.Id == request.ParentId,
                cancellationToken);
            if (parent is null || parent.AllowPosting)
            {
                throw Validation("parentId", "A parent must be an existing non-posting account in this company.");
            }

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
        await SaveAsync(cancellationToken);
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

    public async Task<JournalEntryDto?> CreateDraftAsync(
        long companyId,
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await db.Companies.SingleOrDefaultAsync(
            item => item.Id == companyId,
            cancellationToken);
        if (company is null)
        {
            return null;
        }

        var period = await db.AccountingPeriods.SingleOrDefaultAsync(
            item => item.Id == request.AccountingPeriodId && item.CompanyId == companyId,
            cancellationToken);
        if (period is null || request.EntryDate < period.StartsOn || request.EntryDate > period.EndsOn)
        {
            throw Validation("accountingPeriodId", "Choose an accounting period in this company that contains the entry date.");
        }

        if (period.Status != "Open")
        {
            throw new AccountingConflictException("Journal entries cannot be created in a closed period.");
        }

        if (!await db.Journals.AnyAsync(
                journal => journal.Id == request.JournalId && journal.CompanyId == companyId && journal.IsActive,
                cancellationToken))
        {
            throw Validation("journalId", "Choose an active journal in this company.");
        }

        var description = Normalize(request.Description);
        if (description.Length is 0 or > 500)
        {
            throw Validation("description", "A description of 1–500 characters is required.");
        }

        if (request.Lines is null || request.Lines.Count is < 2 or > 1000)
        {
            throw Validation("lines", "A journal entry must contain 2–1000 lines.");
        }

        var accountIds = request.Lines.Select(line => line.AccountId).Distinct().ToArray();
        var accounts = await db.ChartOfAccounts
            .Where(account => account.CompanyId == companyId && accountIds.Contains(account.Id))
            .ToDictionaryAsync(account => account.Id, cancellationToken);
        if (accounts.Count != accountIds.Length || accounts.Values.Any(account => !account.IsActive || !account.AllowPosting))
        {
            throw Validation("lines", "Each line must use an active, postable account in this company.");
        }

        var currencyCodes = request.Lines
            .Select(line => Normalize(line.TransactionCurrencyCode).ToUpperInvariant())
            .Distinct()
            .Append(company.FunctionalCurrencyCode)
            .Distinct()
            .ToArray();
        var currencies = await db.Currencies
            .Where(currency => currencyCodes.Contains(currency.Code))
            .ToDictionaryAsync(currency => currency.Code, cancellationToken);
        if (currencies.Count != currencyCodes.Length)
        {
            throw Validation("lines", "Each line must use a supported transaction currency.");
        }

        var lines = new List<JournalLine>(request.Lines.Count);
        for (var index = 0; index < request.Lines.Count; index++)
        {
            var requestLine = request.Lines[index];
            var currencyCode = Normalize(requestLine.TransactionCurrencyCode).ToUpperInvariant();
            var currency = currencies[currencyCode];
            var lineErrors = new Dictionary<string, string[]>();
            if (requestLine.Debit < 0 || requestLine.Credit < 0 ||
                (requestLine.Debit == 0) == (requestLine.Credit == 0) ||
                requestLine.Debit > MaximumAmount || requestLine.Credit > MaximumAmount)
            {
                lineErrors["amount"] = ["Enter a positive debit or credit (not both) within the supported amount range."];
            }

            if (requestLine.ExchangeRate <= 0 || requestLine.ExchangeRate > 9_999_999_999.9999999999m)
            {
                lineErrors["exchangeRate"] = ["Exchange rate must be greater than zero and within the supported range."];
            }
            else if (currencyCode == company.FunctionalCurrencyCode && requestLine.ExchangeRate != 1)
            {
                lineErrors["exchangeRate"] = ["The functional currency exchange rate must be 1."];
            }

            if (decimal.Round(requestLine.Debit, currency.DecimalPlaces) != requestLine.Debit ||
                decimal.Round(requestLine.Credit, currency.DecimalPlaces) != requestLine.Credit)
            {
                lineErrors["amount"] = [$"{currencyCode} supports {currency.DecimalPlaces} decimal places."];
            }

            if (requestLine.Description?.Length > 500)
            {
                lineErrors["description"] = ["Line description must be at most 500 characters."];
            }

            if (lineErrors.Count != 0)
            {
                throw new AccountingValidationException(new Dictionary<string, string[]>
                {
                    [$"lines[{index}]"] = lineErrors.Values.SelectMany(messages => messages).ToArray()
                });
            }

            var functionalDebit = decimal.Round(
                requestLine.Debit * requestLine.ExchangeRate,
                currencies[company.FunctionalCurrencyCode].DecimalPlaces,
                MidpointRounding.AwayFromZero);
            var functionalCredit = decimal.Round(
                requestLine.Credit * requestLine.ExchangeRate,
                currencies[company.FunctionalCurrencyCode].DecimalPlaces,
                MidpointRounding.AwayFromZero);
            if (functionalDebit > MaximumAmount || functionalCredit > MaximumAmount)
            {
                throw Validation($"lines[{index}]", "Converted amount exceeds the supported range.");
            }

            lines.Add(new JournalLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                AccountId = requestLine.AccountId,
                TransactionCurrencyCode = currencyCode,
                Debit = requestLine.Debit,
                Credit = requestLine.Credit,
                FunctionalDebit = functionalDebit,
                FunctionalCredit = functionalCredit,
                ExchangeRate = requestLine.ExchangeRate,
                Description = string.IsNullOrWhiteSpace(requestLine.Description)
                    ? null
                    : Normalize(requestLine.Description)
            });
        }

        var entry = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = request.JournalId,
            AccountingPeriodId = period.Id,
            EntryDate = request.EntryDate,
            Description = description,
            Lines = lines
        };
        db.JournalEntries.Add(entry);
        await SaveAsync(cancellationToken);
        return ToDto(entry);
    }

    public async Task<JournalEntryDto?> UpdateDraftAsync(
        long companyId,
        long entryId,
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
        if (company is null) return null;
        var entry = await db.JournalEntries.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == entryId, cancellationToken);
        if (entry is null) return null;
        if (entry.Status != "Draft")
            throw new AccountingConflictException("Only draft journal entries can be edited.");

        var period = await db.AccountingPeriods.SingleOrDefaultAsync(
            item => item.Id == request.AccountingPeriodId && item.CompanyId == companyId,
            cancellationToken);
        if (period is null || request.EntryDate < period.StartsOn || request.EntryDate > period.EndsOn)
            throw Validation("accountingPeriodId", "Choose an accounting period in this company that contains the entry date.");
        if (period.Status != "Open")
            throw new AccountingConflictException("Journal entries cannot be edited in a closed period.");
        if (!await db.Journals.AnyAsync(
                journal => journal.Id == request.JournalId && journal.CompanyId == companyId && journal.IsActive,
                cancellationToken))
            throw Validation("journalId", "Choose an active journal in this company.");

        var description = Normalize(request.Description);
        if (description.Length is 0 or > 500)
            throw Validation("description", "A description of 1–500 characters is required.");
        if (request.Lines is null || request.Lines.Count is < 2 or > 1000)
            throw Validation("lines", "A journal entry must contain 2–1000 lines.");

        var accountIds = request.Lines.Select(line => line.AccountId).Distinct().ToArray();
        var accounts = await db.ChartOfAccounts
            .Where(account => account.CompanyId == companyId && accountIds.Contains(account.Id))
            .ToDictionaryAsync(account => account.Id, cancellationToken);
        if (accounts.Count != accountIds.Length || accounts.Values.Any(account => !account.IsActive || !account.AllowPosting))
            throw Validation("lines", "Each line must use an active, postable account in this company.");

        var currencyCodes = request.Lines
            .Select(line => Normalize(line.TransactionCurrencyCode).ToUpperInvariant())
            .Append(company.FunctionalCurrencyCode)
            .Distinct()
            .ToArray();
        var currencies = await db.Currencies
            .Where(currency => currencyCodes.Contains(currency.Code))
            .ToDictionaryAsync(currency => currency.Code, cancellationToken);
        if (currencies.Count != currencyCodes.Length)
            throw Validation("lines", "Each line must use a supported transaction currency.");

        var lines = new List<JournalLine>(request.Lines.Count);
        for (var index = 0; index < request.Lines.Count; index++)
        {
            var requestLine = request.Lines[index];
            var currencyCode = Normalize(requestLine.TransactionCurrencyCode).ToUpperInvariant();
            var currency = currencies[currencyCode];
            var lineErrors = new Dictionary<string, string[]>();
            if (requestLine.Debit < 0 || requestLine.Credit < 0 ||
                (requestLine.Debit == 0) == (requestLine.Credit == 0) ||
                requestLine.Debit > MaximumAmount || requestLine.Credit > MaximumAmount)
                lineErrors["amount"] = ["Enter a positive debit or credit (not both) within the supported amount range."];
            if (requestLine.ExchangeRate <= 0 || requestLine.ExchangeRate > 9_999_999_999.9999999999m)
                lineErrors["exchangeRate"] = ["Exchange rate must be greater than zero and within the supported range."];
            else if (currencyCode == company.FunctionalCurrencyCode && requestLine.ExchangeRate != 1)
                lineErrors["exchangeRate"] = ["The functional currency exchange rate must be 1."];
            if (decimal.Round(requestLine.Debit, currency.DecimalPlaces) != requestLine.Debit ||
                decimal.Round(requestLine.Credit, currency.DecimalPlaces) != requestLine.Credit)
                lineErrors["amount"] = [$"{currencyCode} supports {currency.DecimalPlaces} decimal places."];
            if (requestLine.Description?.Length > 500)
                lineErrors["description"] = ["Line description must be at most 500 characters."];
            if (lineErrors.Count != 0)
                throw new AccountingValidationException(new Dictionary<string, string[]>
                {
                    [$"lines[{index}]"] = lineErrors.Values.SelectMany(messages => messages).ToArray()
                });

            var functionalDebit = decimal.Round(
                requestLine.Debit * requestLine.ExchangeRate,
                currencies[company.FunctionalCurrencyCode].DecimalPlaces,
                MidpointRounding.AwayFromZero);
            var functionalCredit = decimal.Round(
                requestLine.Credit * requestLine.ExchangeRate,
                currencies[company.FunctionalCurrencyCode].DecimalPlaces,
                MidpointRounding.AwayFromZero);
            if (functionalDebit > MaximumAmount || functionalCredit > MaximumAmount)
                throw Validation($"lines[{index}]", "Converted amount exceeds the supported range.");
            lines.Add(new JournalLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                AccountId = requestLine.AccountId,
                TransactionCurrencyCode = currencyCode,
                Debit = requestLine.Debit,
                Credit = requestLine.Credit,
                FunctionalDebit = functionalDebit,
                FunctionalCredit = functionalCredit,
                ExchangeRate = requestLine.ExchangeRate,
                Description = string.IsNullOrWhiteSpace(requestLine.Description) ? null : Normalize(requestLine.Description)
            });
        }

        entry.JournalId = request.JournalId;
        entry.AccountingPeriodId = period.Id;
        entry.EntryDate = request.EntryDate;
        entry.Description = description;
        entry.TotalDebit = 0;
        entry.TotalCredit = 0;
        entry.Lines.Clear();
        foreach (var line in lines) entry.Lines.Add(line);
        entry.Revision++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AccountingConflictException("This journal entry was changed by another request. Reload it and try again.");
        }
        return ToDto(entry);
    }

    public async Task<JournalEntryDto?> GetJournalEntryAsync(
        long companyId,
        long entryId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var entry = await db.JournalEntries.AsNoTracking()
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == entryId, cancellationToken);
        return entry is null ? null : ToDto(entry);
    }

    public async Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var entries = await db.JournalEntries.AsNoTracking()
            .Where(entry => entry.CompanyId == companyId)
            .Include(entry => entry.Lines)
            .OrderByDescending(entry => entry.EntryDate)
            .ThenByDescending(entry => entry.Id)
            .ToListAsync(cancellationToken);
        return entries.Select(ToDto).ToArray();
    }

    public async Task<PagedResultDto<JournalEntryDto>> GetJournalEntriesPageAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        ValidatePagination(page, pageSize);
        var entries = db.JournalEntries.AsNoTracking().Where(entry => entry.CompanyId == companyId);
        var totalCount = await entries.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var pageEntries = await entries
            .OrderByDescending(entry => entry.EntryDate)
            .ThenByDescending(entry => entry.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Include(entry => entry.Lines)
            .ToListAsync(cancellationToken);
        var items = pageEntries.Select(ToDto).ToArray();
        return new PagedResultDto<JournalEntryDto>(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    public async Task<JournalEntryDto?> PostAsync(
        long companyId,
        long entryId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var entry = await db.JournalEntries
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == entryId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        if (entry.Status != "Draft")
        {
            throw new AccountingConflictException("Only draft journal entries can be posted.");
        }

        var period = await db.AccountingPeriods.SingleAsync(
            item => item.CompanyId == companyId && item.Id == entry.AccountingPeriodId,
            cancellationToken);
        if (period.Status != "Open" || entry.EntryDate < period.StartsOn || entry.EntryDate > period.EndsOn)
        {
            throw new AccountingConflictException("The entry's accounting period is closed or does not contain its date.");
        }

        var totalDebit = entry.Lines.Sum(line => line.FunctionalDebit);
        var totalCredit = entry.Lines.Sum(line => line.FunctionalCredit);
        if (entry.Lines.Count < 2 || totalDebit <= 0 || totalDebit != totalCredit)
        {
            throw Validation("lines", "Journal entry must have at least two lines and balance in functional currency.");
        }

        entry.TotalDebit = totalDebit;
        entry.TotalCredit = totalCredit;
        entry.Status = "Posted";
        entry.Revision++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AccountingConflictException("This journal entry was updated by another request. Reload it and try again.");
        }

        return ToDto(entry);
    }

    private long EnsureTenant() =>
        currentTenant.TenantId ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");

    private static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw Validation("pagination", "Page must be positive and page size must be between 1 and 100.");
        }
    }

    private async Task EnsureCompanyAsync(long tenantId, long companyId, CancellationToken cancellationToken)
    {
        if (!await db.Companies.AnyAsync(
                company => company.TenantId == tenantId && company.Id == companyId,
                cancellationToken))
        {
            throw new KeyNotFoundException("Company was not found in this tenant.");
        }
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new AccountingConflictException("A record with the same code already exists in this company.");
        }
    }

    private static void ValidateDateRange(DateOnly startsOn, DateOnly endsOn, string label)
    {
        if (endsOn < startsOn)
        {
            throw Validation("endsOn", $"{label} end date must be on or after its start date.");
        }
    }

    private static void ValidateCode(IDictionary<string, string[]> errors, string field, string code, int maxLength)
    {
        if (code.Length is 0 || code.Length > maxLength)
        {
            errors[field] = [$"Code is required and must be at most {maxLength} characters."];
        }
    }

    private static void ValidateName(IDictionary<string, string[]> errors, string field, string name, int maxLength)
    {
        if (name.Length is 0 || name.Length > maxLength)
        {
            errors[field] = [$"Name is required and must be at most {maxLength} characters."];
        }
    }

    private static void ThrowIfInvalid(IReadOnlyDictionary<string, string[]> errors)
    {
        if (errors.Count != 0)
        {
            throw new AccountingValidationException(errors);
        }
    }

    private static AccountingValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static AccountGroupDto ToDto(AccountGroup group) =>
        new(group.Id, group.CompanyId, group.Code, group.Name, group.AccountType, group.NormalBalance);

    private static AccountDto ToDto(ChartOfAccount account) =>
        new(account.Id, account.CompanyId, account.AccountGroupId, account.ParentId,
            account.Code, account.Name, account.AllowPosting, account.IsActive);

    private static JournalDto ToDto(Journal journal) =>
        new(journal.Id, journal.CompanyId, journal.Code, journal.Name, journal.JournalType, journal.IsActive);

    private static JournalEntryDto ToDto(JournalEntry entry) =>
        new(entry.Id, entry.CompanyId, entry.JournalId, entry.AccountingPeriodId, entry.EntryDate,
            entry.Description, entry.Status, entry.TotalDebit, entry.TotalCredit,
            entry.Lines.OrderBy(line => line.Id)
                .Select(line => new JournalLineDto(
                    line.Id, line.AccountId, line.TransactionCurrencyCode, line.Debit, line.Credit,
                    line.FunctionalDebit, line.FunctionalCredit, line.ExchangeRate, line.Description))
                .ToArray());
}
