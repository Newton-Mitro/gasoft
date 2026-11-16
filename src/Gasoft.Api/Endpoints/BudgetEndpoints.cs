using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Api.Endpoints;

public static class BudgetEndpoints
{
    public static IEndpointRouteBuilder MapBudgetEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/budgets").RequireAuthorization();

        companies.MapGet("", async (
            long companyId,
            long? fiscalYearId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this workspace." });

            var fiscalYears = await db.FiscalYears.AsNoTracking()
                .Where(year => year.CompanyId == companyId)
                .OrderByDescending(year => year.StartsOn)
                .Select(year => new { year.Id, year.StartsOn, year.EndsOn, year.Status })
                .ToListAsync(cancellationToken);
            if (fiscalYearId.HasValue && fiscalYears.All(year => year.Id != fiscalYearId.Value))
                return Results.NotFound(new { error = "Fiscal year was not found for this company." });

            var selectedYearId = fiscalYearId ?? fiscalYears.FirstOrDefault()?.Id;
            var budgets = selectedYearId.HasValue
                ? await db.Budgets.AsNoTracking()
                    .Where(budget => budget.CompanyId == companyId && budget.FiscalYearId == selectedYearId.Value)
                    .OrderBy(budget => budget.Name)
                    .Select(budget => new { budget.Id, budget.Name, budget.FiscalYearId })
                    .ToListAsync(cancellationToken)
                : [];
            var periods = selectedYearId.HasValue
                ? await db.AccountingPeriods.AsNoTracking()
                    .Where(period => period.CompanyId == companyId && period.FiscalYearId == selectedYearId.Value)
                    .OrderBy(period => period.StartsOn)
                    .Select(period => new { period.Id, period.StartsOn, period.EndsOn, period.Status })
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
                select new { AccountId = account.Id, account.Code, account.Name, AccountType = accountGroup.AccountType })
                .ToListAsync(cancellationToken);

            return Results.Ok(new { fiscalYears, budgets, periods, accounts });
        })
        .WithName("GetBudgets");

        companies.MapPost("", async (
            long companyId,
            CreateBudgetRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            var name = request.Name?.Trim();
            if (!tenantId.HasValue || !await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this workspace." });
            if (string.IsNullOrWhiteSpace(name) || name.Length > 120)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["name"] = ["Enter a budget name of up to 120 characters."]
                });
            if (!await db.FiscalYears.AnyAsync(
                    year => year.Id == request.FiscalYearId && year.CompanyId == companyId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["fiscalYearId"] = ["Choose a fiscal year belonging to this company."]
                });

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
                return Results.Conflict(new { error = "A budget with this name already exists for the selected fiscal year." });
            }
            return Results.Created(
                $"/api/v1/companies/{companyId}/budgets/{budget.Id}",
                new { budget.Id, budget.Name, budget.FiscalYearId });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateBudget");

        companies.MapPut("/{budgetId:long}/lines", async (
            long companyId,
            long budgetId,
            SaveBudgetLinesRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var budget = await db.Budgets.SingleOrDefaultAsync(
                item => item.Id == budgetId && item.CompanyId == companyId, cancellationToken);
            if (budget is null) return Results.NotFound(new { error = "Budget was not found for this company." });

            var lines = request.Lines ?? [];
            if (lines.Any(line => line.Amount < 0) ||
                lines.Select(line => (line.AccountId, line.AccountingPeriodId)).Distinct().Count() != lines.Count)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["lines"] = ["Budget amounts must be non-negative and each account-period pair may appear only once."]
                });

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
            if (lines.Any(line => !validPeriods.Contains(line.AccountingPeriodId) || !validAccounts.Contains(line.AccountId)))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["lines"] = ["Every budget line must reference an active revenue or expense account and a period in this budget’s fiscal year."]
                });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var existing = await db.BudgetLines
                .Where(line => line.CompanyId == companyId && line.BudgetId == budgetId)
                .ToListAsync(cancellationToken);
            db.BudgetLines.RemoveRange(existing);
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue) return Results.NotFound(new { error = "The current workspace was not found." });
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
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("SaveBudgetLines");

        companies.MapDelete("/{budgetId:long}", async (
            long companyId,
            long budgetId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var budget = await db.Budgets.SingleOrDefaultAsync(
                item => item.Id == budgetId && item.CompanyId == companyId, cancellationToken);
            if (budget is null) return Results.NotFound(new { error = "Budget was not found for this company." });
            db.Budgets.Remove(budget);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("DeleteBudget");

        var reports = app.MapGroup("/api/v1/companies/{companyId:long}/reports").RequireAuthorization();
        reports.MapGet("/budget-vs-actual", async (
            long companyId,
            long budgetId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var budget = await db.Budgets.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == budgetId && item.CompanyId == companyId, cancellationToken);
            if (budget is null) return Results.NotFound(new { error = "Budget was not found for this company." });
            var currency = await db.Companies.AsNoTracking()
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleOrDefaultAsync(cancellationToken);
            if (currency is null) return Results.NotFound(new { error = "Company was not found in this workspace." });

            var periods = await db.AccountingPeriods.AsNoTracking()
                .Where(period => period.CompanyId == companyId && period.FiscalYearId == budget.FiscalYearId)
                .OrderBy(period => period.StartsOn)
                .Select(period => new BudgetPeriodResponse(period.Id, period.StartsOn, period.EndsOn, period.Status))
                .ToListAsync(cancellationToken);
            var accounts = await (
                from account in db.ChartOfAccounts.AsNoTracking()
                join accountGroup in db.AccountGroups.AsNoTracking()
                    on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                    equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
                where account.CompanyId == companyId &&
                      (accountGroup.AccountType == "Revenue" || accountGroup.AccountType == "Expense")
                orderby account.Code
                select new BudgetAccountResponse(account.Id, account.Code, account.Name, accountGroup.AccountType))
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
                line => (line.AccountId, line.AccountingPeriodId), line => line.Amount);
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
                    return new BudgetPeriodValueResponse(
                        period.Id,
                        planned,
                        actual,
                        variance,
                        account.AccountType == "Revenue" ? variance : -variance);
                }).ToArray();
                return new BudgetReportAccountResponse(
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
            return Results.Ok(new
            {
                budget = new { budget.Id, budget.Name, budget.FiscalYearId },
                currencyCode = currency,
                periods,
                accounts = accountRows,
                summary = new
                {
                    revenueBudget,
                    revenueActual,
                    expenseBudget,
                    expenseActual,
                    netIncomeBudget = revenueBudget - expenseBudget,
                    netIncomeActual = revenueActual - expenseActual
                }
            });
        })
        .WithName("GetBudgetVsActual");

        return app;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public sealed record CreateBudgetRequest(string? Name, long FiscalYearId);
    public sealed record BudgetLineRequest(long AccountId, long AccountingPeriodId, decimal Amount);
    public sealed record SaveBudgetLinesRequest(IReadOnlyList<BudgetLineRequest>? Lines);
    private sealed record BudgetPeriodResponse(long Id, DateOnly StartsOn, DateOnly EndsOn, string Status);
    private sealed record BudgetAccountResponse(long AccountId, string AccountCode, string AccountName, string AccountType);
    private sealed record BudgetPeriodValueResponse(
        long PeriodId,
        decimal Budget,
        decimal Actual,
        decimal Variance,
        decimal FavorableVariance);
    private sealed record BudgetReportAccountResponse(
        long AccountId,
        string AccountCode,
        string AccountName,
        string AccountType,
        IReadOnlyList<BudgetPeriodValueResponse> Periods,
        decimal Budget,
        decimal Actual,
        decimal FavorableVariance);
}
