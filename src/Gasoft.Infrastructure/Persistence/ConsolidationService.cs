using Gasoft.Application.Abstractions;
using Gasoft.Application.Consolidation;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class ConsolidationService(AppDbContext db, ICurrentTenant currentTenant) : IConsolidationService
{
    public async Task<IReadOnlyList<ConsolidationGroupResponse>> GetGroupsAsync(
        CancellationToken cancellationToken)
    {
        var groupRows = await db.ConsolidationGroups.AsNoTracking()
            .OrderBy(group => group.Name)
            .Select(group => new
            {
                group.Id,
                group.Code,
                group.Name,
                group.ReportingCurrencyCode
            })
            .ToListAsync(cancellationToken);
        var memberships = await (
            from membership in db.ConsolidationGroupCompanies.AsNoTracking()
            join company in db.Companies.AsNoTracking() on membership.CompanyId equals company.Id
            orderby company.LegalName
            select new
            {
                membership.ConsolidationGroupId,
                Company = new ConsolidationCompanyResponse(
                    company.Id,
                    company.Code,
                    company.LegalName,
                    company.FunctionalCurrencyCode)
            })
            .ToListAsync(cancellationToken);
        return groupRows.Select(group => new ConsolidationGroupResponse(
            group.Id,
            group.Code,
            group.Name,
            group.ReportingCurrencyCode,
            memberships.Where(member => member.ConsolidationGroupId == group.Id)
                .Select(member => member.Company)
                .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<ConsolidationCompanyResponse>> GetCompaniesAsync(
        CancellationToken cancellationToken) =>
        await db.Companies.AsNoTracking()
            .OrderBy(company => company.LegalName)
            .Select(company => new ConsolidationCompanyResponse(
                company.Id,
                company.Code,
                company.LegalName,
                company.FunctionalCurrencyCode))
            .ToListAsync(cancellationToken);

    public async Task<ConsolidationGroupResponse?> CreateGroupAsync(
        ConsolidationGroupRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue) return null;
        var validation = await ValidateGroupRequestAsync(request, cancellationToken);
        if (validation.Errors.Count != 0) throw new ConsolidationValidationException(validation.Errors);
        var group = new ConsolidationGroup
        {
            TenantId = tenantId.Value,
            Code = validation.Code,
            Name = validation.Name,
            ReportingCurrencyCode = validation.CurrencyCode
        };
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.ConsolidationGroups.Add(group);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            foreach (var companyId in validation.CompanyIds)
            {
                db.ConsolidationGroupCompanies.Add(new ConsolidationGroupCompany
                {
                    TenantId = tenantId.Value,
                    ConsolidationGroupId = group.Id,
                    CompanyId = companyId
                });
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ConsolidationConflictException(
                $"Consolidation group code '{validation.Code}' is already in use.");
        }
        return ToGroupResponse(group, validation.Companies);
    }

    public async Task<ConsolidationGroupResponse?> UpdateGroupAsync(
        long groupId,
        ConsolidationGroupRequest request,
        CancellationToken cancellationToken)
    {
        var group = await db.ConsolidationGroups.SingleOrDefaultAsync(
            item => item.Id == groupId, cancellationToken);
        if (group is null) return null;
        if (await db.ConsolidationRuns.AnyAsync(
                run => run.ConsolidationGroupId == groupId, cancellationToken))
            throw new ConsolidationConflictException("A group with consolidation runs cannot be changed.");
        var validation = await ValidateGroupRequestAsync(request, cancellationToken, groupId);
        if (validation.Errors.Count != 0) throw new ConsolidationValidationException(validation.Errors);

        group.Code = validation.Code;
        group.Name = validation.Name;
        group.ReportingCurrencyCode = validation.CurrencyCode;
        var existing = await db.ConsolidationGroupCompanies
            .Where(member => member.ConsolidationGroupId == groupId)
            .ToListAsync(cancellationToken);
        var requestedIds = validation.CompanyIds.ToHashSet();
        var existingIds = existing.Select(member => member.CompanyId).ToHashSet();
        db.ConsolidationGroupCompanies.RemoveRange(
            existing.Where(member => !requestedIds.Contains(member.CompanyId)));
        foreach (var companyId in validation.CompanyIds)
        {
            if (existingIds.Contains(companyId)) continue;
            db.ConsolidationGroupCompanies.Add(new ConsolidationGroupCompany
            {
                TenantId = group.TenantId,
                ConsolidationGroupId = group.Id,
                CompanyId = companyId
            });
        }
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ConsolidationConflictException(
                $"Consolidation group code '{validation.Code}' is already in use.");
        }
        return ToGroupResponse(group, validation.Companies);
    }

    public async Task<IReadOnlyList<ConsolidationRunSummaryDto>?> GetRunsAsync(
        long groupId,
        CancellationToken cancellationToken)
    {
        if (!await db.ConsolidationGroups.AnyAsync(group => group.Id == groupId, cancellationToken))
            return null;
        return await db.ConsolidationRuns.AsNoTracking()
            .Where(run => run.ConsolidationGroupId == groupId)
            .OrderByDescending(run => run.PeriodEnd)
            .ThenByDescending(run => run.Id)
            .Select(run => new ConsolidationRunSummaryDto(run.Id, run.PeriodEnd, run.Status))
            .ToListAsync(cancellationToken);
    }

    public async Task<ConsolidationRunResponse?> CreateRunAsync(
        long groupId,
        CreateConsolidationRunRequest request,
        CancellationToken cancellationToken)
    {
        var group = await db.ConsolidationGroups.SingleOrDefaultAsync(
            item => item.Id == groupId, cancellationToken);
        if (group is null) return null;
        if (await db.ConsolidationRuns.AnyAsync(
                run => run.ConsolidationGroupId == groupId && run.PeriodEnd == request.PeriodEnd,
                cancellationToken))
            throw new ConsolidationConflictException("A consolidation run already exists for this period end.");
        if (request.PeriodEnd == default || request.PeriodStart == default || request.PeriodStart > request.PeriodEnd)
            throw Validation("period", "Choose a valid period start and end date; the start must be on or before the end.");

        var companyIds = await db.ConsolidationGroupCompanies.AsNoTracking()
            .Where(member => member.ConsolidationGroupId == groupId)
            .Select(member => member.CompanyId)
            .ToArrayAsync(cancellationToken);
        if (companyIds.Length < 2)
            throw new ConsolidationConflictException(
                "Add at least two companies to the consolidation group before running it.");
        var companies = await db.Companies.AsNoTracking()
            .Where(company => companyIds.Contains(company.Id))
            .ToDictionaryAsync(company => company.Id, cancellationToken);
        if (companies.Count != companyIds.Length)
            throw new ConsolidationConflictException(
                "A consolidation group contains a company that is no longer available.");

        var rateInputs = request.TranslationRates ?? [];
        if (rateInputs.Select(rate => rate.CompanyId).Distinct().Count() != rateInputs.Count ||
            rateInputs.Any(rate => !companyIds.Contains(rate.CompanyId)) ||
            companyIds.Any(id => !rateInputs.Any(rate => rate.CompanyId == id)))
            throw Validation(
                "translationRates",
                "Provide closing and average translation rates for every company in this group.");
        var ratesByCompany = rateInputs.ToDictionary(rate => rate.CompanyId);
        foreach (var companyId in companyIds)
        {
            var company = companies[companyId];
            var rates = ratesByCompany[companyId];
            if (rates.ClosingRate <= 0 || rates.AverageRate <= 0 ||
                rates.ClosingRate > 9_999_999_999.9999999999m ||
                rates.AverageRate > 9_999_999_999.9999999999m ||
                company.FunctionalCurrencyCode == group.ReportingCurrencyCode &&
                (rates.ClosingRate != 1 || rates.AverageRate != 1))
                throw Validation(
                    "translationRates",
                    $"Provide valid positive rates for {company.Code}; rates for the reporting currency must be 1.");
        }

        var rawBalances = await (
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
            where companyIds.Contains(line.CompanyId) && entry.Status == "Posted" &&
                  entry.EntryDate <= request.PeriodEnd
            group new { line, entry } by new
            {
                line.CompanyId,
                account.Code,
                account.Name,
                accountGroup.AccountType
            } into grouped
            select new
            {
                grouped.Key.CompanyId,
                grouped.Key.Code,
                grouped.Key.Name,
                grouped.Key.AccountType,
                Debit = grouped.Sum(item => item.line.FunctionalDebit),
                Credit = grouped.Sum(item => item.line.FunctionalCredit),
                PeriodDebit = grouped.Sum(item => item.entry.EntryDate >= request.PeriodStart
                    ? item.line.FunctionalDebit
                    : 0),
                PeriodCredit = grouped.Sum(item => item.entry.EntryDate >= request.PeriodStart
                    ? item.line.FunctionalCredit
                    : 0)
            }).ToListAsync(cancellationToken);
        var currencyPlaces = await db.Currencies
            .Where(currency => currency.Code == group.ReportingCurrencyCode)
            .Select(currency => (int)currency.DecimalPlaces)
            .SingleAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var run = new ConsolidationRun
        {
            TenantId = group.TenantId,
            ConsolidationGroupId = groupId,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Status = "Draft"
        };
        try
        {
            db.ConsolidationRuns.Add(run);
            await db.SaveChangesAsync(cancellationToken);
            var translationRates = companyIds.Select(companyId =>
            {
                var company = companies[companyId];
                var input = ratesByCompany[companyId];
                return new ConsolidationTranslationRate
                {
                    TenantId = group.TenantId,
                    ConsolidationRunId = run.Id,
                    CompanyId = companyId,
                    SourceCurrencyCode = company.FunctionalCurrencyCode,
                    TargetCurrencyCode = group.ReportingCurrencyCode,
                    ClosingRate = input.ClosingRate,
                    AverageRate = input.AverageRate
                };
            }).ToArray();
            db.ConsolidationTranslationRates.AddRange(translationRates);
            await db.SaveChangesAsync(cancellationToken);
            var lines = rawBalances
                .Select(balance => new
                {
                    balance.CompanyId,
                    balance.Code,
                    balance.Name,
                    balance.AccountType,
                    Rate = balance.AccountType is "Revenue" or "Expense"
                        ? ratesByCompany[balance.CompanyId].AverageRate
                        : ratesByCompany[balance.CompanyId].ClosingRate,
                    Net = balance.AccountType is "Revenue" or "Expense"
                        ? balance.PeriodDebit - balance.PeriodCredit
                        : balance.Debit - balance.Credit
                })
                .Where(balance => balance.Net != 0)
                .Select(balance =>
                {
                    var translated = decimal.Round(
                        balance.Net * balance.Rate,
                        currencyPlaces,
                        MidpointRounding.AwayFromZero);
                    return new ConsolidationJournalLine
                    {
                        TenantId = group.TenantId,
                        ConsolidationRunId = run.Id,
                        CompanyId = balance.CompanyId,
                        AccountCode = balance.Code,
                        AccountName = balance.Name,
                        Debit = Math.Max(translated, 0),
                        Credit = Math.Max(-translated, 0),
                        ExchangeRate = balance.Rate
                    };
                })
                .ToList();
            foreach (var companyId in companyIds)
            {
                var companyNet = lines.Where(line => line.CompanyId == companyId)
                    .Sum(line => line.Debit - line.Credit);
                if (companyNet == 0) continue;
                lines.Add(new ConsolidationJournalLine
                {
                    TenantId = group.TenantId,
                    ConsolidationRunId = run.Id,
                    CompanyId = companyId,
                    AccountCode = "CTA",
                    AccountName = "Foreign currency translation adjustment",
                    Debit = Math.Max(-companyNet, 0),
                    Credit = Math.Max(companyNet, 0),
                    ExchangeRate = ratesByCompany[companyId].ClosingRate,
                    IsTranslationAdjustment = true,
                    Description = "Balancing foreign currency translation adjustment"
                });
            }
            db.ConsolidationJournalLines.AddRange(lines);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CreateRunResponse(
                run.Id,
                run.PeriodStart,
                run.PeriodEnd,
                run.Status,
                group.ReportingCurrencyCode,
                lines,
                translationRates);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new ConsolidationConflictException(
                "A consolidation run already exists for this period end.");
        }
    }

    public async Task<ConsolidationRunResponse?> GetRunAsync(
        long groupId,
        long runId,
        CancellationToken cancellationToken)
    {
        var run = await db.ConsolidationRuns.AsNoTracking()
            .Where(item => item.Id == runId && item.ConsolidationGroupId == groupId)
            .Select(item => new { item.Id, item.PeriodStart, item.PeriodEnd, item.Status })
            .SingleOrDefaultAsync(cancellationToken);
        if (run is null) return null;
        var group = await db.ConsolidationGroups.AsNoTracking()
            .Where(item => item.Id == groupId)
            .Select(item => item.ReportingCurrencyCode)
            .SingleAsync(cancellationToken);
        var lines = await db.ConsolidationJournalLines.AsNoTracking()
            .Where(line => line.ConsolidationRunId == runId)
            .ToListAsync(cancellationToken);
        var rates = await db.ConsolidationTranslationRates.AsNoTracking()
            .Where(rate => rate.ConsolidationRunId == runId)
            .OrderBy(rate => rate.CompanyId)
            .ToListAsync(cancellationToken);
        return CreateRunResponse(
            run.Id,
            run.PeriodStart,
            run.PeriodEnd,
            run.Status,
            group,
            lines,
            rates);
    }

    public async Task<bool> SaveEliminationsAsync(
        long groupId,
        long runId,
        SaveEliminationsRequest request,
        CancellationToken cancellationToken)
    {
        var run = await db.ConsolidationRuns.SingleOrDefaultAsync(
            item => item.Id == runId && item.ConsolidationGroupId == groupId, cancellationToken);
        if (run is null) return false;
        if (run.Status != "Draft")
            throw new ConsolidationConflictException("Only draft consolidation runs may be updated.");
        var inputs = request.Lines ?? [];
        if (inputs.Count > 500 || inputs.Any(line =>
                string.IsNullOrWhiteSpace(line.AccountCode) || line.AccountCode.Trim().Length > 40 ||
                string.IsNullOrWhiteSpace(line.AccountName) || line.AccountName.Trim().Length > 160 ||
                line.Description?.Length > 200 ||
                line.Debit < 0 || line.Credit < 0 ||
                (line.Debit == 0) == (line.Credit == 0) ||
                line.Debit > 99_999_999_999_999.999999m ||
                line.Credit > 99_999_999_999_999.999999m))
            throw Validation(
                "lines",
                "Enter up to 500 valid elimination lines with an account code and name and exactly one positive debit or credit per line.");
        if (inputs.Sum(line => line.Debit) != inputs.Sum(line => line.Credit))
            throw Validation(
                "lines",
                "Intercompany eliminations must balance; total debits must equal total credits.");

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, cancellationToken);
        var existing = await db.ConsolidationJournalLines
            .Where(line => line.ConsolidationRunId == runId && line.IsElimination)
            .ToListAsync(cancellationToken);
        db.ConsolidationJournalLines.RemoveRange(existing);
        db.ConsolidationJournalLines.AddRange(inputs.Select(line => new ConsolidationJournalLine
        {
            TenantId = run.TenantId,
            ConsolidationRunId = runId,
            CompanyId = null,
            AccountCode = line.AccountCode.Trim().ToUpperInvariant(),
            AccountName = line.AccountName.Trim(),
            Debit = line.Debit,
            Credit = line.Credit,
            ExchangeRate = 1,
            IsElimination = true,
            Description = string.IsNullOrWhiteSpace(line.Description) ? null : line.Description.Trim()
        }));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<FinalizedConsolidationRunDto?> FinalizeRunAsync(
        long groupId,
        long runId,
        CancellationToken cancellationToken)
    {
        var run = await db.ConsolidationRuns.SingleOrDefaultAsync(
            item => item.Id == runId && item.ConsolidationGroupId == groupId, cancellationToken);
        if (run is null) return null;
        if (run.Status != "Draft")
            throw new ConsolidationConflictException("Only draft consolidation runs may be finalized.");
        var lines = await db.ConsolidationJournalLines
            .Where(line => line.ConsolidationRunId == runId)
            .ToListAsync(cancellationToken);
        if (lines.Sum(line => line.Debit) != lines.Sum(line => line.Credit))
            throw new ConsolidationConflictException(
                "The consolidated trial balance must balance before finalization.");
        run.Status = "Finalized";
        await db.SaveChangesAsync(cancellationToken);
        return new FinalizedConsolidationRunDto(run.Id, run.Status);
    }

    private async Task<GroupValidation> ValidateGroupRequestAsync(
        ConsolidationGroupRequest request,
        CancellationToken cancellationToken,
        long? currentGroupId = null)
    {
        var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var name = request.Name?.Trim() ?? string.Empty;
        var currency = request.ReportingCurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();
        if (code.Length is < 2 or > 30 ||
            !code.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_'))
            errors["code"] = ["Use 2–30 letters, numbers, hyphens, or underscores."];
        if (name.Length is 0 or > 160)
            errors["name"] = ["Name is required and must be at most 160 characters."];
        if (currency.Length != 3 ||
            !await db.Currencies.AnyAsync(item => item.Code == currency, cancellationToken))
            errors["reportingCurrencyCode"] = ["Choose a supported three-letter currency code."];
        var ids = request.CompanyIds?.Distinct().ToArray() ?? [];
        if (ids.Length < 2) errors["companyIds"] = ["Select at least two companies."];
        if (ids.Any(id => id <= 0)) errors["companyIds"] = ["Company identifiers must be positive."];
        if (await db.ConsolidationGroups.AnyAsync(
                group => group.Code == code && (!currentGroupId.HasValue || group.Id != currentGroupId.Value),
                cancellationToken))
            errors["code"] = ["This consolidation group code is already in use."];
        var companies = ids.Length == 0
            ? []
            : await db.Companies.AsNoTracking()
                .Where(company => ids.Contains(company.Id))
                .OrderBy(company => company.LegalName)
                .ToListAsync(cancellationToken);
        if (companies.Count != ids.Length)
            errors["companyIds"] = ["Choose companies in this workspace only."];
        return new GroupValidation(code, name, currency, ids, companies, errors);
    }

    private static ConsolidationGroupResponse ToGroupResponse(
        ConsolidationGroup group,
        IEnumerable<Company> companies) =>
        new(
            group.Id,
            group.Code,
            group.Name,
            group.ReportingCurrencyCode,
            companies.Select(ToCompanyResponse).ToArray());

    private static ConsolidationCompanyResponse ToCompanyResponse(Company company) =>
        new(company.Id, company.Code, company.LegalName, company.FunctionalCurrencyCode);

    private static ConsolidationRunResponse CreateRunResponse(
        long id,
        DateOnly periodStart,
        DateOnly periodEnd,
        string status,
        string reportingCurrencyCode,
        IReadOnlyList<ConsolidationJournalLine> lines,
        IReadOnlyList<ConsolidationTranslationRate> rates) =>
        new(
            id,
            periodStart,
            periodEnd,
            status,
            reportingCurrencyCode,
            CreateConsolidatedAccounts(lines),
            rates.Select(ToTranslationRateResponse).ToArray(),
            lines.Where(line => line.IsElimination).Select(ToAdjustmentResponse).ToArray(),
            lines.Where(line => line.IsTranslationAdjustment).Select(ToAdjustmentResponse).ToArray());

    private static ConsolidatedAccountResponse[] CreateConsolidatedAccounts(
        IEnumerable<ConsolidationJournalLine> lines) =>
        lines.GroupBy(line => line.AccountCode)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var net = group.Sum(line => line.Debit - line.Credit);
                var accountNames = string.Join(" / ", group.Select(line => line.AccountName)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal));
                return new ConsolidatedAccountResponse(
                    group.Key,
                    accountNames,
                    Math.Max(net, 0),
                    Math.Max(-net, 0));
            })
            .ToArray();

    private static ConsolidationTranslationRateResponse ToTranslationRateResponse(
        ConsolidationTranslationRate rate) =>
        new(rate.CompanyId, rate.SourceCurrencyCode, rate.TargetCurrencyCode, rate.ClosingRate, rate.AverageRate);

    private static ConsolidationAdjustmentResponse ToAdjustmentResponse(ConsolidationJournalLine line) =>
        new(line.AccountCode, line.AccountName, line.Debit, line.Credit, line.Description);

    private static ConsolidationValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private sealed record GroupValidation(
        string Code,
        string Name,
        string CurrencyCode,
        long[] CompanyIds,
        IReadOnlyList<Company> Companies,
        Dictionary<string, string[]> Errors);
}
