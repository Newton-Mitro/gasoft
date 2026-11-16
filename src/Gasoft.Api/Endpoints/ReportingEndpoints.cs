using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/reports").RequireAuthorization();

        companies.MapGet("/cash-flow/accounts", async (
            long companyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var cashAccountIds = await db.BankAccounts.AsNoTracking()
                .Where(bank => bank.CompanyId == companyId)
                .Select(bank => bank.ChartOfAccountId)
                .ToArrayAsync(cancellationToken);
            var accounts = await (
                from account in db.ChartOfAccounts.AsNoTracking()
                join accountGroup in db.AccountGroups.AsNoTracking()
                    on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                    equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
                where account.CompanyId == companyId && account.IsActive && account.AllowPosting
                orderby account.Code
                select new
                {
                    accountId = account.Id,
                    account.Code,
                    account.Name,
                    accountType = accountGroup.AccountType,
                    account.CashFlowCategory,
                    isCashAccount = cashAccountIds.Contains(account.Id)
                }).ToListAsync(cancellationToken);
            return Results.Ok(accounts);
        });

        companies.MapPut("/cash-flow/accounts/{accountId:long}", async (
            long companyId,
            long accountId,
            CashFlowCategoryRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var category = request.Category?.Trim();
            var validCategories = new[]
            {
                "Operating", "Investing", "Financing", "NonCashAdjustment",
                "WorkingCapitalAsset", "WorkingCapitalLiability"
            };
            if (category is not null && !validCategories.Contains(category))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["category"] = ["Choose a valid cash-flow classification or leave it unclassified."]
                });
            var account = await (
                from chart in db.ChartOfAccounts
                join accountGroup in db.AccountGroups
                    on new { chart.TenantId, chart.CompanyId, chart.AccountGroupId }
                    equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
                where chart.CompanyId == companyId && chart.Id == accountId
                select new { Account = chart, accountGroup.AccountType })
                .SingleOrDefaultAsync(cancellationToken);
            if (account is null) return Results.NotFound();
            if ((category == "NonCashAdjustment" && account.AccountType is not ("Revenue" or "Expense")) ||
                (category == "WorkingCapitalAsset" && account.AccountType != "Asset") ||
                (category == "WorkingCapitalLiability" && account.AccountType != "Liability"))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["category"] = ["Non-cash adjustments must use revenue/expense accounts; working-capital categories must use their corresponding balance-sheet account type."]
                });
            if (category is not null && await db.BankAccounts.AnyAsync(
                    bank => bank.CompanyId == companyId && bank.ChartOfAccountId == accountId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["category"] = ["Cash accounts are classified automatically and cannot be assigned a counterparty category."]
                });
            account.Account.CashFlowCategory = category;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new { accountId, category });
        });

        companies.MapGet("/cash-flow", async (
            long companyId,
            DateOnly? fromDate,
            DateOnly? toDate,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!fromDate.HasValue || !toDate.HasValue || fromDate > toDate)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["dateRange"] = ["Provide a start and end date; the end date must be on or after the start date."]
                });

            var currency = await db.Companies.AsNoTracking()
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleOrDefaultAsync(cancellationToken);
            if (currency is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

            var cashAccounts = await (
                from bank in db.BankAccounts.AsNoTracking()
                join account in db.ChartOfAccounts.AsNoTracking()
                    on new { bank.TenantId, bank.CompanyId, AccountId = bank.ChartOfAccountId }
                    equals new { account.TenantId, account.CompanyId, AccountId = account.Id }
                where bank.CompanyId == companyId && bank.IsActive
                select new { AccountId = account.Id, account.Code, AccountName = account.Name, BankName = bank.Name })
                .Distinct().ToListAsync(cancellationToken);
            var cashIds = cashAccounts.Select(account => account.AccountId).Distinct().ToArray();
            var openingBalances = await (
                from line in db.JournalLines.AsNoTracking()
                join entry in db.JournalEntries.AsNoTracking()
                    on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                    equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
                where line.CompanyId == companyId && cashIds.Contains(line.AccountId) &&
                      entry.Status == "Posted" && entry.EntryDate < fromDate.Value
                group line by line.AccountId into grouped
                select new { AccountId = grouped.Key, Balance = grouped.Sum(line => line.FunctionalDebit - line.FunctionalCredit) })
                .ToDictionaryAsync(item => item.AccountId, item => item.Balance, cancellationToken);

            var periodCashLines = await (
                from line in db.JournalLines.AsNoTracking()
                join entry in db.JournalEntries.AsNoTracking()
                    on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                    equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
                where line.CompanyId == companyId && cashIds.Contains(line.AccountId) &&
                      entry.Status == "Posted" && entry.EntryDate >= fromDate.Value && entry.EntryDate <= toDate.Value
                select new { line.AccountId, line.JournalEntryId, entry.EntryDate, line.FunctionalDebit, line.FunctionalCredit })
                .ToListAsync(cancellationToken);

            var openingByAccount = cashAccounts.Select(account => new
            {
                account.AccountId,
                account.Code,
                account.AccountName,
                account.BankName,
                balance = openingBalances.GetValueOrDefault(account.AccountId)
            }).ToArray();
            var cashAccountBalances = openingByAccount.Select(account =>
            {
                var change = periodCashLines.Where(line => line.AccountId == account.AccountId)
                    .Sum(line => line.FunctionalDebit - line.FunctionalCredit);
                return new
                {
                    account.AccountId,
                    account.Code,
                    account.AccountName,
                    account.BankName,
                    openingBalance = account.balance,
                    cashChange = change,
                    closingBalance = account.balance + change
                };
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

            var directFlows = new Dictionary<(string Category, long AccountId, string Code, string Name), (decimal Inflows, decimal Outflows)>();
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
                    AddDirectFlow(directFlows, line.Category, line.AccountId, line.Code, line.AccountName, cashIn, share);
                }
                var unclassifiedRemainder = Math.Max(amount - allocated, 0);
                if (unclassifiedRemainder > 0)
                    AddDirectFlow(directFlows, "Unclassified", 0, "—", "Unclassified cash movement", cashIn, unclassifiedRemainder);
            }

            var direct = new[]
            {
                CreateDirectSection("Operating", directFlows),
                CreateDirectSection("Investing", directFlows),
                CreateDirectSection("Financing", directFlows),
                CreateDirectSection("Unclassified", directFlows)
            };

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
                      entry.EntryDate >= fromDate.Value && entry.EntryDate <= toDate.Value &&
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
                .Select(account => new { account.Id, account.Code, account.Name, account.CashFlowCategory })
                .ToListAsync(cancellationToken);
            var classifiedIds = classifiedAccounts.Select(account => account.Id).ToArray();
            var classifiedBalances = await (
                from line in db.JournalLines.AsNoTracking()
                join entry in db.JournalEntries.AsNoTracking()
                    on new { line.TenantId, line.CompanyId, EntryId = line.JournalEntryId }
                    equals new { entry.TenantId, entry.CompanyId, EntryId = entry.Id }
                where line.CompanyId == companyId && classifiedIds.Contains(line.AccountId) &&
                      entry.Status == "Posted" && entry.EntryDate <= toDate.Value
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
                    .Where(balance => balance.AccountId == account.Id && balance.EntryDate >= fromDate.Value)
                    .Sum(balance => balance.Debit - balance.Credit));
            var workingCapitalAdjustments = 0m;
            foreach (var account in classifiedAccounts.Where(account =>
                         account.CashFlowCategory is "WorkingCapitalAsset" or "WorkingCapitalLiability"))
            {
                var opening = classifiedBalances.Where(balance =>
                    balance.AccountId == account.Id && balance.EntryDate < fromDate.Value)
                    .Sum(balance => balance.Debit - balance.Credit);
                var closing = classifiedBalances.Where(balance =>
                    balance.AccountId == account.Id && balance.EntryDate <= toDate.Value)
                    .Sum(balance => balance.Debit - balance.Credit);
                var change = closing - opening;
                workingCapitalAdjustments -= change;
            }
            var directInvesting = direct.Single(section => section.Category == "Investing").Net;
            var directFinancing = direct.Single(section => section.Category == "Financing").Net;
            var unclassifiedSection = direct.Single(section => section.Category == "Unclassified");
            var directUnclassified = unclassifiedSection.Net;
            var indirectOperating = netIncome + nonCashAdjustments + workingCapitalAdjustments;
            var actualCashMovement = cashEntries.Sum(entry => entry.Change);
            var indirectCalculatedChange = indirectOperating + directInvesting + directFinancing + directUnclassified;
            var openingBalance = openingByAccount.Sum(account => account.balance);

            return Results.Ok(new
            {
                companyId,
                currencyCode = currency,
                from = fromDate,
                to = toDate,
                cashAccounts = cashAccountBalances,
                direct = new
                {
                    operating = direct.Single(section => section.Category == "Operating"),
                    investing = direct.Single(section => section.Category == "Investing"),
                    financing = direct.Single(section => section.Category == "Financing"),
                    unclassified = direct.Single(section => section.Category == "Unclassified"),
                    netCashIncrease = actualCashMovement
                },
                indirect = new
                {
                    netIncome,
                    nonCashAdjustments,
                    workingCapitalAdjustments,
                    operatingCashFlow = indirectOperating,
                    investingCashFlow = directInvesting,
                    financingCashFlow = directFinancing,
                    unclassifiedCashFlow = directUnclassified,
                    unclassifiedInflows = unclassifiedSection.Inflows,
                    unclassifiedOutflows = unclassifiedSection.Outflows,
                    netCashIncrease = indirectCalculatedChange,
                    actualCashMovement,
                    reconciliationDifference = actualCashMovement - indirectCalculatedChange
                },
                openingBalance,
                closingBalance = openingBalance + actualCashMovement,
                actualCashMovement
            });
        })
        .WithName("GetCashFlow");

        companies.MapGet("/trial-balance", async (
            long companyId,
            DateOnly? fromDate,
            DateOnly? toDate,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["to"] = ["The end date must be on or after the start date."]
                });
            }

            var currency = await db.Companies.AsNoTracking()
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleOrDefaultAsync(cancellationToken);
            if (currency is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

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
                where line.CompanyId == companyId &&
                      entry.Status == "Posted" &&
                      (!fromDate.HasValue || entry.EntryDate >= fromDate.Value) &&
                      (!toDate.HasValue || entry.EntryDate <= toDate.Value)
                group line by new { account.Id, account.Code, account.Name, accountGroup.AccountType } into grouped
                orderby grouped.Key.Code
                select new
                {
                    accountId = grouped.Key.Id,
                    accountCode = grouped.Key.Code,
                    accountName = grouped.Key.Name,
                    accountType = grouped.Key.AccountType,
                    debit = grouped.Sum(line => line.FunctionalDebit),
                    credit = grouped.Sum(line => line.FunctionalCredit)
                }).ToListAsync(cancellationToken);

            var accounts = rows.Select(row => new
            {
                row.accountId,
                row.accountCode,
                row.accountName,
                row.accountType,
                debit = Math.Max(row.debit - row.credit, 0),
                credit = Math.Max(row.credit - row.debit, 0)
            }).ToArray();

            return Results.Ok(new
            {
                companyId,
                currencyCode = currency,
                from = fromDate,
                to = toDate,
                accounts,
                totalDebit = accounts.Sum(account => account.debit),
                totalCredit = accounts.Sum(account => account.credit)
            });
        });

        companies.MapGet("/income-statement", async (
            long companyId,
            DateOnly? fromDate,
            DateOnly? toDate,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["to"] = ["The end date must be on or after the start date."]
                });
            }

            var currency = await db.Companies.AsNoTracking()
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleOrDefaultAsync(cancellationToken);
            if (currency is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

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
                where line.CompanyId == companyId &&
                      entry.Status == "Posted" &&
                      (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense") &&
                      (!fromDate.HasValue || entry.EntryDate >= fromDate.Value) &&
                      (!toDate.HasValue || entry.EntryDate <= toDate.Value)
                group line by new { account.Id, account.Code, account.Name, accountGroup.AccountType } into grouped
                orderby grouped.Key.AccountType, grouped.Key.Code
                select new
                {
                    accountId = grouped.Key.Id,
                    accountCode = grouped.Key.Code,
                    accountName = grouped.Key.Name,
                    accountType = grouped.Key.AccountType,
                    debit = grouped.Sum(line => line.FunctionalDebit),
                    credit = grouped.Sum(line => line.FunctionalCredit)
                }).ToListAsync(cancellationToken);

            var accounts = rows.Select(row => new
            {
                row.accountId,
                row.accountCode,
                row.accountName,
                row.accountType,
                amount = row.accountType == "Revenue" ? row.credit - row.debit : row.debit - row.credit
            }).ToArray();
            var revenue = accounts.Where(account => account.accountType == "Revenue").Sum(account => account.amount);
            var expenses = accounts.Where(account => account.accountType == "Expense").Sum(account => account.amount);

            return Results.Ok(new
            {
                companyId,
                currencyCode = currency,
                from = fromDate,
                to = toDate,
                accounts,
                totalRevenue = revenue,
                totalExpenses = expenses,
                netIncome = revenue - expenses
            });
        });

        companies.MapGet("/balance-sheet", async (
            long companyId,
            DateOnly? asOfDate,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var currency = await db.Companies.AsNoTracking()
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleOrDefaultAsync(cancellationToken);
            if (currency is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

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
                where line.CompanyId == companyId &&
                      entry.Status == "Posted" &&
                      entry.EntryDate <= effectiveDate
                group line by new { account.Id, account.Code, account.Name, accountGroup.AccountType } into grouped
                orderby grouped.Key.AccountType, grouped.Key.Code
                select new
                {
                    accountId = grouped.Key.Id,
                    accountCode = grouped.Key.Code,
                    accountName = grouped.Key.Name,
                    accountType = grouped.Key.AccountType,
                    debit = grouped.Sum(line => line.FunctionalDebit),
                    credit = grouped.Sum(line => line.FunctionalCredit)
                }).ToListAsync(cancellationToken);

            var assets = balances
                .Where(account => account.accountType == "Asset")
                .Select(account => new
                {
                    account.accountId,
                    account.accountCode,
                    account.accountName,
                    account.accountType,
                    balance = account.debit - account.credit
                })
                .ToArray();
            var liabilities = balances
                .Where(account => account.accountType == "Liability")
                .Sum(account => account.credit - account.debit);
            var equity = balances
                .Where(account => account.accountType == "Equity")
                .Sum(account => account.credit - account.debit);
            var revenue = balances
                .Where(account => account.accountType == "Revenue")
                .Sum(account => account.credit - account.debit);
            var expenses = balances
                .Where(account => account.accountType == "Expense")
                .Sum(account => account.debit - account.credit);
            var currentEarnings = revenue - expenses;
            var balanceSheetAccounts = balances
                .Where(account => account.accountType is "Asset" or "Liability" or "Equity")
                .Select(account => new
                {
                    accountId = (long?)account.accountId,
                    account.accountCode,
                    account.accountName,
                    account.accountType,
                    balance = account.accountType == "Asset"
                        ? account.debit - account.credit
                        : account.credit - account.debit
                })
                .Where(account => account.balance != 0)
                .ToList();
            if (currentEarnings != 0)
            {
                balanceSheetAccounts.Add(new
                {
                    accountId = (long?)null,
                    accountCode = "CURRENT-EARNINGS",
                    accountName = "Current earnings",
                    accountType = "Equity",
                    balance = currentEarnings
                });
            }

            var totalAssets = assets.Sum(account => account.balance);
            var totalLiabilitiesAndEquity = liabilities + equity + currentEarnings;
            return Results.Ok(new
            {
                companyId,
                currencyCode = currency,
                asOfDate = effectiveDate,
                accounts = balanceSheetAccounts,
                totalAssets,
                totalLiabilities = liabilities,
                totalEquity = equity,
                currentEarnings,
                totalLiabilitiesAndEquity,
                balanceDifference = totalAssets - totalLiabilitiesAndEquity
            });
        });

        companies.MapGet("/receivables-aging", async (
            long companyId,
            DateOnly? asOfDate,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var companyCurrency = await db.Companies.AsNoTracking()
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleOrDefaultAsync(cancellationToken);
            if (companyCurrency is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

            var allocated = await (
                from allocation in db.PaymentAllocations.AsNoTracking()
                join payment in db.Payments.AsNoTracking()
                    on new { allocation.TenantId, allocation.CompanyId, allocation.PaymentId }
                    equals new { payment.TenantId, payment.CompanyId, PaymentId = payment.Id }
                where allocation.CompanyId == companyId &&
                      allocation.SalesInvoiceId.HasValue &&
                      payment.Status == "Posted" &&
                      payment.PaymentDate <= effectiveDate
                group allocation by allocation.SalesInvoiceId!.Value into grouped
                select new { InvoiceId = grouped.Key, Amount = grouped.Sum(item => item.Amount) })
                .ToDictionaryAsync(item => item.InvoiceId, item => item.Amount, cancellationToken);

            var invoices = await (
                from invoice in db.SalesInvoices.AsNoTracking()
                join party in db.Parties.AsNoTracking()
                    on invoice.CustomerId equals party.Id
                where invoice.CompanyId == companyId &&
                      invoice.Status == "Posted" &&
                      invoice.InvoiceDate <= effectiveDate
                orderby invoice.DueDate, party.DisplayName, invoice.InvoiceNumber
                select new AgingDocument(
                    invoice.Id,
                    invoice.InvoiceNumber,
                    party.PartyNo,
                    party.DisplayName,
                    invoice.InvoiceDate,
                    invoice.DueDate,
                    invoice.CurrencyCode,
                    invoice.GrandTotal))
                .ToListAsync(cancellationToken);

            return Results.Ok(CreateAgingReport(companyId, companyCurrency, effectiveDate, invoices, allocated));
        })
        .WithName("GetReceivablesAging");

        companies.MapGet("/payables-aging", async (
            long companyId,
            DateOnly? asOfDate,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var effectiveDate = asOfDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var companyCurrency = await db.Companies.AsNoTracking()
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleOrDefaultAsync(cancellationToken);
            if (companyCurrency is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

            var allocated = await (
                from allocation in db.PaymentAllocations.AsNoTracking()
                join payment in db.Payments.AsNoTracking()
                    on new { allocation.TenantId, allocation.CompanyId, allocation.PaymentId }
                    equals new { payment.TenantId, payment.CompanyId, PaymentId = payment.Id }
                where allocation.CompanyId == companyId &&
                      allocation.PurchaseBillId.HasValue &&
                      payment.Status == "Posted" &&
                      payment.PaymentDate <= effectiveDate
                group allocation by allocation.PurchaseBillId!.Value into grouped
                select new { BillId = grouped.Key, Amount = grouped.Sum(item => item.Amount) })
                .ToDictionaryAsync(item => item.BillId, item => item.Amount, cancellationToken);

            var bills = await (
                from bill in db.PurchaseBills.AsNoTracking()
                join party in db.Parties.AsNoTracking()
                    on bill.SupplierId equals party.Id
                where bill.CompanyId == companyId &&
                      bill.Status == "Posted" &&
                      bill.BillDate <= effectiveDate
                orderby bill.DueDate, party.DisplayName, bill.BillNumber
                select new AgingDocument(
                    bill.Id,
                    bill.BillNumber,
                    party.PartyNo,
                    party.DisplayName,
                    bill.BillDate,
                    bill.DueDate,
                    bill.CurrencyCode,
                    bill.GrandTotal))
                .ToListAsync(cancellationToken);

            return Results.Ok(CreateAgingReport(companyId, companyCurrency, effectiveDate, bills, allocated));
        })
        .WithName("GetPayablesAging");

        return app;
    }

    private static AgingReportResponse CreateAgingReport(
        long companyId,
        string companyCurrency,
        DateOnly asOfDate,
        IReadOnlyList<AgingDocument> documents,
        IReadOnlyDictionary<long, decimal> allocated)
    {
        var rows = documents
            .Select(document =>
            {
                var outstanding = document.OriginalAmount - allocated.GetValueOrDefault(document.Id);
                var daysPastDue = Math.Max(asOfDate.DayNumber - document.DueDate.DayNumber, 0);
                return new AgingDocumentResponse(
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

        var totals = rows
            .GroupBy(document => document.CurrencyCode)
            .OrderBy(group => group.Key)
            .Select(group => new AgingCurrencyTotalResponse(
                group.Key,
                group.Where(document => document.DaysPastDue == 0).Sum(document => document.OutstandingAmount),
                group.Where(document => document.DaysPastDue is >= 1 and <= 30).Sum(document => document.OutstandingAmount),
                group.Where(document => document.DaysPastDue is >= 31 and <= 60).Sum(document => document.OutstandingAmount),
                group.Where(document => document.DaysPastDue is >= 61 and <= 90).Sum(document => document.OutstandingAmount),
                group.Where(document => document.DaysPastDue > 90).Sum(document => document.OutstandingAmount),
                group.Sum(document => document.OutstandingAmount)))
            .ToArray();

        return new AgingReportResponse(companyId, companyCurrency, asOfDate, rows, totals);
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

    private static CashFlowSectionResponse CreateDirectSection(
        string category,
        IReadOnlyDictionary<(string Category, long AccountId, string Code, string Name), (decimal Inflows, decimal Outflows)> flows)
    {
        var accounts = flows.Where(flow => flow.Key.Category == category)
            .OrderBy(flow => flow.Key.Code)
            .Select(flow => new CashFlowAccountFlowResponse(
                flow.Key.AccountId,
                flow.Key.Code,
                flow.Key.Name,
                flow.Value.Inflows,
                flow.Value.Outflows,
                flow.Value.Inflows - flow.Value.Outflows))
            .ToArray();
        var inflows = accounts.Sum(account => account.Inflows);
        var outflows = accounts.Sum(account => account.Outflows);
        return new CashFlowSectionResponse(category, inflows, outflows, inflows - outflows, accounts);
    }

    public sealed record CashFlowCategoryRequest(string? Category);

    private sealed record CashFlowAccountFlowResponse(
        long AccountId,
        string AccountCode,
        string AccountName,
        decimal Inflows,
        decimal Outflows,
        decimal Net);

    private sealed record CashFlowSectionResponse(
        string Category,
        decimal Inflows,
        decimal Outflows,
        decimal Net,
        IReadOnlyList<CashFlowAccountFlowResponse> Accounts);

    private sealed record AgingDocument(
        long Id,
        string DocumentNumber,
        string PartyNumber,
        string PartyName,
        DateOnly DocumentDate,
        DateOnly DueDate,
        string CurrencyCode,
        decimal OriginalAmount);

    private sealed record AgingDocumentResponse(
        long DocumentId,
        string DocumentNumber,
        string PartyNumber,
        string PartyName,
        DateOnly DocumentDate,
        DateOnly DueDate,
        string CurrencyCode,
        decimal OriginalAmount,
        decimal OutstandingAmount,
        int DaysPastDue,
        string AgingBucket);

    private sealed record AgingCurrencyTotalResponse(
        string CurrencyCode,
        decimal Current,
        decimal Days1To30,
        decimal Days31To60,
        decimal Days61To90,
        decimal Over90Days,
        decimal Total);

    private sealed record AgingReportResponse(
        long CompanyId,
        string CompanyFunctionalCurrencyCode,
        DateOnly AsOfDate,
        IReadOnlyList<AgingDocumentResponse> Documents,
        IReadOnlyList<AgingCurrencyTotalResponse> TotalsByCurrency);
}
