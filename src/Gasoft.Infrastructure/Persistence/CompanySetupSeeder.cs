using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Infrastructure.Persistence;

public static class CompanySetupSeeder
{
    private static readonly (string Code, string Name, string AccountType)[] AccountGroups =
    [
        ("ASSET", "Assets", "Asset"),
        ("LIABILITY", "Liabilities", "Liability"),
        ("EQUITY", "Equity", "Equity"),
        ("REVENUE", "Revenue", "Revenue"),
        ("EXPENSE", "Expenses", "Expense")
    ];

    private static readonly (string Code, string Name, string AccountType)[] Accounts =
    [
        ("1000", "Cash and cash equivalents", "Asset"),
        ("1100", "Accounts receivable", "Asset"),
        ("1200", "Inventory", "Asset"),
        ("1300", "Recoverable tax", "Asset"),
        ("2000", "Accounts payable", "Liability"),
        ("2100", "Tax payable", "Liability"),
        ("3000", "Opening balance equity", "Equity"),
        ("4000", "Sales revenue", "Revenue"),
        ("5000", "Cost of goods sold", "Expense"),
        ("6000", "General expenses", "Expense"),
        ("6100", "Inventory adjustments", "Expense")
    ];

    public static async Task<CompanySetupSeedResult> SeedAsync(
        AppDbContext db,
        long tenantId,
        Company company,
        CancellationToken cancellationToken)
    {
        var existingGroups = await db.AccountGroups
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.CompanyId == company.Id)
            .ToListAsync(cancellationToken);
        var groupsToAdd = AccountGroups
            .Where(seed => existingGroups.All(existing => existing.Code != seed.Code))
            .Select(group => new AccountGroup
            {
                TenantId = tenantId,
                CompanyId = company.Id,
                Code = group.Code,
                Name = group.Name,
                AccountType = group.AccountType,
                NormalBalance = group.AccountType is "Asset" or "Expense" ? "Debit" : "Credit"
            })
            .ToArray();
        db.AccountGroups.AddRange(groupsToAdd);

        var journals = await db.Journals
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.CompanyId == company.Id)
            .ToListAsync(cancellationToken);
        var journalsToAdd = GetDefaultJournals(tenantId, company.Id)
            .Where(seed => journals.All(existing => existing.Code != seed.Code))
            .ToArray();
        db.Journals.AddRange(journalsToAdd);

        var currentDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var yearStart = new DateOnly(currentDate.Year, 1, 1);
        var fiscalYear = await db.FiscalYears
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                item => item.TenantId == tenantId && item.CompanyId == company.Id && item.StartsOn == yearStart,
                cancellationToken);
        var fiscalYearCreated = fiscalYear is null;
        if (fiscalYear is null)
        {
            fiscalYear = new FiscalYear
            {
                TenantId = tenantId,
                CompanyId = company.Id,
                StartsOn = yearStart,
                EndsOn = new DateOnly(currentDate.Year, 12, 31),
                Status = "Open"
            };
            db.FiscalYears.Add(fiscalYear);
        }

        await db.SaveChangesAsync(cancellationToken);

        var groupsByCode = existingGroups.Concat(groupsToAdd)
            .ToDictionary(group => group.Code, StringComparer.Ordinal);
        var groupsByType = AccountGroups.ToDictionary(
            group => group.AccountType,
            group => groupsByCode[group.Code]);
        var accounts = await db.ChartOfAccounts
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.CompanyId == company.Id)
            .Select(item => item.Code)
            .ToListAsync(cancellationToken);
        var accountsToAdd = Accounts
            .Where(seed => !accounts.Contains(seed.Code, StringComparer.Ordinal))
            .Select(account => new ChartOfAccount
            {
                TenantId = tenantId,
                CompanyId = company.Id,
                AccountGroupId = groupsByType[account.AccountType].Id,
                Code = account.Code,
                Name = account.Name,
                AllowPosting = true,
                IsActive = true
            })
            .ToArray();
        db.ChartOfAccounts.AddRange(accountsToAdd);

        var existingPeriods = await db.AccountingPeriods
            .IgnoreQueryFilters()
            .Where(item =>
                item.TenantId == tenantId &&
                item.CompanyId == company.Id &&
                item.FiscalYearId == fiscalYear.Id)
            .ToListAsync(cancellationToken);
        var periodsToAdd = Enumerable.Range(1, 12)
            .Select(month => new DateOnly(currentDate.Year, month, 1))
            .Select(start => new AccountingPeriod
            {
                TenantId = tenantId,
                CompanyId = company.Id,
                FiscalYearId = fiscalYear.Id,
                StartsOn = start,
                EndsOn = start.AddMonths(1).AddDays(-1),
                Status = "Open"
            })
            .Where(candidate => existingPeriods.All(existing =>
                candidate.EndsOn < existing.StartsOn || candidate.StartsOn > existing.EndsOn))
            .ToArray();
        db.AccountingPeriods.AddRange(periodsToAdd);
        await db.SaveChangesAsync(cancellationToken);

        return new CompanySetupSeedResult(
            groupsToAdd.Length,
            accountsToAdd.Length,
            journalsToAdd.Length,
            fiscalYearCreated,
            periodsToAdd.Length);
    }

    private static Journal[] GetDefaultJournals(long tenantId, long companyId) =>
    [
        new Journal
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "GEN",
            Name = "General journal",
            JournalType = "General"
        },
        new Journal
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "SALES",
            Name = "Sales journal",
            JournalType = "Sales"
        },
        new Journal
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "PURCHASE",
            Name = "Purchase journal",
            JournalType = "Purchase"
        },
        new Journal
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "BANK",
            Name = "Bank journal",
            JournalType = "Bank"
        }
    ];
}

public sealed record CompanySetupSeedResult(
    int AccountGroupsAdded,
    int AccountsAdded,
    int JournalsAdded,
    bool FiscalYearAdded,
    int AccountingPeriodsAdded);
