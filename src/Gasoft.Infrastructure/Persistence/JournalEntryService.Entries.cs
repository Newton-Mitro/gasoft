using Gasoft.Application.Abstractions;
using Gasoft.Application.Accounting;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Gasoft.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class JournalEntryService
{
    public async Task<JournalEntryDto?> CreateDraftAsync(
        long companyId,
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await db.Companies.SingleOrDefaultAsync(
            item => item.Id == companyId,
            cancellationToken);
        if (company is null)
        {
            return null;
        }

        var period = await db.AccountingPeriods.SingleOrDefaultAsync(
            item => item.Id == request.AccountingPeriodId && item.CompanyId == companyId,
            cancellationToken);
        if (period is null || request.EntryDate < period.StartsOn || request.EntryDate > period.EndsOn)
        {
            throw Validation("accountingPeriodId", "Choose an accounting period in this company that contains the entry date.");
        }

        if (period.Status != "Open")
        {
            throw new AccountingConflictException("Journal entries cannot be created in a closed period.");
        }
        if (!await db.FiscalYears.AnyAsync(
                year => year.CompanyId == companyId &&
                        year.Id == period.FiscalYearId &&
                        year.Status == "Open",
                cancellationToken))
            throw new AccountingConflictException("Journal entries cannot be created in a closed fiscal year.");

        if (!await db.Journals.AnyAsync(
                journal => journal.Id == request.JournalId && journal.CompanyId == companyId && journal.IsActive,
                cancellationToken))
        {
            throw Validation("journalId", "Choose an active journal in this company.");
        }

        var description = Normalize(request.Description);
        if (description.Length is 0 or > 500)
        {
            throw Validation("description", "A description of 1–500 characters is required.");
        }

        if (request.Lines is null || request.Lines.Count is < 2 or > 1000)
        {
            throw Validation("lines", "A journal entry must contain 2–1000 lines.");
        }

        var accountIds = request.Lines.Select(line => line.AccountId).Distinct().ToArray();
        var accounts = await db.ChartOfAccounts
            .Where(account => account.CompanyId == companyId && accountIds.Contains(account.Id))
            .ToDictionaryAsync(account => account.Id, cancellationToken);
        if (accounts.Count != accountIds.Length || accounts.Values.Any(account => !account.IsActive || !account.AllowPosting))
        {
            throw Validation("lines", "Each line must use an active, postable account in this company.");
        }

        var currencyCodes = request.Lines
            .Select(line => Normalize(line.TransactionCurrencyCode).ToUpperInvariant())
            .Distinct()
            .Append(company.FunctionalCurrencyCode)
            .Distinct()
            .ToArray();
        var currencies = await db.Currencies
            .Where(currency => currencyCodes.Contains(currency.Code))
            .ToDictionaryAsync(currency => currency.Code, cancellationToken);
        if (currencies.Count != currencyCodes.Length)
        {
            throw Validation("lines", "Each line must use a supported transaction currency.");
        }

        var lines = new List<JournalLine>(request.Lines.Count);
        for (var index = 0; index < request.Lines.Count; index++)
        {
            var requestLine = request.Lines[index];
            var currencyCode = Normalize(requestLine.TransactionCurrencyCode).ToUpperInvariant();
            var currency = currencies[currencyCode];
            var lineErrors = new Dictionary<string, string[]>();
            if (requestLine.Debit < 0 || requestLine.Credit < 0 ||
                (requestLine.Debit == 0) == (requestLine.Credit == 0) ||
                requestLine.Debit > MaximumAmount || requestLine.Credit > MaximumAmount)
            {
                lineErrors["amount"] = ["Enter a positive debit or credit (not both) within the supported amount range."];
            }

            if (requestLine.ExchangeRate <= 0 || requestLine.ExchangeRate > 9_999_999_999.9999999999m)
            {
                lineErrors["exchangeRate"] = ["Exchange rate must be greater than zero and within the supported range."];
            }
            else if (currencyCode == company.FunctionalCurrencyCode && requestLine.ExchangeRate != 1)
            {
                lineErrors["exchangeRate"] = ["The functional currency exchange rate must be 1."];
            }

            if (decimal.Round(requestLine.Debit, currency.DecimalPlaces) != requestLine.Debit ||
                decimal.Round(requestLine.Credit, currency.DecimalPlaces) != requestLine.Credit)
            {
                lineErrors["amount"] = [$"{currencyCode} supports {currency.DecimalPlaces} decimal places."];
            }

            if (requestLine.Description?.Length > 500)
            {
                lineErrors["description"] = ["Line description must be at most 500 characters."];
            }

            if (lineErrors.Count != 0)
            {
                throw new AccountingValidationException(new Dictionary<string, string[]>
                {
                    [$"lines[{index}]"] = lineErrors.Values.SelectMany(messages => messages).ToArray()
                });
            }

            var functionalDebit = decimal.Round(
                requestLine.Debit * requestLine.ExchangeRate,
                currencies[company.FunctionalCurrencyCode].DecimalPlaces,
                MidpointRounding.AwayFromZero);
            var functionalCredit = decimal.Round(
                requestLine.Credit * requestLine.ExchangeRate,
                currencies[company.FunctionalCurrencyCode].DecimalPlaces,
                MidpointRounding.AwayFromZero);
            if (functionalDebit > MaximumAmount || functionalCredit > MaximumAmount)
            {
                throw Validation($"lines[{index}]", "Converted amount exceeds the supported range.");
            }

            lines.Add(new JournalLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                AccountId = requestLine.AccountId,
                TransactionCurrencyCode = currencyCode,
                Debit = requestLine.Debit,
                Credit = requestLine.Credit,
                FunctionalDebit = functionalDebit,
                FunctionalCredit = functionalCredit,
                ExchangeRate = requestLine.ExchangeRate,
                Description = string.IsNullOrWhiteSpace(requestLine.Description)
                    ? null
                    : Normalize(requestLine.Description)
            });
        }

        var entry = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = request.JournalId,
            AccountingPeriodId = period.Id,
            EntryDate = request.EntryDate,
            Description = description,
            Lines = lines
        };
        db.JournalEntries.Add(entry);
        await SaveAsync(cancellationToken);
        return ToDto(entry);
    }

    public async Task<JournalEntryDto?> UpdateDraftAsync(
        long companyId,
        long entryId,
        CreateJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
        if (company is null) return null;
        var entry = await db.JournalEntries.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == entryId, cancellationToken);
        if (entry is null) return null;
        if (entry.Status != "Draft")
            throw new AccountingConflictException("Only draft journal entries can be edited.");
        if (await db.FiscalYearClosings.AnyAsync(
                closing => closing.CompanyId == companyId && closing.JournalEntryId == entryId,
                cancellationToken))
            throw new AccountingConflictException(
                "A generated year-end closing journal cannot be edited. Reopen the periods and update the underlying entries before regenerating it.");

        var period = await db.AccountingPeriods.SingleOrDefaultAsync(
            item => item.Id == request.AccountingPeriodId && item.CompanyId == companyId,
            cancellationToken);
        if (period is null || request.EntryDate < period.StartsOn || request.EntryDate > period.EndsOn)
            throw Validation("accountingPeriodId", "Choose an accounting period in this company that contains the entry date.");
        if (period.Status != "Open")
            throw new AccountingConflictException("Journal entries cannot be edited in a closed period.");
        if (!await db.Journals.AnyAsync(
                journal => journal.Id == request.JournalId && journal.CompanyId == companyId && journal.IsActive,
                cancellationToken))
            throw Validation("journalId", "Choose an active journal in this company.");

        var description = Normalize(request.Description);
        if (description.Length is 0 or > 500)
            throw Validation("description", "A description of 1–500 characters is required.");
        if (request.Lines is null || request.Lines.Count is < 2 or > 1000)
            throw Validation("lines", "A journal entry must contain 2–1000 lines.");

        var accountIds = request.Lines.Select(line => line.AccountId).Distinct().ToArray();
        var accounts = await db.ChartOfAccounts
            .Where(account => account.CompanyId == companyId && accountIds.Contains(account.Id))
            .ToDictionaryAsync(account => account.Id, cancellationToken);
        if (accounts.Count != accountIds.Length || accounts.Values.Any(account => !account.IsActive || !account.AllowPosting))
            throw Validation("lines", "Each line must use an active, postable account in this company.");

        var currencyCodes = request.Lines
            .Select(line => Normalize(line.TransactionCurrencyCode).ToUpperInvariant())
            .Append(company.FunctionalCurrencyCode)
            .Distinct()
            .ToArray();
        var currencies = await db.Currencies
            .Where(currency => currencyCodes.Contains(currency.Code))
            .ToDictionaryAsync(currency => currency.Code, cancellationToken);
        if (currencies.Count != currencyCodes.Length)
            throw Validation("lines", "Each line must use a supported transaction currency.");

        var lines = new List<JournalLine>(request.Lines.Count);
        for (var index = 0; index < request.Lines.Count; index++)
        {
            var requestLine = request.Lines[index];
            var currencyCode = Normalize(requestLine.TransactionCurrencyCode).ToUpperInvariant();
            var currency = currencies[currencyCode];
            var lineErrors = new Dictionary<string, string[]>();
            if (requestLine.Debit < 0 || requestLine.Credit < 0 ||
                (requestLine.Debit == 0) == (requestLine.Credit == 0) ||
                requestLine.Debit > MaximumAmount || requestLine.Credit > MaximumAmount)
                lineErrors["amount"] = ["Enter a positive debit or credit (not both) within the supported amount range."];
            if (requestLine.ExchangeRate <= 0 || requestLine.ExchangeRate > 9_999_999_999.9999999999m)
                lineErrors["exchangeRate"] = ["Exchange rate must be greater than zero and within the supported range."];
            else if (currencyCode == company.FunctionalCurrencyCode && requestLine.ExchangeRate != 1)
                lineErrors["exchangeRate"] = ["The functional currency exchange rate must be 1."];
            if (decimal.Round(requestLine.Debit, currency.DecimalPlaces) != requestLine.Debit ||
                decimal.Round(requestLine.Credit, currency.DecimalPlaces) != requestLine.Credit)
                lineErrors["amount"] = [$"{currencyCode} supports {currency.DecimalPlaces} decimal places."];
            if (requestLine.Description?.Length > 500)
                lineErrors["description"] = ["Line description must be at most 500 characters."];
            if (lineErrors.Count != 0)
                throw new AccountingValidationException(new Dictionary<string, string[]>
                {
                    [$"lines[{index}]"] = lineErrors.Values.SelectMany(messages => messages).ToArray()
                });

            var functionalDebit = decimal.Round(
                requestLine.Debit * requestLine.ExchangeRate,
                currencies[company.FunctionalCurrencyCode].DecimalPlaces,
                MidpointRounding.AwayFromZero);
            var functionalCredit = decimal.Round(
                requestLine.Credit * requestLine.ExchangeRate,
                currencies[company.FunctionalCurrencyCode].DecimalPlaces,
                MidpointRounding.AwayFromZero);
            if (functionalDebit > MaximumAmount || functionalCredit > MaximumAmount)
                throw Validation($"lines[{index}]", "Converted amount exceeds the supported range.");
            lines.Add(new JournalLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                AccountId = requestLine.AccountId,
                TransactionCurrencyCode = currencyCode,
                Debit = requestLine.Debit,
                Credit = requestLine.Credit,
                FunctionalDebit = functionalDebit,
                FunctionalCredit = functionalCredit,
                ExchangeRate = requestLine.ExchangeRate,
                Description = string.IsNullOrWhiteSpace(requestLine.Description) ? null : Normalize(requestLine.Description)
            });
        }

        entry.JournalId = request.JournalId;
        entry.AccountingPeriodId = period.Id;
        entry.EntryDate = request.EntryDate;
        entry.Description = description;
        entry.TotalDebit = 0;
        entry.TotalCredit = 0;
        entry.Lines.Clear();
        foreach (var line in lines) entry.Lines.Add(line);
        entry.Revision++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AccountingConflictException("This journal entry was changed by another request. Reload it and try again.");
        }
        return ToDto(entry);
    }

    public async Task<JournalEntryDto?> GetJournalEntryAsync(
        long companyId,
        long entryId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var entry = await db.JournalEntries.AsNoTracking()
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == entryId, cancellationToken);
        if (entry is null) return null;
        var reversedByEntryId = await db.JournalEntries.AsNoTracking()
            .Where(item => item.ReversalOfEntryId == entryId)
            .Select(item => (long?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return ToDto(entry) with { ReversedByEntryId = reversedByEntryId };
    }

    public async Task<IReadOnlyList<JournalEntryDto>> GetJournalEntriesAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var entries = await db.JournalEntries.AsNoTracking()
            .Where(entry => entry.CompanyId == companyId)
            .Include(entry => entry.Lines)
            .OrderByDescending(entry => entry.EntryDate)
            .ThenByDescending(entry => entry.Id)
            .ToListAsync(cancellationToken);
        return await ToDtosWithReversalsAsync(entries, cancellationToken);
    }

    public async Task<PagedResultDto<JournalEntryDto>> GetJournalEntriesPageAsync(
        long companyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        ValidatePagination(page, pageSize);
        var entries = db.JournalEntries.AsNoTracking().Where(entry => entry.CompanyId == companyId);
        var totalCount = await entries.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var pageEntries = await entries
            .OrderByDescending(entry => entry.EntryDate)
            .ThenByDescending(entry => entry.Id)
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Include(entry => entry.Lines)
            .ToListAsync(cancellationToken);
        var items = await ToDtosWithReversalsAsync(pageEntries, cancellationToken);
        return new PagedResultDto<JournalEntryDto>(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    public async Task<JournalEntryDto?> ReverseJournalEntryAsync(
        long companyId,
        long entryId,
        ReverseJournalEntryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(tenantId, companyId, cancellationToken);
        var original = await db.JournalEntries
            .Include(entry => entry.Lines)
            .SingleOrDefaultAsync(entry => entry.CompanyId == companyId && entry.Id == entryId, cancellationToken);
        if (original is null) return null;
        if (original.Status != "Posted")
            throw new AccountingConflictException("Only posted journal entries can be reversed.");
        if (original.Description.StartsWith("Opening balances for ", StringComparison.Ordinal))
            throw new AccountingConflictException(
                "Fiscal-year opening entries must be corrected through the fiscal-year opening workflow.");
        if (await IsOperationalJournalEntryAsync(companyId, entryId, cancellationToken))
            throw new AccountingConflictException(
                "This journal entry is linked to an operational record. Correct it through that record's workflow instead.");
        if (await db.JournalEntries.AnyAsync(
                entry => entry.CompanyId == companyId && entry.ReversalOfEntryId == entryId,
                cancellationToken))
            throw new AccountingConflictException("This journal entry has already been reversed.");

        var originalDebit = original.Lines.Sum(line => line.FunctionalDebit);
        var originalCredit = original.Lines.Sum(line => line.FunctionalCredit);
        if (original.Lines.Count < 2 || originalDebit <= 0 || originalDebit != originalCredit ||
            original.TotalDebit != originalDebit || original.TotalCredit != originalCredit)
            throw new AccountingConflictException("This posted journal entry is not balanced and cannot be reversed.");

        var period = await db.AccountingPeriods
            .Where(item => item.CompanyId == companyId &&
                item.Status == "Open" &&
                item.StartsOn <= request.ReversalDate &&
                item.EndsOn >= request.ReversalDate)
            .OrderBy(item => item.StartsOn)
            .FirstOrDefaultAsync(cancellationToken);
        if (period is null || !await db.FiscalYears.AnyAsync(
                year => year.CompanyId == companyId &&
                        year.Id == period.FiscalYearId &&
                        year.Status == "Open",
                cancellationToken))
            throw new AccountingConflictException(
                "Choose a reversal date within an open accounting period and open fiscal year.");

        var reversalDescription = $"Reversal of journal entry {original.Id}: {original.Description}";
        var reversal = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = original.JournalId,
            AccountingPeriodId = period.Id,
            EntryDate = request.ReversalDate,
            Description = reversalDescription[..Math.Min(500, reversalDescription.Length)],
            Status = "Posted",
            TotalDebit = originalCredit,
            TotalCredit = originalDebit,
            Revision = 1,
            ReversalOfEntryId = original.Id,
            Lines = original.Lines.Select(line => new JournalLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                AccountId = line.AccountId,
                TransactionCurrencyCode = line.TransactionCurrencyCode,
                Debit = line.Credit,
                Credit = line.Debit,
                FunctionalDebit = line.FunctionalCredit,
                FunctionalCredit = line.FunctionalDebit,
                ExchangeRate = line.ExchangeRate,
                Description = line.Description
            }).ToList()
        };

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.JournalEntries.Add(reversal);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            if (currentTenant.UserId is long actorUserId)
            {
                db.Notifications.Add(new AppNotification
                {
                    TenantId = tenantId,
                    UserId = actorUserId,
                    Title = "Journal reversal posted",
                    Message =
                        $"Posted reversal #{reversal.Id} on {reversal.EntryDate:yyyy-MM-dd} for journal entry #{original.Id} " +
                        $"\"{original.Description}\" dated {original.EntryDate:yyyy-MM-dd}.",
                    Category = "accounting",
                    EventType = NotificationEvents.JournalReversalPosted,
                    CreatedAt = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync(cancellationToken);
            }
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "UX_JournalEntries_TenantId_CompanyId_ReversalOfEntryId"
            })
        {
            throw new AccountingConflictException("This journal entry has already been reversed.");
        }

        return ToDto(reversal);
    }

    public async Task<JournalEntryDto?> PostAsync(
        long companyId,
        long entryId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var entry = await db.JournalEntries
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == entryId, cancellationToken);
        if (entry is null)
        {
            return null;
        }

        if (entry.Status != "Draft")
        {
            throw new AccountingConflictException("Only draft journal entries can be posted.");
        }

        var period = await db.AccountingPeriods.SingleAsync(
            item => item.CompanyId == companyId && item.Id == entry.AccountingPeriodId,
            cancellationToken);
        var fiscalYear = await db.FiscalYears.SingleAsync(
            item => item.CompanyId == companyId && item.Id == period.FiscalYearId,
            cancellationToken);
        if (fiscalYear.Status != "Open")
            throw new AccountingConflictException("Journal entries cannot be posted in a closed fiscal year.");

        var isFiscalYearClosingEntry = await db.FiscalYearClosings.AnyAsync(
            closing => closing.CompanyId == companyId && closing.JournalEntryId == entryId,
            cancellationToken);
        if ((period.Status != "Open" && !isFiscalYearClosingEntry) ||
            entry.EntryDate < period.StartsOn || entry.EntryDate > period.EndsOn)
        {
            throw new AccountingConflictException("The entry's accounting period is closed or does not contain its date.");
        }

        var totalDebit = entry.Lines.Sum(line => line.FunctionalDebit);
        var totalCredit = entry.Lines.Sum(line => line.FunctionalCredit);
        if (entry.Lines.Count < 2 || totalDebit <= 0 || totalDebit != totalCredit)
        {
            throw Validation("lines", "Journal entry must have at least two lines and balance in functional currency.");
        }

        entry.TotalDebit = totalDebit;
        entry.TotalCredit = totalCredit;
        entry.Status = "Posted";
        entry.Revision++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AccountingConflictException("This journal entry was updated by another request. Reload it and try again.");
        }

        return ToDto(entry);
    }
}
