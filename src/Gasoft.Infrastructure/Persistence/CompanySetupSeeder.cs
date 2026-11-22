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

    private static readonly (string Code, string Name, string AccountType, string? CashFlowCategory)[] Accounts =
    [
        ("1000", "Cash and cash equivalents", "Asset", null),
        ("1010", "Petty cash", "Asset", null),
        ("1100", "Accounts receivable", "Asset", "WorkingCapitalAsset"),
        ("1200", "Inventory", "Asset", "WorkingCapitalAsset"),
        ("1300", "Recoverable tax", "Asset", "WorkingCapitalAsset"),
        ("1400", "Property, plant and equipment", "Asset", "Investing"),
        ("1450", "Furniture and equipment", "Asset", "Investing"),
        ("1490", "Accumulated depreciation", "Asset", null),
        ("1500", "Computer equipment", "Asset", "Investing"),
        ("1600", "Intangible assets", "Asset", "Investing"),
        ("2000", "Accounts payable", "Liability", "WorkingCapitalLiability"),
        ("2100", "Tax payable", "Liability", "WorkingCapitalLiability"),
        ("2200", "Loans payable", "Liability", "Financing"),
        ("2300", "Accrued expenses", "Liability", "WorkingCapitalLiability"),
        ("2400", "Deferred revenue", "Liability", "WorkingCapitalLiability"),
        ("3000", "Opening balance equity", "Equity", "Financing"),
        ("3100", "Retained earnings", "Equity", "Financing"),
        ("3200", "Owner contributions", "Equity", "Financing"),
        ("4000", "Sales revenue", "Revenue", "Operating"),
        ("4100", "Service revenue", "Revenue", "Operating"),
        ("4200", "Other operating income", "Revenue", "Operating"),
        ("5000", "Cost of goods sold", "Expense", "Operating"),
        ("6000", "General expenses", "Expense", "Operating"),
        ("6100", "Inventory adjustments", "Expense", null),
        ("6200", "Depreciation expense", "Expense", "NonCashAdjustment"),
        ("6300", "Interest expense", "Expense", "Operating"),
        ("6400", "Salaries and wages", "Expense", "Operating"),
        ("6500", "Rent and occupancy", "Expense", "Operating"),
        ("6600", "Utilities", "Expense", "Operating"),
        ("6700", "Insurance", "Expense", "Operating"),
        ("6800", "Professional fees", "Expense", "Operating"),
        ("6900", "Bank charges", "Expense", "Operating"),
        ("7000", "Income tax expense", "Expense", "Operating")
    ];

    public static async Task SeedLedgerDefaultsAsync(
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

        var existingJournals = await db.Journals
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.CompanyId == company.Id)
            .ToListAsync(cancellationToken);
        var journalsToAdd = GetDefaultJournals(tenantId, company.Id)
            .Where(seed => existingJournals.All(existing => existing.Code != seed.Code))
            .ToArray();
        db.Journals.AddRange(journalsToAdd);

        await db.SaveChangesAsync(cancellationToken);

        var groupsByCode = existingGroups.Concat(groupsToAdd)
            .ToDictionary(group => group.Code, StringComparer.Ordinal);
        var groupsByType = AccountGroups.ToDictionary(
            group => group.AccountType,
            group => groupsByCode[group.Code]);
        var existingAccountCodes = await db.ChartOfAccounts
            .IgnoreQueryFilters()
            .Where(item => item.TenantId == tenantId && item.CompanyId == company.Id)
            .Select(item => item.Code)
            .ToListAsync(cancellationToken);
        var accountsToAdd = Accounts
            .Where(seed => !existingAccountCodes.Contains(seed.Code, StringComparer.Ordinal))
            .Select(account => new ChartOfAccount
            {
                TenantId = tenantId,
                CompanyId = company.Id,
                AccountGroupId = groupsByType[account.AccountType].Id,
                Code = account.Code,
                Name = account.Name,
                AllowPosting = true,
                IsActive = true,
                CashFlowCategory = account.CashFlowCategory
            })
            .ToArray();
        db.ChartOfAccounts.AddRange(accountsToAdd);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static Journal[] GetDefaultJournals(long tenantId, long companyId) =>
    [
        new()
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "GEN",
            Name = "General journal",
            JournalType = "General"
        },
        new()
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "SALES",
            Name = "Sales journal",
            JournalType = "Sales"
        },
        new()
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "PURCHASE",
            Name = "Purchase journal",
            JournalType = "Purchase"
        },
        new()
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "BANK",
            Name = "Bank journal",
            JournalType = "Bank"
        },
        new()
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "CASHREC",
            Name = "Cash receipts journal",
            JournalType = "Cash"
        },
        new()
        {
            TenantId = tenantId,
            CompanyId = companyId,
            Code = "CASHPAY",
            Name = "Cash payments journal",
            JournalType = "Cash"
        }
    ];
}
