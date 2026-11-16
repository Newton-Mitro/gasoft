using Gasoft.Application.Abstractions;
using Gasoft.Application.Budgets;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class BudgetService(AppDbContext db, ICurrentTenant currentTenant) : IBudgetService
{
    public async Task<BudgetWorkspaceDto?> GetWorkspaceAsync(
        long companyId,
        long? fiscalYearId,
        CancellationToken cancellationToken)
    {
        if (!await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken)) return null;

        var fiscalYears = await db.FiscalYears.AsNoTracking()
            .Where(year => year.CompanyId == companyId)
            .OrderByDescending(year => year.StartsOn)
            .Select(year => new BudgetFiscalYearDto(year.Id, year.StartsOn, year.EndsOn, year.Status))
            .ToListAsync(cancellationToken);
        if (fiscalYearId.HasValue && fiscalYears.All(year => year.Id != fiscalYearId.Value))
            throw new KeyNotFoundException("Fiscal year was not found for this company.");

        var selectedYearId = fiscalYearId ?? fiscalYears.FirstOrDefault()?.Id;
        var budgets = selectedYearId.HasValue
            ? await db.Budgets.AsNoTracking()
                .Where(budget => budget.CompanyId == companyId && budget.FiscalYearId == selectedYearId.Value)
                .OrderBy(budget => budget.Name)
                .Select(budget => new BudgetDto(budget.Id, budget.Name, budget.FiscalYearId))
                .ToListAsync(cancellationToken)
            : [];
        var periods = selectedYearId.HasValue
            ? await db.AccountingPeriods.AsNoTracking()
                .Where(period => period.CompanyId == companyId && period.FiscalYearId == selectedYearId.Value)
                .OrderBy(period => period.StartsOn)
                .Select(period => new BudgetPeriodDto(period.Id, period.StartsOn, period.EndsOn, period.Status))
                .ToListAsync(cancellationToken)
            : [];
        var accounts = await (
            from account in db.ChartOfAccounts.AsNoTracking()
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == companyId && account.IsActive && account.AllowPosting &&
                  (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense")
            orderby account.Code
            select new BudgetAccountDto(account.Id, account.Code, account.Name, accountGroup.AccountType))
            .ToListAsync(cancellationToken);

        return new BudgetWorkspaceDto(fiscalYears, budgets, periods, accounts);
    }

    public async Task<BudgetDto?> CreateAsync(
        long companyId,
        CreateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue ||
            !await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
            return null;
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
            throw Validation("name", "Enter a budget name of up to 120 characters.");
        if (!await db.FiscalYears.AnyAsync(
                year => year.Id == request.FiscalYearId && year.CompanyId == companyId,
                cancellationToken))
            throw Validation("fiscalYearId", "Choose a fiscal year belonging to this company.");

        var budget = new Budget
        {
            TenantId = tenantId.Value,
            CompanyId = companyId,
            FiscalYearId = request.FiscalYearId,
            Name = name
        };
        db.Budgets.Add(budget);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BudgetConflictException(
                "A budget with this name already exists for the selected fiscal year.");
        }
        return new BudgetDto(budget.Id, budget.Name, budget.FiscalYearId);
    }

    public async Task SaveLinesAsync(
        long companyId,
        long budgetId,
        IReadOnlyList<BudgetLineRequest>? requestedLines,
        CancellationToken cancellationToken)
    {
        var budget = await db.Budgets.SingleOrDefaultAsync(
            item => item.Id == budgetId && item.CompanyId == companyId,
            cancellationToken);
        if (budget is null) throw new KeyNotFoundException("Budget was not found for this company.");

        var lines = requestedLines ?? [];
        if (lines.Any(line => line.Amount < 0) ||
            lines.Select(line => (line.AccountId, line.AccountingPeriodId)).Distinct().Count() != lines.Count)
            throw Validation(
                "lines",
                "Budget amounts must be non-negative and each account-period pair may appear only once.");

        var periodIds = await db.AccountingPeriods.AsNoTracking()
            .Where(period => period.CompanyId == companyId && period.FiscalYearId == budget.FiscalYearId)
            .Select(period => period.Id)
            .ToArrayAsync(cancellationToken);
        var accountIds = await (
            from account in db.ChartOfAccounts.AsNoTracking()
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == companyId && account.IsActive && account.AllowPosting &&
                  (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense")
            select account.Id)
            .ToArrayAsync(cancellationToken);
        var validPeriods = periodIds.ToHashSet();
        var validAccounts = accountIds.ToHashSet();
        if (lines.Any(line =>
                !validPeriods.Contains(line.AccountingPeriodId) || !validAccounts.Contains(line.AccountId)))
            throw Validation(
                "lines",
                "Every budget line must reference an active revenue or expense account and a period in this budget’s fiscal year.");

        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue) throw new KeyNotFoundException("The current workspace was not found.");
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var existing = await db.BudgetLines
            .Where(line => line.CompanyId == companyId && line.BudgetId == budgetId)
            .ToListAsync(cancellationToken);
        db.BudgetLines.RemoveRange(existing);
        db.BudgetLines.AddRange(lines.Where(line => line.Amount != 0).Select(line => new BudgetLine
        {
            TenantId = tenantId.Value,
            CompanyId = companyId,
            BudgetId = budgetId,
            AccountingPeriodId = line.AccountingPeriodId,
            AccountId = line.AccountId,
            Amount = line.Amount
        }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(long companyId, long budgetId, CancellationToken cancellationToken)
    {
        var budget = await db.Budgets.SingleOrDefaultAsync(
            item => item.Id == budgetId && item.CompanyId == companyId,
            cancellationToken);
        if (budget is null) return false;
        db.Budgets.Remove(budget);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<BudgetVsActualDto> GetBudgetVsActualAsync(
        long companyId,
        long budgetId,
        CancellationToken cancellationToken)
    {
        var budget = await db.Budgets.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == budgetId && item.CompanyId == companyId, cancellationToken);
        if (budget is null) throw new KeyNotFoundException("Budget was not found for this company.");
        var currency = await db.Companies.AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => company.FunctionalCurrencyCode)
            .SingleOrDefaultAsync(cancellationToken);
        if (currency is null) throw new KeyNotFoundException("Company was not found in this workspace.");

        var periods = await db.AccountingPeriods.AsNoTracking()
            .Where(period => period.CompanyId == companyId && period.FiscalYearId == budget.FiscalYearId)
            .OrderBy(period => period.StartsOn)
            .Select(period => new BudgetPeriodDto(period.Id, period.StartsOn, period.EndsOn, period.Status))
            .ToListAsync(cancellationToken);
        var accounts = await (
            from account in db.ChartOfAccounts.AsNoTracking()
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == companyId &&
                  (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense")
            orderby account.Code
            select new BudgetReportAccountBase(account.Id, account.Code, account.Name, accountGroup.AccountType))
            .ToListAsync(cancellationToken);
        var periodIds = periods.Select(period => period.Id).ToArray();
        var budgetLines = await db.BudgetLines.AsNoTracking()
            .Where(line => line.CompanyId == companyId && line.BudgetId == budgetId)
            .Select(line => new { line.AccountId, line.AccountingPeriodId, line.Amount })
            .ToListAsync(cancellationToken);
        var actualLines = await (
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
            where line.CompanyId == companyId && periodIds.Contains(entry.AccountingPeriodId) &&
                  entry.Status == "Posted" &&
                  (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense")
            select new
            {
                line.AccountId,
                entry.AccountingPeriodId,
                accountGroup.AccountType,
                line.FunctionalDebit,
                line.FunctionalCredit
            }).ToListAsync(cancellationToken);

        var budgetByKey = budgetLines.ToDictionary(
            line => (line.AccountId, line.AccountingPeriodId),
            line => line.Amount);
        var actualByKey = actualLines
            .GroupBy(line => (line.AccountId, line.AccountingPeriodId))
            .ToDictionary(
                group => group.Key,
                group => group.First().AccountType == "Revenue"
                    ? group.Sum(line => line.FunctionalCredit - line.FunctionalDebit)
                    : group.Sum(line => line.FunctionalDebit - line.FunctionalCredit));

        var accountRows = accounts.Select(account =>
        {
            var values = periods.Select(period =>
            {
                budgetByKey.TryGetValue((account.AccountId, period.Id), out var planned);
                actualByKey.TryGetValue((account.AccountId, period.Id), out var actual);
                var variance = actual - planned;
                return new BudgetPeriodValueDto(
                    period.Id,
                    planned,
                    actual,
                    variance,
                    account.AccountType == "Revenue" ? variance : -variance);
            }).ToArray();
            return new BudgetReportAccountDto(
                account.AccountId,
                account.AccountCode,
                account.AccountName,
                account.AccountType,
                values,
                values.Sum(value => value.Budget),
                values.Sum(value => value.Actual),
                values.Sum(value => value.FavorableVariance));
        }).ToArray();

        var revenue = accountRows.Where(account => account.AccountType == "Revenue").ToArray();
        var expenses = accountRows.Where(account => account.AccountType == "Expense").ToArray();
        var revenueBudget = revenue.Sum(account => account.Budget);
        var revenueActual = revenue.Sum(account => account.Actual);
        var expenseBudget = expenses.Sum(account => account.Budget);
        var expenseActual = expenses.Sum(account => account.Actual);
        return new BudgetVsActualDto(
            new BudgetDto(budget.Id, budget.Name, budget.FiscalYearId),
            currency,
            periods,
            accountRows,
            new BudgetSummaryDto(
                revenueBudget,
                revenueActual,
                expenseBudget,
                expenseActual,
                revenueBudget - expenseBudget,
                revenueActual - expenseActual));
    }

    private static BudgetValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private sealed record BudgetReportAccountBase(
        long AccountId,
        string AccountCode,
        string AccountName,
        string AccountType);
}
