using Gasoft.Application.Abstractions;
using Gasoft.Application.Reports;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Infrastructure.Persistence;

public sealed class ReportingService(AppDbContext db) : IReportingService
{
    private static readonly string[] CashFlowCategories =
    [
        "Operating",
        "Investing",
        "Financing",
        "NonCashAdjustment",
        "WorkingCapitalAsset",
        "WorkingCapitalLiability"
    ];

    public async Task<CashFlowReportDto?> GetCashFlowAsync(
        long companyId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var currency = await GetCompanyCurrencyAsync(companyId, cancellationToken);
        if (currency is null) return null;

        var cashAccounts = await (
            from bank in db.BankAccounts.AsNoTracking()
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { bank.TenantId, bank.CompanyId, AccountId = bank.ChartOfAccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            where bank.CompanyId == companyId && bank.IsActive
            select new { AccountId = account.Id, account.Code, AccountName = account.Name, BankName = bank.Name })
            .Distinct()
            .ToListAsync(cancellationToken);
        var cashIds = cashAccounts.Select(account => account.AccountId).Distinct().ToArray();
        var openingBalances = await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            where line.CompanyId == companyId && cashIds.Contains(line.AccountId) &&
                  entry.Status == "Posted" && entry.EntryDate < fromDate
            group line by line.AccountId into grouped
            select new
            {
                AccountId = grouped.Key,
                Balance = grouped.Sum(line => line.FunctionalDebit - line.FunctionalCredit)
            }).ToDictionaryAsync(item => item.AccountId, item => item.Balance, cancellationToken);
        var periodCashLines = await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            where line.CompanyId == companyId && cashIds.Contains(line.AccountId) &&
                  entry.Status == "Posted" && entry.EntryDate >= fromDate && entry.EntryDate <= toDate
            select new
            {
                line.AccountId,
                line.JournalEntryId,
                line.FunctionalDebit,
                line.FunctionalCredit
            }).ToListAsync(cancellationToken);

        var cashAccountBalances = cashAccounts.Select(account =>
        {
            var opening = openingBalances.GetValueOrDefault(account.AccountId);
            var change = periodCashLines.Where(line => line.AccountId == account.AccountId)
                .Sum(line => line.FunctionalDebit - line.FunctionalCredit);
            return new CashFlowAccountBalanceDto(
                account.AccountId,
                account.Code,
                account.AccountName,
                account.BankName,
                opening,
                change,
                opening + change);
        }).ToArray();
        var cashEntries = periodCashLines.GroupBy(line => line.JournalEntryId)
            .Select(group => new
            {
                JournalEntryId = group.Key,
                Change = group.Sum(line => line.FunctionalDebit - line.FunctionalCredit)
            })
            .Where(entry => entry.Change != 0)
            .ToArray();
        var entryIds = cashEntries.Select(entry => entry.JournalEntryId).ToArray();
        var counterpartLines = await (
            from line in db.JournalLines.AsNoTracking()
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            where line.CompanyId == companyId && entryIds.Contains(line.JournalEntryId) &&
                  !cashIds.Contains(line.AccountId)
            select new
            {
                line.JournalEntryId,
                line.AccountId,
                account.Code,
                AccountName = account.Name,
                account.CashFlowCategory,
                line.FunctionalDebit,
                line.FunctionalCredit
            }).ToListAsync(cancellationToken);
        var counterpartsByEntry = counterpartLines.ToLookup(line => line.JournalEntryId);
        var directFlows = new Dictionary<
            (string Category, long AccountId, string Code, string Name),
            (decimal Inflows, decimal Outflows)>();
        foreach (var cashEntry in cashEntries)
        {
            var cashIn = cashEntry.Change > 0;
            var amount = Math.Abs(cashEntry.Change);
            var lines = counterpartsByEntry[cashEntry.JournalEntryId]
                .Select(line => new
                {
                    line.AccountId,
                    line.Code,
                    line.AccountName,
                    Category = ToDirectCategory(line.CashFlowCategory),
                    Amount = cashIn ? line.FunctionalCredit : line.FunctionalDebit
                })
                .Where(line => line.Amount > 0)
                .ToArray();
            var counterpartTotal = lines.Sum(line => line.Amount);
            var allocated = 0m;
            for (var index = 0; index < lines.Length; index++)
            {
                var line = lines[index];
                var share = counterpartTotal == 0
                    ? 0
                    : index == lines.Length - 1
                        ? Math.Max(amount - allocated, 0)
                        : decimal.Round(amount * line.Amount / counterpartTotal, 6, MidpointRounding.ToZero);
                allocated += share;
                AddDirectFlow(
                    directFlows, line.Category, line.AccountId, line.Code, line.AccountName, cashIn, share);
            }
            var unclassifiedRemainder = Math.Max(amount - allocated, 0);
            if (unclassifiedRemainder > 0)
                AddDirectFlow(directFlows, "Unclassified", 0, "—", "Unclassified cash movement", cashIn, unclassifiedRemainder);
        }

