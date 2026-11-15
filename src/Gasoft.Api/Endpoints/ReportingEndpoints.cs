using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Endpoints;

public static class ReportingEndpoints
{
    public static IEndpointRouteBuilder MapReportingEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/reports").RequireAuthorization();

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

        return app;
    }
}
