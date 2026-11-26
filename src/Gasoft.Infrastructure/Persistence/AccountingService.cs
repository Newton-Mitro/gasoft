using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
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

    public async Task<FiscalYearCloseChecklistDto?> GetFiscalYearCloseChecklistAsync(
        long companyId,
        long fiscalYearId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var year = await db.FiscalYears.AsNoTracking().SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == fiscalYearId,
            cancellationToken);
        return year is null ? null : await BuildFiscalYearCloseChecklistAsync(year, cancellationToken);
    }

    public async Task<JournalEntryDto?> PrepareFiscalYearClosingJournalAsync(
        long companyId,
        long fiscalYearId,
        PrepareFiscalYearClosingRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var userId = currentTenant.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required.");
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var year = await db.FiscalYears.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == fiscalYearId,
            cancellationToken);
        if (year is null) return null;
        if (year.Status != "Open")
            throw new AccountingConflictException("A closing journal can only be prepared for an open fiscal year.");

        var existingClosing = await db.FiscalYearClosings.SingleOrDefaultAsync(
            closing => closing.CompanyId == companyId && closing.FiscalYearId == fiscalYearId,
            cancellationToken);
        if (existingClosing is not null)
        {
            var existingEntry = await db.JournalEntries
                .Include(entry => entry.Lines)
                .SingleAsync(entry => entry.CompanyId == companyId && entry.Id == existingClosing.JournalEntryId,
                    cancellationToken);
            return ToDto(existingEntry);
        }

        var retainedEarningsAccount = await (
            from account in db.ChartOfAccounts
            join accountGroup in db.AccountGroups
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == companyId &&
                  account.Id == request.RetainedEarningsAccountId &&
                  account.IsActive &&
                  account.AllowPosting &&
                  accountGroup.AccountType == "Equity"
            select account)
            .SingleOrDefaultAsync(cancellationToken);
        if (retainedEarningsAccount is null)
            throw Validation(
                "retainedEarningsAccountId",
                "Choose an active, postable Equity account for retained earnings.");

        var closingPeriod = await db.AccountingPeriods.SingleOrDefaultAsync(
            period => period.CompanyId == companyId &&
                      period.FiscalYearId == fiscalYearId &&
                      period.StartsOn <= year.EndsOn &&
                      period.EndsOn >= year.EndsOn,
            cancellationToken);
        if (closingPeriod is null)
            throw new AccountingConflictException(
                "Create an accounting period that includes the fiscal year-end before preparing closing entries.");
        if (closingPeriod.Status != "Closed" ||
            await db.AccountingPeriods.AnyAsync(
                period => period.CompanyId == companyId &&
                          period.FiscalYearId == fiscalYearId &&
                          period.Status != "Closed",
                cancellationToken))
            throw new AccountingConflictException(
                "Close all accounting periods before preparing the year-end closing journal.");

        var generalJournal = await db.Journals.SingleOrDefaultAsync(
            journal => journal.CompanyId == companyId && journal.Code == "GEN" && journal.IsActive,
            cancellationToken);
        if (generalJournal is null)
            throw new AccountingConflictException("An active General journal (GEN) is required for year-end closing.");

        var company = await db.Companies.SingleAsync(item => item.Id == companyId, cancellationToken);
        var functionalCurrency = await db.Currencies.SingleAsync(
            currency => currency.Code == company.FunctionalCurrencyCode,
            cancellationToken);
        var periodIds = await db.AccountingPeriods.AsNoTracking()
            .Where(period => period.CompanyId == companyId && period.FiscalYearId == fiscalYearId)
            .Select(period => period.Id)
            .ToArrayAsync(cancellationToken);
        var balances = await GetFiscalYearIncomeExpenseBalancesAsync(companyId, periodIds, cancellationToken);
        var closingLines = new List<JournalLine>();
        decimal netIncome = 0;
        foreach (var balance in balances)
        {
            var net = balance.AccountType == "Revenue"
                ? balance.Credit - balance.Debit
                : balance.Debit - balance.Credit;
            net = decimal.Round(net, functionalCurrency.DecimalPlaces, MidpointRounding.AwayFromZero);
            if (net == 0) continue;
            if (Math.Abs(net) > MaximumAmount)
                throw new AccountingConflictException(
                    $"The closing balance for account {balance.Code} exceeds the supported journal amount.");

            var debit = balance.AccountType == "Revenue" ? net : -net;
            closingLines.Add(CreateClosingLine(
                tenantId,
                companyId,
                balance.AccountId,
                company.FunctionalCurrencyCode,
                debit));
            netIncome += net * (balance.AccountType == "Revenue" ? 1 : -1);
        }

        if (closingLines.Count == 0)
            throw new AccountingConflictException("There are no income or expense balances to close.");

        netIncome = decimal.Round(netIncome, functionalCurrency.DecimalPlaces, MidpointRounding.AwayFromZero);
        if (netIncome != 0)
        {
            if (Math.Abs(netIncome) > MaximumAmount)
                throw new AccountingConflictException("The net income exceeds the supported journal amount.");
            closingLines.Add(CreateClosingLine(
                tenantId,
                companyId,
                retainedEarningsAccount.Id,
                company.FunctionalCurrencyCode,
                -netIncome));
        }
        if (closingLines.Count < 2)
            throw new AccountingConflictException("The year-end balances do not require a closing journal.");

        var entry = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = generalJournal.Id,
            AccountingPeriodId = closingPeriod.Id,
            EntryDate = year.EndsOn,
            Description = $"Year-end closing entry for {year.StartsOn:yyyy-MM-dd} to {year.EndsOn:yyyy-MM-dd}",
            Lines = closingLines
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        db.FiscalYearClosings.Add(new FiscalYearClosing
        {
            TenantId = tenantId,
            CompanyId = companyId,
            FiscalYearId = fiscalYearId,
            JournalEntryId = entry.Id,
            RetainedEarningsAccountId = retainedEarningsAccount.Id,
            CreatedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(entry);
    }

    public async Task<JournalEntryDto?> PrepareFiscalYearOpeningBalancesAsync(
        long companyId,
        long fiscalYearId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var year = await db.FiscalYears.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == fiscalYearId,
            cancellationToken);
        if (year is null) return null;
        if (year.Status != "Open")
            throw new AccountingConflictException("Opening balances can only be prepared for an open fiscal year.");

        var firstPeriod = await db.AccountingPeriods
            .Where(period => period.CompanyId == companyId && period.FiscalYearId == fiscalYearId)
            .OrderBy(period => period.StartsOn)
            .FirstOrDefaultAsync(cancellationToken);
        if (firstPeriod is null ||
            firstPeriod.StartsOn > year.StartsOn ||
            firstPeriod.EndsOn < year.StartsOn)
            throw new AccountingConflictException(
                "Create an accounting period that includes the fiscal year start before preparing opening balances.");

        var previousYear = await db.FiscalYears
            .Where(item => item.CompanyId == companyId && item.EndsOn < year.StartsOn)
            .OrderByDescending(item => item.EndsOn)
            .FirstOrDefaultAsync(cancellationToken);
        if (previousYear is null)
            throw new AccountingConflictException("Create and close a prior fiscal year before preparing opening balances.");
        if (previousYear.Status != "Closed")
            throw new AccountingConflictException("Close the prior fiscal year before preparing opening balances.");

        var previousYearChecklist = await BuildFiscalYearCloseChecklistAsync(previousYear, cancellationToken);
        if (!previousYearChecklist.CanClose)
            throw new AccountingConflictException(
                "Complete the prior fiscal-year close checklist, including posting any required closing journal, before preparing opening balances.");

        var closing = await db.FiscalYearClosings.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.CompanyId == companyId && item.FiscalYearId == previousYear.Id,
                cancellationToken);
        if (closing is not null && previousYearChecklist.ClosingJournalStatus != "Posted")
            throw new AccountingConflictException(
                "Post the prior fiscal year closing journal before preparing opening balances.");

        var periodIds = await db.AccountingPeriods.AsNoTracking()
            .Where(period => period.CompanyId == companyId && period.FiscalYearId == fiscalYearId)
            .Select(period => period.Id)
            .ToArrayAsync(cancellationToken);
        var openingDescription =
            $"Opening balances for {year.StartsOn:yyyy-MM-dd} to {year.EndsOn:yyyy-MM-dd}";
        var existingOpeningEntries = await db.JournalEntries.AsNoTracking()
            .Include(entry => entry.Lines)
            .Where(entry => entry.CompanyId == companyId &&
                           periodIds.Contains(entry.AccountingPeriodId) &&
                           entry.Description == openingDescription)
            .OrderBy(entry => entry.Id)
            .ToListAsync(cancellationToken);
        if (existingOpeningEntries.Count > 1)
            throw new AccountingConflictException(
                "More than one opening-balance journal exists for this fiscal year. Resolve the duplicates before continuing.");
        if (existingOpeningEntries.Count == 1)
        {
            var existingOpeningEntry = existingOpeningEntries[0];
            if (existingOpeningEntry.AccountingPeriodId != firstPeriod.Id)
                throw new AccountingConflictException(
                    "The existing opening-balance journal is not in the first accounting period. Resolve it before preparing another.");
            if (existingOpeningEntry.Status == "Draft" && firstPeriod.Status != "Open")
                throw new AccountingConflictException(
                    "Reopen the first accounting period before reviewing and posting the opening-balance journal.");
            if (existingOpeningEntry.Status is not ("Draft" or "Posted"))
                throw new AccountingConflictException(
                    "The existing opening-balance journal is not available for posting.");

            await transaction.CommitAsync(cancellationToken);
            return ToDto(existingOpeningEntry);
        }

        if (firstPeriod.Status != "Open")
            throw new AccountingConflictException(
                "Open the first accounting period before preparing opening balances.");

        var generalJournal = await db.Journals.SingleOrDefaultAsync(
            journal => journal.CompanyId == companyId && journal.Code == "GEN" && journal.IsActive,
            cancellationToken);
        if (generalJournal is null)
            throw new AccountingConflictException("An active General journal (GEN) is required for opening balances.");

        var company = await db.Companies.SingleAsync(item => item.Id == companyId, cancellationToken);
        var functionalCurrency = await db.Currencies.SingleAsync(
            currency => currency.Code == company.FunctionalCurrencyCode,
            cancellationToken);
        var balances = await GetFiscalYearBalanceSheetBalancesAsync(companyId, previousYear.Id, cancellationToken);
        if (balances.Count == 0)
            throw new AccountingConflictException("The prior fiscal year has no balance-sheet balances to carry forward.");

        var openingLines = new List<JournalLine>();
        foreach (var balance in balances.OrderBy(item => item.Code))
        {
            var openingAmount = balance.AccountType == "Asset"
                ? balance.Debit - balance.Credit
                : balance.Credit - balance.Debit;
            openingAmount = decimal.Round(openingAmount, functionalCurrency.DecimalPlaces, MidpointRounding.AwayFromZero);
            if (openingAmount == 0)
                continue;

            openingLines.Add(CreateOpeningLine(
                tenantId,
                companyId,
                balance.AccountId,
                company.FunctionalCurrencyCode,
                openingAmount));
        }

        if (openingLines.Count == 0)
            throw new AccountingConflictException("The prior fiscal year has no opening balances to carry forward.");

        var totalDebit = openingLines.Sum(line => line.FunctionalDebit);
        var totalCredit = openingLines.Sum(line => line.FunctionalCredit);
        var roundingDifference = totalDebit - totalCredit;
        if (roundingDifference != 0)
        {
            if (closing is null)
                throw new AccountingConflictException(
                    "The prior-year balance-sheet balances do not balance. Review the prior-year ledger before carrying them forward.");

            var retainedEarningsAccount = await db.ChartOfAccounts.SingleOrDefaultAsync(
                account => account.CompanyId == companyId && account.Id == closing.RetainedEarningsAccountId,
                cancellationToken);
            if (retainedEarningsAccount is null)
                throw new AccountingConflictException("The retained earnings account used for the prior close is no longer available.");

            openingLines.Add(CreateOpeningLine(
                tenantId,
                companyId,
                retainedEarningsAccount.Id,
                company.FunctionalCurrencyCode,
                -roundingDifference));
        }

        var entry = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = generalJournal.Id,
            AccountingPeriodId = firstPeriod.Id,
            EntryDate = year.StartsOn,
            Description = openingDescription,
            Lines = openingLines
        };
        db.JournalEntries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToDto(entry);
    }

    public async Task<FiscalYearDto?> CloseFiscalYearAsync(
        long companyId,
        long fiscalYearId,
        ChangeAccountingPeriodStatusRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var userId = currentTenant.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required.");
        var reason = ValidatePeriodStatusReason(request.Reason);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var changed = await db.FiscalYears
            .Where(year => year.CompanyId == companyId &&
                           year.Id == fiscalYearId &&
                           year.Status == "Open")
            .ExecuteUpdateAsync(setters => setters.SetProperty(year => year.Status, "Closed"), cancellationToken);
        if (changed == 0)
        {
            var exists = await db.FiscalYears.AnyAsync(
                year => year.CompanyId == companyId && year.Id == fiscalYearId,
                cancellationToken);
            if (!exists) return null;
            throw new AccountingConflictException("Only open fiscal years can be closed.");
        }

        var year = await db.FiscalYears.SingleAsync(
            item => item.CompanyId == companyId && item.Id == fiscalYearId,
            cancellationToken);
        var checklist = await BuildFiscalYearCloseChecklistAsync(year, cancellationToken);
        if (!checklist.CanClose)
            throw new AccountingConflictException(FiscalYearCloseBlockerMessage(checklist));

        db.FiscalYearStatusChanges.Add(new FiscalYearStatusChange
        {
            TenantId = tenantId,
            CompanyId = companyId,
            FiscalYearId = fiscalYearId,
            ChangedByUserId = userId,
            FromStatus = "Open",
            ToStatus = "Closed",
            Reason = reason,
            ChangedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new FiscalYearDto(year.Id, companyId, year.StartsOn, year.EndsOn, year.Status);
    }

    public async Task<FiscalYearDto?> ReopenFiscalYearAsync(
        long companyId,
        long fiscalYearId,
        ChangeAccountingPeriodStatusRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var userId = currentTenant.UserId
            ?? throw new UnauthorizedAccessException("An authenticated user is required.");
        var reason = ValidatePeriodStatusReason(request.Reason);

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var changed = await db.FiscalYears
            .Where(year => year.CompanyId == companyId &&
                           year.Id == fiscalYearId &&
                           year.Status == "Closed")
            .ExecuteUpdateAsync(setters => setters.SetProperty(year => year.Status, "Open"), cancellationToken);
        if (changed == 0)
        {
            var exists = await db.FiscalYears.AnyAsync(
                year => year.CompanyId == companyId && year.Id == fiscalYearId,
                cancellationToken);
            if (!exists) return null;
            throw new AccountingConflictException("Only closed fiscal years can be reopened.");
        }

        var year = await db.FiscalYears.SingleAsync(
            item => item.CompanyId == companyId && item.Id == fiscalYearId,
            cancellationToken);
        db.FiscalYearStatusChanges.Add(new FiscalYearStatusChange
        {
            TenantId = tenantId,
            CompanyId = companyId,
            FiscalYearId = fiscalYearId,
            ChangedByUserId = userId,
            FromStatus = "Closed",
            ToStatus = "Open",
            Reason = reason,
            ChangedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new FiscalYearDto(year.Id, companyId, year.StartsOn, year.EndsOn, year.Status);
    }

    public async Task<IReadOnlyList<FiscalYearStatusChangeDto>?> GetFiscalYearStatusHistoryAsync(
        long companyId,
        long fiscalYearId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        if (!await db.FiscalYears.AnyAsync(
                year => year.CompanyId == companyId && year.Id == fiscalYearId,
                cancellationToken))
            return null;

        return await (
            from change in db.FiscalYearStatusChanges.AsNoTracking()
            join user in db.Users.AsNoTracking() on change.ChangedByUserId equals user.Id
            where change.CompanyId == companyId && change.FiscalYearId == fiscalYearId
            orderby change.ChangedAt descending, change.Id descending
            select new FiscalYearStatusChangeDto(
                change.Id,
                change.FromStatus,
                change.ToStatus,
                change.Reason,
                change.ChangedAt,
                change.ChangedByUserId,
                user.DisplayName ?? user.Email ?? user.Id.ToString()))
            .ToListAsync(cancellationToken);
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
        if (!await db.FiscalYears.AnyAsync(
                year => year.CompanyId == companyId &&
                        year.Id == period.FiscalYearId &&
                        year.Status == "Open",
                cancellationToken))
            throw new AccountingConflictException("Journal entries cannot be created in a closed fiscal year.");

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
        if (await db.FiscalYearClosings.AnyAsync(
                closing => closing.CompanyId == companyId && closing.JournalEntryId == entryId,
                cancellationToken))
            throw new AccountingConflictException(
                "A generated year-end closing journal cannot be edited. Reopen the periods and update the underlying entries before regenerating it.");

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
        if (entry is null) return null;
        var reversedByEntryId = await db.JournalEntries.AsNoTracking()
            .Where(item => item.ReversalOfEntryId == entryId)
            .Select(item => (long?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return ToDto(entry) with { ReversedByEntryId = reversedByEntryId };
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
        return await ToDtosWithReversalsAsync(entries, cancellationToken);
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
        var items = await ToDtosWithReversalsAsync(pageEntries, cancellationToken);
        return new PagedResultDto<JournalEntryDto>(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    public async Task<JournalEntryDto?> ReverseJournalEntryAsync(
        long companyId,
        long entryId,
        ReverseJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var original = await db.JournalEntries
            .Include(entry => entry.Lines)
            .SingleOrDefaultAsync(entry => entry.CompanyId == companyId && entry.Id == entryId, cancellationToken);
        if (original is null) return null;
        if (original.Status != "Posted")
            throw new AccountingConflictException("Only posted journal entries can be reversed.");
        if (original.Description.StartsWith("Opening balances for ", StringComparison.Ordinal))
            throw new AccountingConflictException(
                "Fiscal-year opening entries must be corrected through the fiscal-year opening workflow.");
        if (await IsOperationalJournalEntryAsync(companyId, entryId, cancellationToken))
            throw new AccountingConflictException(
                "This journal entry is linked to an operational record. Correct it through that record's workflow instead.");
        if (await db.JournalEntries.AnyAsync(
                entry => entry.CompanyId == companyId && entry.ReversalOfEntryId == entryId,
                cancellationToken))
            throw new AccountingConflictException("This journal entry has already been reversed.");

        var originalDebit = original.Lines.Sum(line => line.FunctionalDebit);
        var originalCredit = original.Lines.Sum(line => line.FunctionalCredit);
        if (original.Lines.Count < 2 || originalDebit <= 0 || originalDebit != originalCredit ||
            original.TotalDebit != originalDebit || original.TotalCredit != originalCredit)
            throw new AccountingConflictException("This posted journal entry is not balanced and cannot be reversed.");

        var period = await db.AccountingPeriods
            .Where(item => item.CompanyId == companyId &&
                item.Status == "Open" &&
                item.StartsOn <= request.ReversalDate &&
                item.EndsOn >= request.ReversalDate)
            .OrderBy(item => item.StartsOn)
            .FirstOrDefaultAsync(cancellationToken);
        if (period is null || !await db.FiscalYears.AnyAsync(
                year => year.CompanyId == companyId &&
                        year.Id == period.FiscalYearId &&
                        year.Status == "Open",
                cancellationToken))
            throw new AccountingConflictException(
                "Choose a reversal date within an open accounting period and open fiscal year.");

        var reversalDescription = $"Reversal of journal entry {original.Id}: {original.Description}";
        var reversal = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = original.JournalId,
            AccountingPeriodId = period.Id,
            EntryDate = request.ReversalDate,
            Description = reversalDescription[..Math.Min(500, reversalDescription.Length)],
            Status = "Posted",
            TotalDebit = originalCredit,
            TotalCredit = originalDebit,
            Revision = 1,
            ReversalOfEntryId = original.Id,
            Lines = original.Lines.Select(line => new JournalLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                AccountId = line.AccountId,
                TransactionCurrencyCode = line.TransactionCurrencyCode,
                Debit = line.Credit,
                Credit = line.Debit,
                FunctionalDebit = line.FunctionalCredit,
                FunctionalCredit = line.FunctionalDebit,
                ExchangeRate = line.ExchangeRate,
                Description = line.Description
            }).ToList()
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.JournalEntries.Add(reversal);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (currentTenant.UserId is long actorUserId)
            {
                db.Notifications.Add(new AppNotification
                {
                    TenantId = tenantId,
                    UserId = actorUserId,
                    Title = "Journal reversal posted",
                    Message =
                        $"Posted reversal #{reversal.Id} on {reversal.EntryDate:yyyy-MM-dd} for journal entry #{original.Id} " +
                        $"\"{original.Description}\" dated {original.EntryDate:yyyy-MM-dd}.",
                    Category = "accounting",
                    EventType = NotificationEvents.JournalReversalPosted,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "UX_JournalEntries_TenantId_CompanyId_ReversalOfEntryId"
            })
        {
            throw new AccountingConflictException("This journal entry has already been reversed.");
        }

        return ToDto(reversal);
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
        var fiscalYear = await db.FiscalYears.SingleAsync(
            item => item.CompanyId == companyId && item.Id == period.FiscalYearId,
            cancellationToken);
        if (fiscalYear.Status != "Open")
            throw new AccountingConflictException("Journal entries cannot be posted in a closed fiscal year.");

        var isFiscalYearClosingEntry = await db.FiscalYearClosings.AnyAsync(
            closing => closing.CompanyId == companyId && closing.JournalEntryId == entryId,
            cancellationToken);
        if ((period.Status != "Open" && !isFiscalYearClosingEntry) ||
            entry.EntryDate < period.StartsOn || entry.EntryDate > period.EndsOn)
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

    private static string ValidatePeriodStatusReason(string? value)
    {
        var reason = Normalize(value);
        if (reason.Length is 0 or > 500)
            throw Validation("reason", "Enter a reason of 1–500 characters.");
        return reason;
    }

    private async Task<AccountingPeriodCloseChecklistDto> BuildPeriodCloseChecklistAsync(
        AccountingPeriod period,
        CancellationToken cancellationToken)
    {
        var draftJournalEntryCount = await db.JournalEntries.CountAsync(
            entry => entry.CompanyId == period.CompanyId &&
                     entry.AccountingPeriodId == period.Id &&
                     entry.Status == "Draft",
            cancellationToken);
        var unreconciledBankTransactionCount = await db.BankStatementTransactions
            .Where(item => item.CompanyId == period.CompanyId &&
                           item.TransactionDate >= period.StartsOn &&
                           item.TransactionDate <= period.EndsOn)
            .Where(item =>
                (db.BankReconciliationMatches
                    .Where(match => match.CompanyId == period.CompanyId &&
                                    match.BankStatementTransactionId == item.Id)
                    .Sum(match => (decimal?)match.Amount) ?? 0) != item.Amount)
            .CountAsync(cancellationToken);
        return new AccountingPeriodCloseChecklistDto(
            period.Id,
            draftJournalEntryCount,
            unreconciledBankTransactionCount);
    }

    private async Task<FiscalYearCloseChecklistDto> BuildFiscalYearCloseChecklistAsync(
        FiscalYear year,
        CancellationToken cancellationToken)
    {
        var periods = await db.AccountingPeriods.AsNoTracking()
            .Where(period => period.CompanyId == year.CompanyId && period.FiscalYearId == year.Id)
            .Select(period => new { period.Id, period.Status })
            .ToListAsync(cancellationToken);
        var periodIds = periods.Select(period => period.Id).ToArray();
        var draftJournalEntryCount = periodIds.Length == 0
            ? 0
            : await db.JournalEntries.CountAsync(
                entry => entry.CompanyId == year.CompanyId &&
                         periodIds.Contains(entry.AccountingPeriodId) &&
                         entry.Status == "Draft",
                cancellationToken);
        var closing = await db.FiscalYearClosings.AsNoTracking()
            .Where(item => item.CompanyId == year.CompanyId && item.FiscalYearId == year.Id)
            .Join(
                db.JournalEntries.AsNoTracking(),
                item => item.JournalEntryId,
                entry => entry.Id,
                (item, entry) => new { item.JournalEntryId, entry.Status })
            .SingleOrDefaultAsync(cancellationToken);
        IReadOnlyList<FiscalYearClosingLineDto> closingLines = closing is null
            ? []
            : await (
                from line in db.JournalLines.AsNoTracking()
                join account in db.ChartOfAccounts.AsNoTracking()
                    on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                    equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
                where line.CompanyId == year.CompanyId && line.JournalEntryId == closing.JournalEntryId
                orderby account.Code
                select new FiscalYearClosingLineDto(
                    account.Code,
                    account.Name,
                    line.FunctionalDebit,
                    line.FunctionalCredit))
                .ToListAsync(cancellationToken);
        var balances = await GetFiscalYearIncomeExpenseBalancesAsync(
            year.CompanyId, periodIds, cancellationToken);
        var closingJournalRequired = closing is not null || balances.Any(balance =>
            balance.AccountType == "Revenue"
                ? balance.Credit != balance.Debit
                : balance.Debit != balance.Credit);
        return new FiscalYearCloseChecklistDto(
            year.Id,
            periods.Count,
            periods.Count(period => period.Status != "Closed"),
            draftJournalEntryCount,
            closingJournalRequired,
            closing?.JournalEntryId,
            closing?.Status,
            closingLines);
    }

    private async Task<IReadOnlyList<FiscalYearBalanceSheetBalance>> GetFiscalYearBalanceSheetBalancesAsync(
        long companyId,
        long fiscalYearId,
        CancellationToken cancellationToken)
    {
        var periodIds = await db.AccountingPeriods.AsNoTracking()
            .Where(period => period.CompanyId == companyId && period.FiscalYearId == fiscalYearId)
            .Select(period => period.Id)
            .ToArrayAsync(cancellationToken);
        if (periodIds.Length == 0) return [];

        return await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, JournalEntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, JournalEntryId = entry.Id }
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where line.CompanyId == companyId &&
                  periodIds.Contains(entry.AccountingPeriodId) &&
                  entry.Status == "Posted" &&
                  (accountGroup.AccountType == "Asset" ||
                   accountGroup.AccountType == "Liability" ||
                   accountGroup.AccountType == "Equity")
            group line by new
            {
                AccountId = account.Id,
                account.Code,
                accountGroup.AccountType
            }
            into grouped
            orderby grouped.Key.Code
            select new FiscalYearBalanceSheetBalance(
                grouped.Key.AccountId,
                grouped.Key.Code,
                grouped.Key.AccountType,
                grouped.Sum(line => line.FunctionalDebit),
                grouped.Sum(line => line.FunctionalCredit)))
            .ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<FiscalYearIncomeExpenseBalance>> GetFiscalYearIncomeExpenseBalancesAsync(
        long companyId,
        long[] periodIds,
        CancellationToken cancellationToken)
    {
        if (periodIds.Length == 0) return [];
        return await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, JournalEntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, JournalEntryId = entry.Id }
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where line.CompanyId == companyId &&
                  periodIds.Contains(entry.AccountingPeriodId) &&
                  entry.Status == "Posted" &&
                  (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense")
            group line by new
            {
                AccountId = account.Id,
                account.Code,
                accountGroup.AccountType
            }
            into grouped
            orderby grouped.Key.Code
            select new FiscalYearIncomeExpenseBalance(
                grouped.Key.AccountId,
                grouped.Key.Code,
                grouped.Key.AccountType,
                grouped.Sum(line => line.FunctionalDebit),
                grouped.Sum(line => line.FunctionalCredit)))
            .ToListAsync(cancellationToken);
    }

    private static JournalLine CreateClosingLine(
        long tenantId,
        long companyId,
        long accountId,
        string currencyCode,
        decimal debitMinusCredit)
    {
        var debit = Math.Max(debitMinusCredit, 0);
        var credit = Math.Max(-debitMinusCredit, 0);
        return new JournalLine
        {
            TenantId = tenantId,
            CompanyId = companyId,
            AccountId = accountId,
            TransactionCurrencyCode = currencyCode,
            Debit = debit,
            Credit = credit,
            FunctionalDebit = debit,
            FunctionalCredit = credit,
            ExchangeRate = 1,
            Description = "Fiscal-year closing"
        };
    }

    private static JournalLine CreateOpeningLine(
        long tenantId,
        long companyId,
        long accountId,
        string currencyCode,
        decimal debitMinusCredit)
    {
        var debit = Math.Max(debitMinusCredit, 0);
        var credit = Math.Max(-debitMinusCredit, 0);
        return new JournalLine
        {
            TenantId = tenantId,
            CompanyId = companyId,
            AccountId = accountId,
            TransactionCurrencyCode = currencyCode,
            Debit = debit,
            Credit = credit,
            FunctionalDebit = debit,
            FunctionalCredit = credit,
            ExchangeRate = 1,
            Description = "Opening balance"
        };
    }

    private static string FiscalYearCloseBlockerMessage(FiscalYearCloseChecklistDto checklist)
    {
        var blockers = new List<string>();
        if (checklist.AccountingPeriodCount == 0)
            blockers.Add("the fiscal year has no accounting periods");
        if (checklist.OpenAccountingPeriodCount > 0)
            blockers.Add($"{checklist.OpenAccountingPeriodCount} accounting period{(checklist.OpenAccountingPeriodCount == 1 ? string.Empty : "s")} still open");
        if (checklist.DraftJournalEntryCount > 0)
            blockers.Add($"{checklist.DraftJournalEntryCount} draft journal entr{(checklist.DraftJournalEntryCount == 1 ? "y" : "ies")}");
        if (checklist.ClosingJournalRequired && checklist.ClosingJournalStatus != "Posted")
            blockers.Add(checklist.ClosingJournalStatus == "Draft"
                ? "the year-end closing journal must be posted"
                : "a year-end closing journal must be prepared and posted");
        return $"Resolve fiscal-year close checklist: {string.Join(" and ", blockers)}.";
    }

    private static string PeriodCloseBlockerMessage(AccountingPeriodCloseChecklistDto checklist)
    {
        var blockers = new List<string>();
        if (checklist.DraftJournalEntryCount > 0)
            blockers.Add($"{checklist.DraftJournalEntryCount} draft journal entr{(checklist.DraftJournalEntryCount == 1 ? "y" : "ies")}");
        if (checklist.UnreconciledBankTransactionCount > 0)
            blockers.Add($"{checklist.UnreconciledBankTransactionCount} unreconciled bank statement transaction{(checklist.UnreconciledBankTransactionCount == 1 ? string.Empty : "s")}");
        return $"Resolve period close checklist: {string.Join(" and ", blockers)}.";
    }

    private sealed record FiscalYearBalanceSheetBalance(
        long AccountId,
        string Code,
        string AccountType,
        decimal Debit,
        decimal Credit);

    private sealed record FiscalYearIncomeExpenseBalance(
        long AccountId,
        string Code,
        string AccountType,
        decimal Debit,
        decimal Credit);

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

    private async Task<JournalEntryDto[]> ToDtosWithReversalsAsync(
        IReadOnlyCollection<JournalEntry> entries,
        CancellationToken cancellationToken)
    {
        var entryIds = entries.Select(entry => entry.Id).ToArray();
        var reversalIds = await db.JournalEntries.AsNoTracking()
            .Where(entry => entry.ReversalOfEntryId.HasValue &&
                entryIds.Contains(entry.ReversalOfEntryId.Value))
            .Select(entry => new { OriginalId = entry.ReversalOfEntryId!.Value, ReversalId = entry.Id })
            .ToDictionaryAsync(entry => entry.OriginalId, entry => entry.ReversalId, cancellationToken);
        return entries.Select(entry => ToDto(entry) with
        {
            ReversedByEntryId = reversalIds.TryGetValue(entry.Id, out var reversalId)
                ? reversalId
                : null
        }).ToArray();
    }

    private async Task<bool> IsOperationalJournalEntryAsync(
        long companyId,
        long entryId,
        CancellationToken cancellationToken) =>
        await db.SalesInvoices.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken) ||
        await db.SalesCreditNotes.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken) ||
        await db.PurchaseBills.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken) ||
        await db.Payments.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken) ||
        await db.CustomerRefunds.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken) ||
        await db.InventoryCounts.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken) ||
        await db.StockMovements.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken) ||
        await db.ExpenseReports.AnyAsync(
            item => item.CompanyId == companyId &&
                (item.JournalEntryId == entryId || item.ReimbursementJournalEntryId == entryId),
            cancellationToken) ||
        await db.FixedAssetDepreciations.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken) ||
        await db.FixedAssets.AnyAsync(
            item => item.CompanyId == companyId &&
                (item.AcquisitionJournalEntryId == entryId || item.DisposalJournalEntryId == entryId),
            cancellationToken) ||
        await db.FiscalYearClosings.AnyAsync(
            item => item.CompanyId == companyId && item.JournalEntryId == entryId, cancellationToken);

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
                .ToArray(),
            entry.ReversalOfEntryId);
}
