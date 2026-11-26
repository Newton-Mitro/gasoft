using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public abstract class AccountingServiceBase
{
    protected readonly AppDbContext db;
    protected readonly ICurrentTenant currentTenant;

    protected AccountingServiceBase(AppDbContext db, ICurrentTenant currentTenant)
    {
        this.db = db;
        this.currentTenant = currentTenant;
    }

    protected const decimal MaximumAmount = 99_999_999_999_999.999999m;

    protected long EnsureTenant() =>
        currentTenant.TenantId ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");

    protected static string ValidatePeriodStatusReason(string? value)
    {
        var reason = Normalize(value);
        if (reason.Length is 0 or > 500)
            throw Validation("reason", "Enter a reason of 1–500 characters.");
        return reason;
    }

    protected async Task<AccountingPeriodCloseChecklistDto> BuildPeriodCloseChecklistAsync(
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

    protected async Task<FiscalYearCloseChecklistDto> BuildFiscalYearCloseChecklistAsync(
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

    protected async Task<IReadOnlyList<FiscalYearBalanceSheetBalance>> GetFiscalYearBalanceSheetBalancesAsync(
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

    protected async Task<IReadOnlyList<FiscalYearIncomeExpenseBalance>> GetFiscalYearIncomeExpenseBalancesAsync(
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

    protected static JournalLine CreateClosingLine(
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

    protected static JournalLine CreateOpeningLine(
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

    protected static string FiscalYearCloseBlockerMessage(FiscalYearCloseChecklistDto checklist)
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

    protected static string PeriodCloseBlockerMessage(AccountingPeriodCloseChecklistDto checklist)
    {
        var blockers = new List<string>();
        if (checklist.DraftJournalEntryCount > 0)
            blockers.Add($"{checklist.DraftJournalEntryCount} draft journal entr{(checklist.DraftJournalEntryCount == 1 ? "y" : "ies")}");
        if (checklist.UnreconciledBankTransactionCount > 0)
            blockers.Add($"{checklist.UnreconciledBankTransactionCount} unreconciled bank statement transaction{(checklist.UnreconciledBankTransactionCount == 1 ? string.Empty : "s")}");
        return $"Resolve period close checklist: {string.Join(" and ", blockers)}.";
    }

    protected sealed record FiscalYearBalanceSheetBalance(
        long AccountId,
        string Code,
        string AccountType,
        decimal Debit,
        decimal Credit);

    protected sealed record FiscalYearIncomeExpenseBalance(
        long AccountId,
        string Code,
        string AccountType,
        decimal Debit,
        decimal Credit);

    protected static void ValidatePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw Validation("pagination", "Page must be positive and page size must be between 1 and 100.");
        }
    }

    protected async Task EnsureCompanyAsync(long tenantId, long companyId, CancellationToken cancellationToken)
    {
        if (!await db.Companies.AnyAsync(
                company => company.TenantId == tenantId && company.Id == companyId,
                cancellationToken))
        {
            throw new KeyNotFoundException("Company was not found in this tenant.");
        }
    }

    protected async Task<JournalEntryDto[]> ToDtosWithReversalsAsync(
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

    protected async Task<bool> IsOperationalJournalEntryAsync(
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

    protected async Task SaveAsync(CancellationToken cancellationToken)
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

    protected static void ValidateDateRange(DateOnly startsOn, DateOnly endsOn, string label)
    {
        if (endsOn < startsOn)
        {
            throw Validation("endsOn", $"{label} end date must be on or after its start date.");
        }
    }

    protected static void ValidateCode(IDictionary<string, string[]> errors, string field, string code, int maxLength)
    {
        if (code.Length is 0 || code.Length > maxLength)
        {
            errors[field] = [$"Code is required and must be at most {maxLength} characters."];
        }
    }

    protected static void ValidateName(IDictionary<string, string[]> errors, string field, string name, int maxLength)
    {
        if (name.Length is 0 || name.Length > maxLength)
        {
            errors[field] = [$"Name is required and must be at most {maxLength} characters."];
        }
    }

    protected static void ThrowIfInvalid(IReadOnlyDictionary<string, string[]> errors)
    {
        if (errors.Count != 0)
        {
            throw new AccountingValidationException(errors);
        }
    }

    protected static AccountingValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    protected static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    protected static AccountGroupDto ToDto(AccountGroup group) =>
        new(group.Id, group.CompanyId, group.Code, group.Name, group.AccountType, group.NormalBalance);

    protected static AccountDto ToDto(ChartOfAccount account) =>
        new(account.Id, account.CompanyId, account.AccountGroupId, account.ParentId,
            account.Code, account.Name, account.AllowPosting, account.IsActive);

    protected static JournalDto ToDto(Journal journal) =>
        new(journal.Id, journal.CompanyId, journal.Code, journal.Name, journal.JournalType, journal.IsActive);

    protected static JournalEntryDto ToDto(JournalEntry entry) =>
        new(entry.Id, entry.CompanyId, entry.JournalId, entry.AccountingPeriodId, entry.EntryDate,
            entry.Description, entry.Status, entry.TotalDebit, entry.TotalCredit,
            entry.Lines.OrderBy(line => line.Id)
                .Select(line => new JournalLineDto(
                    line.Id, line.AccountId, line.TransactionCurrencyCode, line.Debit, line.Credit,
                    line.FunctionalDebit, line.FunctionalCredit, line.ExchangeRate, line.Description))
                .ToArray(),
            entry.ReversalOfEntryId);
}
