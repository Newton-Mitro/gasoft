using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Api.Endpoints;

public static class FixedAssetEndpoints
{
    private const decimal MaximumAmount = 99_999_999_999_999.999999m;

    public static IEndpointRouteBuilder MapFixedAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var assets = app.MapGroup("/api/v1/companies/{companyId:long}/fixed-assets")
            .RequireAuthorization();

        assets.MapGet("", async (
            long companyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!await db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this workspace." });
            var currencyCode = await db.Companies.AsNoTracking()
                .Where(company => company.Id == companyId)
                .Select(company => company.FunctionalCurrencyCode)
                .SingleAsync(cancellationToken);

            var records = await db.FixedAssets.AsNoTracking()
                .Where(asset => asset.CompanyId == companyId)
                .OrderBy(asset => asset.AssetNumber)
                .ToListAsync(cancellationToken);
            var ids = records.Select(asset => asset.Id).ToArray();
            var depreciation = await db.FixedAssetDepreciations.AsNoTracking()
                .Where(item => item.CompanyId == companyId && ids.Contains(item.FixedAssetId))
                .GroupBy(item => item.FixedAssetId)
                .Select(group => new { AssetId = group.Key, Amount = group.Sum(item => item.Amount) })
                .ToDictionaryAsync(item => item.AssetId, item => item.Amount, cancellationToken);
            var accountIds = records.SelectMany(asset => new[]
                {
                    asset.AssetAccountId,
                    asset.AccumulatedDepreciationAccountId,
                    asset.DepreciationExpenseAccountId
                })
                .Distinct()
                .ToArray();
            var accountNames = await db.ChartOfAccounts.AsNoTracking()
                .Where(account => account.CompanyId == companyId && accountIds.Contains(account.Id))
                .ToDictionaryAsync(account => account.Id, account => $"{account.Code} · {account.Name}", cancellationToken);

            var fiscalPeriods = await db.AccountingPeriods.AsNoTracking()
                .Where(period => period.CompanyId == companyId)
                .OrderByDescending(period => period.StartsOn)
                .Select(period => new { period.Id, period.StartsOn, period.EndsOn, period.Status })
                .ToListAsync(cancellationToken);
            var journals = await db.Journals.AsNoTracking()
                .Where(journal => journal.CompanyId == companyId && journal.IsActive)
                .OrderBy(journal => journal.Code)
                .Select(journal => new { journal.Id, journal.Code, journal.Name })
                .ToListAsync(cancellationToken);
            var accountOptions = await (
                from account in db.ChartOfAccounts.AsNoTracking()
                join accountGroup in db.AccountGroups.AsNoTracking()
                    on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                    equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
                where account.CompanyId == companyId && account.IsActive && account.AllowPosting
                orderby account.Code
                select new { AccountId = account.Id, account.Code, account.Name, AccountType = accountGroup.AccountType })
                .ToListAsync(cancellationToken);
            var depreciationHistory = await db.FixedAssetDepreciations.AsNoTracking()
                .Where(item => item.CompanyId == companyId && ids.Contains(item.FixedAssetId))
                .OrderBy(item => item.DepreciationDate)
                .Select(item => new
                {
                    assetId = item.FixedAssetId,
                    item.DepreciationDate,
                    item.Amount,
                    item.JournalEntryId
                })
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                currencyCode,
                assets = records.Select(asset => new
                {
                    asset.Id,
                    asset.AssetNumber,
                    asset.Name,
                    asset.Description,
                    asset.AcquisitionDate,
                    asset.InServiceDate,
                    asset.AcquisitionCost,
                    asset.ResidualValue,
                    asset.UsefulLifeMonths,
                    asset.AssetAccountId,
                    asset.AccumulatedDepreciationAccountId,
                    asset.DepreciationExpenseAccountId,
                    asset.AcquisitionJournalEntryId,
                    asset.Status,
                    asset.DisposalDate,
                    asset.DisposalProceeds,
                    asset.DisposalJournalEntryId,
                    accumulatedDepreciation = depreciation.GetValueOrDefault(asset.Id),
                    netBookValue = asset.AcquisitionCost - depreciation.GetValueOrDefault(asset.Id),
                    assetAccountName = accountNames.GetValueOrDefault(asset.AssetAccountId, ""),
                    accumulatedDepreciationAccountName = accountNames.GetValueOrDefault(asset.AccumulatedDepreciationAccountId, ""),
                    depreciationExpenseAccountName = accountNames.GetValueOrDefault(asset.DepreciationExpenseAccountId, "")
                }),
                periods = fiscalPeriods,
                journals,
                accounts = accountOptions,
                depreciationHistory
            });
        })
        .WithName("GetFixedAssets");

        assets.MapPost("", async (
            long companyId,
            CreateFixedAssetRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
            if (!tenantId.HasValue || company is null)
                return Results.NotFound(new { error = "Company was not found in this workspace." });

            var number = request.AssetNumber?.Trim().ToUpperInvariant();
            var name = request.Name?.Trim();
            var description = request.Description?.Trim();
            if (string.IsNullOrWhiteSpace(number) || number.Length > 40 ||
                string.IsNullOrWhiteSpace(name) || name.Length > 160 ||
                description?.Length > 500)
                return Validation("asset", "Asset number and name are required (up to 40 and 160 characters); description is limited to 500 characters.");
            if (request.AcquisitionCost <= 0 || request.AcquisitionCost > MaximumAmount ||
                request.ResidualValue < 0 || request.ResidualValue > request.AcquisitionCost ||
                request.UsefulLifeMonths is < 1 or > 1200 ||
                request.AcquisitionDate > request.InServiceDate ||
                request.InServiceDate > DateOnly.MaxValue.AddMonths(-request.UsefulLifeMonths))
                return Validation("asset", "Enter a positive cost, a residual value between zero and cost, useful life from 1 to 1200 months, and valid acquisition/service dates.");

            var period = await db.AccountingPeriods.SingleOrDefaultAsync(
                item => item.CompanyId == companyId && item.Id == request.AccountingPeriodId,
                cancellationToken);
            if (period is null || request.AcquisitionDate < period.StartsOn || request.AcquisitionDate > period.EndsOn)
                return Validation("accountingPeriodId", "Choose a period in this company that contains the acquisition date.");
            if (period.Status != "Open")
                return Results.Conflict(new { error = "An asset cannot be acquired in a closed accounting period." });
            if (!await db.Journals.AnyAsync(
                    journal => journal.Id == request.JournalId && journal.CompanyId == companyId && journal.IsActive,
                    cancellationToken))
                return Validation("journalId", "Choose an active journal in this company.");

            var places = await FunctionalCurrencyPlacesAsync(db, company.FunctionalCurrencyCode, cancellationToken);
            if (!HasScale(request.AcquisitionCost, places) || !HasScale(request.ResidualValue, places))
                return Validation("acquisitionCost", $"Amounts must use at most {places} decimal places.");

            var accountTypes = await GetAccountTypesAsync(db, companyId,
                [request.AssetAccountId, request.AccumulatedDepreciationAccountId,
                    request.DepreciationExpenseAccountId, request.CreditAccountId],
                cancellationToken);
            if (!HasType(accountTypes, request.AssetAccountId, "Asset") ||
                !HasType(accountTypes, request.AccumulatedDepreciationAccountId, "Asset") ||
                !HasType(accountTypes, request.DepreciationExpenseAccountId, "Expense") ||
                !(HasType(accountTypes, request.CreditAccountId, "Asset") ||
                  HasType(accountTypes, request.CreditAccountId, "Liability")) ||
                request.AssetAccountId == request.AccumulatedDepreciationAccountId ||
                request.CreditAccountId == request.AssetAccountId ||
                request.CreditAccountId == request.AccumulatedDepreciationAccountId)
                return Validation("accounts", "Choose distinct posting accounts: a fixed asset and accumulated depreciation asset, a depreciation expense, and an asset or liability acquisition credit account.");

            var functionalCost = decimal.Round(request.AcquisitionCost, places, MidpointRounding.AwayFromZero);
            var asset = new FixedAsset
            {
                TenantId = tenantId.Value,
                CompanyId = companyId,
                AssetNumber = number,
                Name = name,
                Description = string.IsNullOrWhiteSpace(description) ? null : description,
                AcquisitionDate = request.AcquisitionDate,
                InServiceDate = request.InServiceDate,
                AcquisitionCost = functionalCost,
                ResidualValue = request.ResidualValue,
                UsefulLifeMonths = request.UsefulLifeMonths,
                AssetAccountId = request.AssetAccountId,
                AccumulatedDepreciationAccountId = request.AccumulatedDepreciationAccountId,
                DepreciationExpenseAccountId = request.DepreciationExpenseAccountId
            };
            var journalEntry = CreateEntry(
                tenantId.Value,
                companyId,
                request.JournalId,
                period.Id,
                request.AcquisitionDate,
                $"Fixed asset acquisition {number}",
                [
                    CreateLine(tenantId.Value, companyId, request.AssetAccountId, company.FunctionalCurrencyCode,
                        functionalCost, 0, $"Acquisition of {number}"),
                    CreateLine(tenantId.Value, companyId, request.CreditAccountId, company.FunctionalCurrencyCode,
                        0, functionalCost, $"Acquisition of {number}")
                ]);
            asset.AcquisitionJournalEntryId = 0;
            try
            {
                await using var transaction = await db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable, cancellationToken);
                db.JournalEntries.Add(journalEntry);
                await db.SaveChangesAsync(cancellationToken);
                asset.AcquisitionJournalEntryId = journalEntry.Id;
                db.FixedAssets.Add(asset);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                return Results.Conflict(new { error = $"Fixed asset '{number}' already exists in this company." });
            }
            return Results.Created($"/api/v1/companies/{companyId}/fixed-assets/{asset.Id}", new { asset.Id, asset.AssetNumber });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("CreateFixedAsset");

        assets.MapPost("/{assetId:long}/depreciate", async (
            long companyId,
            long assetId,
            DepreciateFixedAssetRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            var asset = await db.FixedAssets.SingleOrDefaultAsync(
                item => item.Id == assetId && item.CompanyId == companyId, cancellationToken);
            if (asset is null) return Results.NotFound(new { error = "Fixed asset was not found." });
            if (asset.Status != "Active")
                return Results.Conflict(new { error = "Only active fixed assets can be depreciated." });
            if (!tenantId.HasValue) return Results.NotFound(new { error = "The current workspace was not found." });

            var targetPeriod = await db.AccountingPeriods.SingleOrDefaultAsync(
                item => item.Id == request.ThroughAccountingPeriodId && item.CompanyId == companyId,
                cancellationToken);
            if (targetPeriod is null)
                return Validation("throughAccountingPeriodId", "Choose an accounting period in this company.");
            if (targetPeriod.Status != "Open")
                return Results.Conflict(new { error = "The selected depreciation-through period is closed." });
            if (!await db.Journals.AnyAsync(
                    journal => journal.Id == request.JournalId && journal.CompanyId == companyId && journal.IsActive,
                    cancellationToken))
                return Validation("journalId", "Choose an active journal in this company.");

            var periods = await db.AccountingPeriods
                .Where(period => period.CompanyId == companyId &&
                    period.StartsOn <= targetPeriod.EndsOn && period.EndsOn >= asset.InServiceDate)
                .OrderBy(period => period.StartsOn)
                .ToListAsync(cancellationToken);
            var existing = await db.FixedAssetDepreciations
                .Where(item => item.CompanyId == companyId && item.FixedAssetId == assetId)
                .ToListAsync(cancellationToken);
            if (existing.Any(item => item.DepreciationDate > targetPeriod.EndsOn))
                return Results.Conflict(new { error = "Depreciation cannot be posted through a period earlier than an already-posted period." });

            var company = await db.Companies.SingleAsync(item => item.Id == companyId, cancellationToken);
            var places = await FunctionalCurrencyPlacesAsync(db, company.FunctionalCurrencyCode, cancellationToken);
            var lifeEnd = asset.InServiceDate.AddMonths(asset.UsefulLifeMonths);
            var lifeDays = lifeEnd.DayNumber - asset.InServiceDate.DayNumber;
            var depreciableCost = asset.AcquisitionCost - asset.ResidualValue;
            var accumulated = existing.Sum(item => item.Amount);
            var entries = new List<JournalEntry>();
            var depreciationRows = new List<FixedAssetDepreciation>();
            var postedAmount = 0m;

            await using var transaction = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            foreach (var period in periods)
            {
                if (period.StartsOn > targetPeriod.StartsOn || period.EndsOn > targetPeriod.EndsOn) continue;
                if (existing.Any(item => item.AccountingPeriodId == period.Id)) continue;

                var elapsedDays = DaysElapsedThrough(asset.InServiceDate, lifeEnd, period.EndsOn, lifeDays);
                var cumulativeScheduled = decimal.Round(
                    depreciableCost * elapsedDays / lifeDays,
                    places,
                    MidpointRounding.AwayFromZero);
                var amount = Math.Min(Math.Max(cumulativeScheduled - accumulated, 0), depreciableCost - accumulated);
                if (amount <= 0) continue;
                if (period.Status != "Open")
                    return Results.Conflict(new { error = $"Depreciation is due in closed period {period.StartsOn:yyyy-MM-dd} to {period.EndsOn:yyyy-MM-dd}. Reopen it before posting." });

                var entry = CreateEntry(
                    tenantId.Value,
                    companyId,
                    request.JournalId,
                    period.Id,
                    period.EndsOn,
                    $"Depreciation {asset.AssetNumber} · {period.EndsOn:yyyy-MM}",
                    [
                        CreateLine(tenantId.Value, companyId, asset.DepreciationExpenseAccountId,
                            company.FunctionalCurrencyCode, amount, 0, $"Depreciation of {asset.AssetNumber}"),
                        CreateLine(tenantId.Value, companyId, asset.AccumulatedDepreciationAccountId,
                            company.FunctionalCurrencyCode, 0, amount, $"Depreciation of {asset.AssetNumber}")
                    ]);
                entries.Add(entry);
                depreciationRows.Add(new FixedAssetDepreciation
                {
                    TenantId = tenantId.Value,
                    CompanyId = companyId,
                    FixedAssetId = assetId,
                    AccountingPeriodId = period.Id,
                    JournalEntryId = 0,
                    DepreciationDate = period.EndsOn,
                    Amount = amount
                });
                accumulated += amount;
                postedAmount += amount;
            }

            if (entries.Count == 0)
                return Results.Conflict(new { error = "No unposted depreciation is due through the selected period." });

            try
            {
                db.JournalEntries.AddRange(entries);
                await db.SaveChangesAsync(cancellationToken);
                for (var index = 0; index < entries.Count; index++)
                    depreciationRows[index].JournalEntryId = entries[index].Id;
                db.FixedAssetDepreciations.AddRange(depreciationRows);
                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                return Results.Conflict(new { error = "Depreciation was posted by another request. Reload the asset and try again." });
            }

            return Results.Ok(new
            {
                assetId,
                postedPeriods = depreciationRows.Count,
                postedAmount,
                accumulatedDepreciation = accumulated,
                netBookValue = asset.AcquisitionCost - accumulated
            });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("DepreciateFixedAsset");

        assets.MapPost("/{assetId:long}/dispose", async (
            long companyId,
            long assetId,
            DisposeFixedAssetRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            var asset = await db.FixedAssets.SingleOrDefaultAsync(
                item => item.Id == assetId && item.CompanyId == companyId, cancellationToken);
            if (asset is null) return Results.NotFound(new { error = "Fixed asset was not found." });
            if (asset.Status != "Active")
                return Results.Conflict(new { error = "Only active fixed assets can be disposed." });
            if (!tenantId.HasValue) return Results.NotFound(new { error = "The current workspace was not found." });
            if (request.Proceeds < 0 || request.Proceeds > MaximumAmount)
                return Validation("proceeds", "Disposal proceeds must be a non-negative amount within the supported range.");

            var period = await db.AccountingPeriods.SingleOrDefaultAsync(
                item => item.Id == request.AccountingPeriodId && item.CompanyId == companyId,
                cancellationToken);
            if (period is null || request.DisposalDate != period.EndsOn ||
                request.DisposalDate < asset.InServiceDate)
                return Validation("accountingPeriodId", "Choose a period containing the disposal; fixed assets are disposed at period end.");
            if (period.Status != "Open")
                return Results.Conflict(new { error = "An asset cannot be disposed in a closed accounting period." });
            if (!await db.Journals.AnyAsync(
                    journal => journal.Id == request.JournalId && journal.CompanyId == companyId && journal.IsActive,
                    cancellationToken))
                return Validation("journalId", "Choose an active journal in this company.");

            var company = await db.Companies.SingleAsync(item => item.Id == companyId, cancellationToken);
            var places = await FunctionalCurrencyPlacesAsync(db, company.FunctionalCurrencyCode, cancellationToken);
            if (!HasScale(request.Proceeds, places))
                return Validation("proceeds", $"Proceeds must use at most {places} decimal places.");
            var accountTypes = await GetAccountTypesAsync(db, companyId,
                [request.CashAccountId, request.GainAccountId, request.LossAccountId],
                cancellationToken);
            if ((request.Proceeds > 0 && !HasType(accountTypes, request.CashAccountId, "Asset")) ||
                !HasType(accountTypes, request.GainAccountId, "Revenue") ||
                !HasType(accountTypes, request.LossAccountId, "Expense") ||
                (request.Proceeds > 0 && (request.CashAccountId == asset.AssetAccountId ||
                    request.CashAccountId == asset.AccumulatedDepreciationAccountId)))
                return Validation("accounts", "Choose an asset account for proceeds, a revenue account for gains, and an expense account for losses.");
            var depreciations = await db.FixedAssetDepreciations
                .Where(item => item.CompanyId == companyId && item.FixedAssetId == assetId)
                .ToListAsync(cancellationToken);
            if (depreciations.Any(item => item.DepreciationDate > request.DisposalDate))
                return Results.Conflict(new { error = "An asset cannot be disposed before an already-posted depreciation period." });
            var accumulated = depreciations.Sum(item => item.Amount);
            var lifeEnd = asset.InServiceDate.AddMonths(asset.UsefulLifeMonths);
            var elapsedDays = DaysElapsedThrough(
                asset.InServiceDate,
                lifeEnd,
                period.EndsOn,
                lifeEnd.DayNumber - asset.InServiceDate.DayNumber);
            var placesForSchedule = places;
            var scheduledDepreciation = decimal.Round(
                (asset.AcquisitionCost - asset.ResidualValue) * elapsedDays /
                (lifeEnd.DayNumber - asset.InServiceDate.DayNumber),
                placesForSchedule,
                MidpointRounding.AwayFromZero);
            if (accumulated < scheduledDepreciation)
                return Results.Conflict(new { error = "Post depreciation through the disposal period before disposing of this asset." });

            var carryingAmount = asset.AcquisitionCost - accumulated;
            var proceeds = request.Proceeds;
            var gain = Math.Max(proceeds - carryingAmount, 0);
            var loss = Math.Max(carryingAmount - proceeds, 0);
            var lines = new List<JournalLine>();
            if (proceeds > 0)
                lines.Add(CreateLine(tenantId.Value, companyId, request.CashAccountId,
                    company.FunctionalCurrencyCode, proceeds, 0, $"Proceeds from {asset.AssetNumber} disposal"));
            if (accumulated > 0)
                lines.Add(CreateLine(tenantId.Value, companyId, asset.AccumulatedDepreciationAccountId,
                    company.FunctionalCurrencyCode, accumulated, 0, $"Remove accumulated depreciation for {asset.AssetNumber}"));
            lines.Add(CreateLine(tenantId.Value, companyId, asset.AssetAccountId,
                company.FunctionalCurrencyCode, 0, asset.AcquisitionCost, $"Remove fixed asset {asset.AssetNumber}"));
            if (gain > 0)
                lines.Add(CreateLine(tenantId.Value, companyId, request.GainAccountId,
                    company.FunctionalCurrencyCode, 0, gain, $"Gain on disposal of {asset.AssetNumber}"));
            if (loss > 0)
                lines.Add(CreateLine(tenantId.Value, companyId, request.LossAccountId,
                    company.FunctionalCurrencyCode, loss, 0, $"Loss on disposal of {asset.AssetNumber}"));
            var entry = CreateEntry(
                tenantId.Value,
                companyId,
                request.JournalId,
                period.Id,
                request.DisposalDate,
                $"Fixed asset disposal {asset.AssetNumber}",
                lines);

            await using var transaction = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, cancellationToken);
            asset.Status = "Disposed";
            asset.DisposalDate = request.DisposalDate;
            asset.DisposalProceeds = proceeds;
            db.JournalEntries.Add(entry);
            await db.SaveChangesAsync(cancellationToken);
            asset.DisposalJournalEntryId = entry.Id;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Ok(new { asset.Id, asset.Status, asset.DisposalDate, asset.DisposalProceeds, gain, loss, carryingAmount });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner", "Admin"))
        .WithName("DisposeFixedAsset");

        return app;
    }

    private static JournalEntry CreateEntry(
        long tenantId,
        long companyId,
        long journalId,
        long periodId,
        DateOnly date,
        string description,
        IReadOnlyList<JournalLine> lines)
    {
        var total = lines.Sum(line => line.FunctionalDebit);
        if (total <= 0 || total > MaximumAmount || total != lines.Sum(line => line.FunctionalCredit))
            throw new InvalidOperationException("Fixed-asset journal entry is not balanced or exceeds the supported amount.");
        return new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = journalId,
            AccountingPeriodId = periodId,
            EntryDate = date,
            Description = description,
            Status = "Posted",
            TotalDebit = total,
            TotalCredit = total,
            Lines = lines.ToList()
        };
    }

    private static JournalLine CreateLine(
        long tenantId,
        long companyId,
        long accountId,
        string currencyCode,
        decimal debit,
        decimal credit,
        string description) => new()
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
        Description = description
    };

    private static async Task<Dictionary<long, string>> GetAccountTypesAsync(
        AppDbContext db,
        long companyId,
        long[] accountIds,
        CancellationToken cancellationToken) =>
        await (
            from account in db.ChartOfAccounts.AsNoTracking()
            join accountGroup in db.AccountGroups.AsNoTracking()
                on new { account.TenantId, account.CompanyId, account.AccountGroupId }
                equals new { accountGroup.TenantId, accountGroup.CompanyId, AccountGroupId = accountGroup.Id }
            where account.CompanyId == companyId && accountIds.Contains(account.Id) &&
                  account.IsActive && account.AllowPosting
            select new { AccountId = account.Id, accountGroup.AccountType })
            .ToDictionaryAsync(item => item.AccountId, item => item.AccountType, cancellationToken);

    private static bool HasType(IReadOnlyDictionary<long, string> types, long accountId, string expected) =>
        types.TryGetValue(accountId, out var actual) && actual == expected;

    private static async Task<int> FunctionalCurrencyPlacesAsync(
        AppDbContext db,
        string currencyCode,
        CancellationToken cancellationToken) =>
        await db.Currencies.Where(currency => currency.Code == currencyCode)
            .Select(currency => (int)currency.DecimalPlaces)
            .SingleAsync(cancellationToken);

    private static bool HasScale(decimal amount, int places) =>
        decimal.Round(amount, places, MidpointRounding.AwayFromZero) == amount;

    private static int DaysElapsedThrough(DateOnly serviceDate, DateOnly lifeEnd, DateOnly periodEnd, int lifeDays) =>
        Math.Clamp(Math.Min(periodEnd.DayNumber + 1, lifeEnd.DayNumber) - serviceDate.DayNumber, 0, lifeDays);

    private static IResult Validation(string key, string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [message] });

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public sealed record CreateFixedAssetRequest(
        string? AssetNumber,
        string? Name,
        string? Description,
        DateOnly AcquisitionDate,
        DateOnly InServiceDate,
        decimal AcquisitionCost,
        decimal ResidualValue,
        int UsefulLifeMonths,
        long AccountingPeriodId,
        long JournalId,
        long AssetAccountId,
        long AccumulatedDepreciationAccountId,
        long DepreciationExpenseAccountId,
        long CreditAccountId);

    public sealed record DepreciateFixedAssetRequest(long ThroughAccountingPeriodId, long JournalId);

    public sealed record DisposeFixedAssetRequest(
        DateOnly DisposalDate,
        decimal Proceeds,
        long AccountingPeriodId,
        long JournalId,
        long CashAccountId,
        long GainAccountId,
        long LossAccountId);
}
