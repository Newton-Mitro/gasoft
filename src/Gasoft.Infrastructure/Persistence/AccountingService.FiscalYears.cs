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

    public async Task<JournalEntryDto?> PrepareInitialFiscalYearOpeningBalancesAsync(
        long companyId,
        long fiscalYearId,
        PrepareInitialFiscalYearOpeningBalancesRequest request,
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
            throw new AccountingConflictException("Starting balances can only be prepared for an open fiscal year.");
        if (await db.FiscalYears.AnyAsync(
                item => item.CompanyId == companyId && item.EndsOn < year.StartsOn,
                cancellationToken))
            throw new AccountingConflictException(
                "Starting balances are only for a company's first fiscal year. Use carry-forward balances for later years.");

        var firstPeriod = await db.AccountingPeriods
            .Where(period => period.CompanyId == companyId && period.FiscalYearId == fiscalYearId)
            .OrderBy(period => period.StartsOn)
            .FirstOrDefaultAsync(cancellationToken);
        if (firstPeriod is null ||
            firstPeriod.StartsOn > year.StartsOn ||
            firstPeriod.EndsOn < year.StartsOn)
            throw new AccountingConflictException(
                "Create an accounting period that includes the fiscal year start before preparing starting balances.");
        if (firstPeriod.Status != "Open")
            throw new AccountingConflictException(
                "Open the first accounting period before preparing starting balances.");

        var openingDescription = $"Opening balances for {year.StartsOn:yyyy-MM-dd} to {year.EndsOn:yyyy-MM-dd}";
        if (await db.JournalEntries.AnyAsync(
                entry => entry.CompanyId == companyId &&
                         entry.AccountingPeriodId == firstPeriod.Id &&
                         entry.Description == openingDescription,
                cancellationToken))
            throw new AccountingConflictException(
                "A starting-balance journal already exists. Review it in Journal entries instead of creating a duplicate.");

        if (request.Lines is null || request.Lines.Count is < 1 or > 999)
            throw Validation("lines", "Add between 1 and 999 balance-sheet account lines.");

        var accountIds = request.Lines.Select(line => line.AccountId).ToArray();
        if (accountIds.Distinct().Count() != accountIds.Length)
            throw Validation("lines", "Each account can appear only once in the starting balances.");

        var accounts = await (
            from account in db.ChartOfAccounts
            join accountGroup in db.AccountGroups
                on new { account.TenantId, account.CompanyId, GroupId = account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, GroupId = accountGroup.Id }
            where account.CompanyId == companyId && accountIds.Contains(account.Id)
            select new
            {
                account.Id,
                account.Code,
                account.IsActive,
                account.AllowPosting,
                accountGroup.AccountType
            })
            .ToDictionaryAsync(account => account.Id, cancellationToken);
        if (accounts.Count != accountIds.Length ||
            accounts.Values.Any(account =>
                !account.IsActive ||
                !account.AllowPosting ||
                account.Code == "3000" ||
                account.AccountType is not ("Asset" or "Liability" or "Equity")))
            throw Validation(
                "lines",
                "Choose active, postable Asset, Liability, or Equity accounts. Opening Balance Equity is added automatically.");

        var company = await db.Companies.SingleAsync(item => item.Id == companyId, cancellationToken);
        var functionalCurrency = await db.Currencies.SingleOrDefaultAsync(
            currency => currency.Code == company.FunctionalCurrencyCode,
            cancellationToken);
        if (functionalCurrency is null)
            throw new AccountingConflictException("The company's functional currency is not available.");

        var openingLines = new List<JournalLine>(request.Lines.Count + 1);
        foreach (var line in request.Lines)
        {
            if (line.AccountId <= 0 ||
                line.Debit < 0 ||
                line.Credit < 0 ||
                (line.Debit == 0) == (line.Credit == 0) ||
                line.Debit > MaximumAmount ||
                line.Credit > MaximumAmount)
                throw Validation("lines", "Each line needs one positive debit or credit within the supported amount range.");
            if (decimal.Round(line.Debit, functionalCurrency.DecimalPlaces) != line.Debit ||
                decimal.Round(line.Credit, functionalCurrency.DecimalPlaces) != line.Credit)
                throw Validation(
                    "lines",
                    $"{company.FunctionalCurrencyCode} supports {functionalCurrency.DecimalPlaces} decimal places.");

            openingLines.Add(CreateOpeningLine(
                tenantId,
                companyId,
                line.AccountId,
                company.FunctionalCurrencyCode,
                line.Debit - line.Credit));
        }

        var difference = openingLines.Sum(line => line.FunctionalDebit) -
                         openingLines.Sum(line => line.FunctionalCredit);
        if (difference != 0)
        {
            if (Math.Abs(difference) > MaximumAmount)
                throw Validation("lines", "The Opening Balance Equity adjustment exceeds the supported amount range.");

            var openingEquityAccountId = await (
                from account in db.ChartOfAccounts
                join accountGroup in db.AccountGroups
                    on new { account.TenantId, account.CompanyId, GroupId = account.AccountGroupId }
                    equals new { accountGroup.TenantId, accountGroup.CompanyId, GroupId = accountGroup.Id }
                where account.CompanyId == companyId &&
                      account.Code == "3000" &&
                      account.IsActive &&
                      account.AllowPosting &&
                      accountGroup.AccountType == "Equity"
                select (long?)account.Id)
                .SingleOrDefaultAsync(cancellationToken);
            if (openingEquityAccountId is null)
                throw new AccountingConflictException(
                    "An active Opening Balance Equity account (3000) is required to balance the starting journal.");

            openingLines.Add(CreateOpeningLine(
                tenantId,
                companyId,
                openingEquityAccountId.Value,
                company.FunctionalCurrencyCode,
                -difference));
        }

        var generalJournal = await db.Journals.SingleOrDefaultAsync(
            journal => journal.CompanyId == companyId && journal.Code == "GEN" && journal.IsActive,
            cancellationToken);
        if (generalJournal is null)
            throw new AccountingConflictException("An active General journal (GEN) is required for starting balances.");

        var entry = await journalEntries.CreateDraftAsync(
            companyId,
            new CreateJournalEntryRequest(
                generalJournal.Id,
                firstPeriod.Id,
                year.StartsOn,
                openingDescription,
                openingLines.Select(line => new CreateJournalLineRequest(
                    line.AccountId,
                    line.TransactionCurrencyCode,
                    line.Debit,
                    line.Credit,
                    line.ExchangeRate,
                    line.Description)).ToArray()),
            cancellationToken);
        if (entry is null) return null;

        await transaction.CommitAsync(cancellationToken);
        return entry;
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
}