        var operating = CreateDirectSection("Operating", directFlows);
        var investing = CreateDirectSection("Investing", directFlows);
        var financing = CreateDirectSection("Financing", directFlows);
        var unclassifiedSection = CreateDirectSection("Unclassified", directFlows);
        var incomeLines = await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where line.CompanyId == companyId && entry.Status == "Posted" &&
                  entry.EntryDate >= fromDate && entry.EntryDate <= toDate &&
                  (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense")
            select new { accountGroup.AccountType, line.FunctionalDebit, line.FunctionalCredit })
            .ToListAsync(cancellationToken);
        var netIncome = incomeLines.Sum(line => line.AccountType == "Revenue"
            ? line.FunctionalCredit - line.FunctionalDebit
            : line.FunctionalDebit - line.FunctionalCredit);

        var classifiedAccounts = await db.ChartOfAccounts.AsNoTracking()
            .Where(account => account.CompanyId == companyId &&
                (account.CashFlowCategory == "NonCashAdjustment" ||
                 account.CashFlowCategory == "WorkingCapitalAsset" ||
                 account.CashFlowCategory == "WorkingCapitalLiability"))
            .Select(account => new { account.Id, account.CashFlowCategory })
            .ToListAsync(cancellationToken);
        var classifiedIds = classifiedAccounts.Select(account => account.Id).ToArray();
        var classifiedBalances = await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            where line.CompanyId == companyId && classifiedIds.Contains(line.AccountId) &&
                  entry.Status == "Posted" && entry.EntryDate <= toDate
            group line by new { line.AccountId, entry.EntryDate } into grouped
            select new
            {
                grouped.Key.AccountId,
                grouped.Key.EntryDate,
                Debit = grouped.Sum(line => line.FunctionalDebit),
                Credit = grouped.Sum(line => line.FunctionalCredit)
            }).ToListAsync(cancellationToken);
        var nonCashAdjustments = classifiedAccounts
            .Where(account => account.CashFlowCategory == "NonCashAdjustment")
            .Sum(account => classifiedBalances
                .Where(balance => balance.AccountId == account.Id && balance.EntryDate >= fromDate)
                .Sum(balance => balance.Debit - balance.Credit));
        var workingCapitalAdjustments = 0m;
        foreach (var account in classifiedAccounts.Where(account =>
                     account.CashFlowCategory is "WorkingCapitalAsset" or "WorkingCapitalLiability"))
        {
            var opening = classifiedBalances.Where(balance =>
                balance.AccountId == account.Id && balance.EntryDate < fromDate)
                .Sum(balance => balance.Debit - balance.Credit);
            var closing = classifiedBalances.Where(balance =>
                balance.AccountId == account.Id && balance.EntryDate <= toDate)
                .Sum(balance => balance.Debit - balance.Credit);
            workingCapitalAdjustments -= closing - opening;
        }

        var actualCashMovement = cashEntries.Sum(entry => entry.Change);
        var indirectOperating = netIncome + nonCashAdjustments + workingCapitalAdjustments;
        var indirectCalculatedChange = indirectOperating + investing.Net + financing.Net + unclassifiedSection.Net;
        var openingBalance = cashAccountBalances.Sum(account => account.OpeningBalance);
        return new CashFlowReportDto(
            companyId,
            currency,
            fromDate,
            toDate,
            cashAccountBalances,
            new DirectCashFlowDto(operating, investing, financing, unclassifiedSection, actualCashMovement),
            new IndirectCashFlowDto(
                netIncome,
                nonCashAdjustments,
                workingCapitalAdjustments,
                indirectOperating,
                investing.Net,
                financing.Net,
                unclassifiedSection.Net,
                unclassifiedSection.Inflows,
                unclassifiedSection.Outflows,
                indirectCalculatedChange,
                actualCashMovement,
                actualCashMovement - indirectCalculatedChange),
            openingBalance,
            openingBalance + actualCashMovement,
            actualCashMovement);
    }

    public async Task<IReadOnlyList<CashFlowAccountDto>> GetCashFlowAccountsAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var cashAccountIds = await db.BankAccounts.AsNoTracking()
            .Where(bank => bank.CompanyId == companyId)
            .Select(bank => bank.ChartOfAccountId)
            .ToArrayAsync(cancellationToken);
        return await (
            from account in db.ChartOfAccounts.AsNoTracking()
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == companyId && account.IsActive && account.AllowPosting
            orderby account.Code
            select new CashFlowAccountDto(
                account.Id,
                account.Code,
                account.Name,
                accountGroup.AccountType,
                account.CashFlowCategory,
                cashAccountIds.Contains(account.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<CashFlowCategoryDto?> UpdateCashFlowCategoryAsync(
        long companyId,
        long accountId,
        CashFlowCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var category = request.Category?.Trim();
        if (category is not null && !CashFlowCategories.Contains(category))
            throw Validation("category", "Choose a valid cash-flow classification or leave it unclassified.");

        var account = await (
            from chart in db.ChartOfAccounts
            join accountGroup in db.AccountGroups
                on new { chart.TenantId, chart.CompanyId, chart.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where chart.CompanyId == companyId && chart.Id == accountId
            select new { Account = chart, accountGroup.AccountType })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null) return null;

        if ((category == "NonCashAdjustment" && account.AccountType is not ("Revenue" or "Expense")) ||
            (category == "WorkingCapitalAsset" && account.AccountType != "Asset") ||
            (category == "WorkingCapitalLiability" && account.AccountType != "Liability"))
            throw Validation("category", "Non-cash adjustments must use revenue/expense accounts; working-capital categories must use their corresponding balance-sheet account type.");
        if (category is not null && await db.BankAccounts.AnyAsync(
                bank => bank.CompanyId == companyId && bank.ChartOfAccountId == accountId,
                cancellationToken))
            throw Validation("category", "Cash accounts are classified automatically and cannot be assigned a counterparty category.");

        account.Account.CashFlowCategory = category;
        await db.SaveChangesAsync(cancellationToken);
        return new CashFlowCategoryDto(accountId, category);
    }

    public async Task<RetainedEarningsRollForwardDto?> GetRetainedEarningsRollForwardAsync(
        long companyId,
        long fiscalYearId,
        CancellationToken cancellationToken)
    {
        var currency = await GetCompanyCurrencyAsync(companyId, cancellationToken);
        if (currency is null) return null;

        var year = await db.FiscalYears.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == fiscalYearId, cancellationToken);
        if (year is null) return null;

        var closing = await db.FiscalYearClosings.AsNoTracking()
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.FiscalYearId == fiscalYearId, cancellationToken);

        var retainedAccount = closing is not null
            ? await (
                from account in db.ChartOfAccounts.AsNoTracking()
                join accountGroup in db.AccountGroups.AsNoTracking()
                    on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                    equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
                where account.CompanyId == companyId && account.Id == closing.RetainedEarningsAccountId
                select new { account.Id, account.Code, account.Name, accountGroup.AccountType })
                .SingleOrDefaultAsync(cancellationToken)
            : await (
                from account in db.ChartOfAccounts.AsNoTracking()
                join accountGroup in db.AccountGroups.AsNoTracking()
                    on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                    equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
                where account.CompanyId == companyId && account.IsActive && account.AllowPosting &&
                      accountGroup.AccountType == "Equity" &&
                      (account.Code == "3100" || account.Name.Contains("Retained"))
                orderby account.Code
                select new { account.Id, account.Code, account.Name, accountGroup.AccountType })
                .FirstOrDefaultAsync(cancellationToken);

        if (retainedAccount is null) return null;

        var beginningBalance = await GetAccountBalanceAsync(companyId, retainedAccount.Id, year.StartsOn.AddDays(-1), cancellationToken);
        var netIncomeForYear = await GetFiscalYearNetIncomeAsync(companyId, year.StartsOn, year.EndsOn, cancellationToken);
        var endingBalance = await GetAccountBalanceAsync(companyId, retainedAccount.Id, year.EndsOn, cancellationToken);
        var ownerDrawsOrDividends = 0m;
        var endingRetainedEarnings = beginningBalance + netIncomeForYear - ownerDrawsOrDividends;

        return new RetainedEarningsRollForwardDto(
            companyId,
            currency,
            fiscalYearId,
            year.StartsOn,
            year.EndsOn,
            retainedAccount.Id,
            retainedAccount.Code,
            retainedAccount.Name,
            beginningBalance,
            netIncomeForYear,
            ownerDrawsOrDividends,
            endingRetainedEarnings,
            endingBalance - endingRetainedEarnings);
    }

    public async Task<TrialBalanceReportDto?> GetTrialBalanceAsync(
        long companyId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var currency = await GetCompanyCurrencyAsync(companyId, cancellationToken);
        if (currency is null) return null;
        var rows = await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where line.CompanyId == companyId && entry.Status == "Posted" &&
                  (!fromDate.HasValue || entry.EntryDate >= fromDate.Value) &&
                  (!toDate.HasValue || entry.EntryDate <= toDate.Value)
            group line by new { account.Id, account.Code, account.Name, accountGroup.AccountType } into grouped
            orderby grouped.Key.Code
            select new
            {
                grouped.Key.Id,
                grouped.Key.Code,
                grouped.Key.Name,
                grouped.Key.AccountType,
                Debit = grouped.Sum(line => line.FunctionalDebit),
                Credit = grouped.Sum(line => line.FunctionalCredit)
            }).ToListAsync(cancellationToken);
        var accounts = rows.Select(row => new TrialBalanceAccountDto(
            row.Id,
            row.Code,
            row.Name,
            row.AccountType,
            Math.Max(row.Debit - row.Credit, 0),
            Math.Max(row.Credit - row.Debit, 0))).ToArray();
        return new TrialBalanceReportDto(
            companyId,
            currency,
            fromDate,
            toDate,
            accounts,
            accounts.Sum(account => account.Debit),
            accounts.Sum(account => account.Credit));
    }

    public async Task<IncomeStatementReportDto?> GetIncomeStatementAsync(
        long companyId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken)
    {
        var currency = await GetCompanyCurrencyAsync(companyId, cancellationToken);
        if (currency is null) return null;
        var rows = await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where line.CompanyId == companyId && entry.Status == "Posted" &&
                  (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense") &&
                  (!fromDate.HasValue || entry.EntryDate >= fromDate.Value) &&
                  (!toDate.HasValue || entry.EntryDate <= toDate.Value)
            group line by new { account.Id, account.Code, account.Name, accountGroup.AccountType } into grouped
            orderby grouped.Key.AccountType, grouped.Key.Code
            select new
            {
                grouped.Key.Id,
                grouped.Key.Code,
                grouped.Key.Name,
                grouped.Key.AccountType,
                Debit = grouped.Sum(line => line.FunctionalDebit),
                Credit = grouped.Sum(line => line.FunctionalCredit)
            }).ToListAsync(cancellationToken);
        var accounts = rows.Select(row => new IncomeStatementAccountDto(
            row.Id,
            row.Code,
            row.Name,
            row.AccountType,
            row.AccountType == "Revenue" ? row.Credit - row.Debit : row.Debit - row.Credit)).ToArray();
        var revenue = accounts.Where(account => account.AccountType == "Revenue").Sum(account => account.Amount);
        var expenses = accounts.Where(account => account.AccountType == "Expense").Sum(account => account.Amount);
        return new IncomeStatementReportDto(
            companyId,
            currency,
            fromDate,
            toDate,
            accounts,
            revenue,
            expenses,
            revenue - expenses);
    }

    public async Task<BalanceSheetReportDto?> GetBalanceSheetAsync(
        long companyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken)
    {
        var currency = await GetCompanyCurrencyAsync(companyId, cancellationToken);
        if (currency is null) return null;
        var balances = await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where line.CompanyId == companyId && entry.Status == "Posted" && entry.EntryDate <= asOfDate
            group line by new { account.Id, account.Code, account.Name, accountGroup.AccountType } into grouped
            orderby grouped.Key.AccountType, grouped.Key.Code
            select new
            {
                grouped.Key.Id,
                grouped.Key.Code,
                grouped.Key.Name,
                grouped.Key.AccountType,
                Debit = grouped.Sum(line => line.FunctionalDebit),
                Credit = grouped.Sum(line => line.FunctionalCredit)
            }).ToListAsync(cancellationToken);

        var liabilities = balances.Where(account => account.AccountType == "Liability")
            .Sum(account => account.Credit - account.Debit);
        var equity = balances.Where(account => account.AccountType == "Equity")
            .Sum(account => account.Credit - account.Debit);
        var revenue = balances.Where(account => account.AccountType == "Revenue")
            .Sum(account => account.Credit - account.Debit);
        var expenses = balances.Where(account => account.AccountType == "Expense")
            .Sum(account => account.Debit - account.Credit);
        var currentEarnings = revenue - expenses;
        var assets = balances.Where(account => account.AccountType == "Asset").ToArray();
        var totalAssets = assets.Sum(account => account.Debit - account.Credit);
        var accounts = balances
            .Where(account => account.AccountType is "Asset" or "Liability" or "Equity")
            .Select(account => new BalanceSheetAccountDto(
                account.Id,
                account.Code,
                account.Name,
                account.AccountType,
                account.AccountType == "Asset"
                    ? account.Debit - account.Credit
                    : account.Credit - account.Debit))
            .Where(account => account.Balance != 0)
            .ToList();
        if (currentEarnings != 0)
            accounts.Add(new BalanceSheetAccountDto(null, "CURRENT-EARNINGS", "Current earnings", "Equity", currentEarnings));

        var totalLiabilitiesAndEquity = liabilities + equity + currentEarnings;
        return new BalanceSheetReportDto(
            companyId,
            currency,
            asOfDate,
            accounts,
            totalAssets,
            liabilities,
            equity,
            currentEarnings,
            totalLiabilitiesAndEquity,
            totalAssets - totalLiabilitiesAndEquity);
    }

    public async Task<AgingReportDto?> GetReceivablesAgingAsync(
        long companyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken)
    {
        var currency = await GetCompanyCurrencyAsync(companyId, cancellationToken);
        if (currency is null) return null;
        var allocated = await (
            from allocation in db.PaymentAllocations.AsNoTracking()
            join payment in db.Payments.AsNoTracking()
                on new { allocation.TenantId, allocation.CompanyId, allocation.PaymentId }
                equals new { payment.TenantId, payment.CompanyId, PaymentId = payment.Id }
            where allocation.CompanyId == companyId && allocation.SalesInvoiceId.HasValue &&
                  payment.Status == "Posted" && payment.PaymentDate <= asOfDate
            group allocation by allocation.SalesInvoiceId!.Value into grouped
            select new { DocumentId = grouped.Key, Amount = grouped.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.DocumentId, item => item.Amount, cancellationToken);
        var documents = await (
            from invoice in db.SalesInvoices.AsNoTracking()
            join party in db.Parties.AsNoTracking() on invoice.CustomerId equals party.Id
            where invoice.CompanyId == companyId && invoice.Status == "Posted" &&
                  invoice.InvoiceDate <= asOfDate
            orderby invoice.DueDate, party.DisplayName, invoice.InvoiceNumber
            select new AgingSourceDocument(
                invoice.Id,
                invoice.InvoiceNumber,
                party.PartyNo,
                party.DisplayName,
                invoice.InvoiceDate,
                invoice.DueDate,
                invoice.CurrencyCode,
                invoice.GrandTotal))
            .ToListAsync(cancellationToken);
        return BuildAgingReport(companyId, currency, asOfDate, documents, allocated);
    }

    public async Task<AgingReportDto?> GetPayablesAgingAsync(
        long companyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken)
    {
        var currency = await GetCompanyCurrencyAsync(companyId, cancellationToken);
        if (currency is null) return null;
        var allocated = await (
            from allocation in db.PaymentAllocations.AsNoTracking()
            join payment in db.Payments.AsNoTracking()
                on new { allocation.TenantId, allocation.CompanyId, allocation.PaymentId }
                equals new { payment.TenantId, payment.CompanyId, PaymentId = payment.Id }
            where allocation.CompanyId == companyId && allocation.PurchaseBillId.HasValue &&
                  payment.Status == "Posted" && payment.PaymentDate <= asOfDate
            group allocation by allocation.PurchaseBillId!.Value into grouped
            select new { DocumentId = grouped.Key, Amount = grouped.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.DocumentId, item => item.Amount, cancellationToken);
        var documents = await (
            from bill in db.PurchaseBills.AsNoTracking()
            join party in db.Parties.AsNoTracking() on bill.SupplierId equals party.Id
            where bill.CompanyId == companyId && bill.Status == "Posted" && bill.BillDate <= asOfDate
            orderby bill.DueDate, party.DisplayName, bill.BillNumber
            select new AgingSourceDocument(
                bill.Id,
                bill.BillNumber,
                party.PartyNo,
                party.DisplayName,
                bill.BillDate,
                bill.DueDate,
                bill.CurrencyCode,
                bill.GrandTotal))
            .ToListAsync(cancellationToken);
        return BuildAgingReport(companyId, currency, asOfDate, documents, allocated);
    }

    private static AgingReportDto BuildAgingReport(
        long companyId,
        string companyCurrency,
        DateOnly asOfDate,
        IReadOnlyList<AgingSourceDocument> documents,
        IReadOnlyDictionary<long, decimal> allocated)
    {
        var rows = documents.Select(document =>
            {
                var outstanding = document.OriginalAmount - allocated.GetValueOrDefault(document.Id);
                var daysPastDue = Math.Max(asOfDate.DayNumber - document.DueDate.DayNumber, 0);
                return new AgingDocumentDto(
                    document.Id,
                    document.DocumentNumber,
                    document.PartyNumber,
                    document.PartyName,
                    document.DocumentDate,
                    document.DueDate,
                    document.CurrencyCode,
                    document.OriginalAmount,
                    outstanding,
                    daysPastDue,
                    GetAgingBucket(daysPastDue));
            })
            .Where(document => document.OutstandingAmount > 0)
            .ToArray();
        var totals = rows.GroupBy(document => document.CurrencyCode)
            .OrderBy(group => group.Key)
            .Select(group => new AgingCurrencyTotalDto(
                group.Key,
                group.Where(document => document.DaysPastDue == 0).Sum(document => document.OutstandingAmount),
                group.Where(document => document.DaysPastDue is >= 1 and <= 30).Sum(document => document.OutstandingAmount),
                group.Where(document => document.DaysPastDue is >= 31 and <= 60).Sum(document => document.OutstandingAmount),
                group.Where(document => document.DaysPastDue is >= 61 and <= 90).Sum(document => document.OutstandingAmount),
                group.Where(document => document.DaysPastDue > 90).Sum(document => document.OutstandingAmount),
                group.Sum(document => document.OutstandingAmount)))
            .ToArray();
        return new AgingReportDto(companyId, companyCurrency, asOfDate, rows, totals);
    }

    private static string GetAgingBucket(int daysPastDue) => daysPastDue switch
    {
        0 => "Current",
        <= 30 => "1-30 days",
        <= 60 => "31-60 days",
        <= 90 => "61-90 days",
        _ => "Over 90 days"
    };

    private static string ToDirectCategory(string? category) => category switch
    {
        "Investing" => "Investing",
        "Financing" => "Financing",
        "Operating" or "NonCashAdjustment" or "WorkingCapitalAsset" or "WorkingCapitalLiability" => "Operating",
        _ => "Unclassified"
    };

    private static void AddDirectFlow(
        IDictionary<(string Category, long AccountId, string Code, string Name), (decimal Inflows, decimal Outflows)> flows,
        string category,
        long accountId,
        string code,
        string name,
        bool cashIn,
        decimal amount)
    {
        var key = (category, accountId, code, name);
        flows.TryGetValue(key, out var existing);
        flows[key] = cashIn
            ? (existing.Inflows + amount, existing.Outflows)
            : (existing.Inflows, existing.Outflows + amount);
    }

    private static CashFlowSectionDto CreateDirectSection(
        string category,
        IReadOnlyDictionary<(string Category, long AccountId, string Code, string Name), (decimal Inflows, decimal Outflows)> flows)
    {
        var accounts = flows.Where(flow => flow.Key.Category == category)
            .OrderBy(flow => flow.Key.Code)
            .Select(flow => new CashFlowAccountFlowDto(
                flow.Key.AccountId,
                flow.Key.Code,
                flow.Key.Name,
                flow.Value.Inflows,
                flow.Value.Outflows,
                flow.Value.Inflows - flow.Value.Outflows))
            .ToArray();
        var inflows = accounts.Sum(account => account.Inflows);
        var outflows = accounts.Sum(account => account.Outflows);
        return new CashFlowSectionDto(category, inflows, outflows, inflows - outflows, accounts);
    }

    private sealed record AgingSourceDocument(
        long Id,
        string DocumentNumber,
        string PartyNumber,
        string PartyName,
        DateOnly DocumentDate,
        DateOnly DueDate,
        string CurrencyCode,
        decimal OriginalAmount);

    private async Task<decimal> GetAccountBalanceAsync(
        long companyId,
        long accountId,
        DateOnly asOfDate,
        CancellationToken cancellationToken)
    {
        return await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            where line.CompanyId == companyId && line.AccountId == accountId &&
                  entry.Status == "Posted" && entry.EntryDate <= asOfDate
            select line.FunctionalDebit - line.FunctionalCredit)
            .SumAsync(cancellationToken);
    }

    private async Task<decimal> GetFiscalYearNetIncomeAsync(
        long companyId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from line in db.JournalLines.AsNoTracking()
            join entry in db.JournalEntries.AsNoTracking()
                on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
            join account in db.ChartOfAccounts.AsNoTracking()
                on new { line.TenantId, line.CompanyId, AccountId = line.AccountId }
                equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where line.CompanyId == companyId && entry.Status == "Posted" &&
                  entry.EntryDate >= fromDate && entry.EntryDate <= toDate &&
                  (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense")
            group new { accountGroup.AccountType, line.FunctionalDebit, line.FunctionalCredit } by accountGroup.AccountType into grouped
            select new
            {
                AccountType = grouped.Key,
                Amount = grouped.Sum(item => item.AccountType == "Revenue"
                    ? item.FunctionalCredit - item.FunctionalDebit
                    : item.FunctionalDebit - item.FunctionalCredit)
            }).ToListAsync(cancellationToken);

        return rows.Where(row => row.AccountType == "Revenue").Sum(row => row.Amount) -
               rows.Where(row => row.AccountType == "Expense").Sum(row => row.Amount);
    }

    private async Task<string?> GetCompanyCurrencyAsync(long companyId, CancellationToken cancellationToken) =>
        await db.Companies.AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => company.FunctionalCurrencyCode)
            .SingleOrDefaultAsync(cancellationToken);

    private static ReportValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}
