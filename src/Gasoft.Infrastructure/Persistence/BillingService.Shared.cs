using Gasoft.Application.Abstractions;
using Gasoft.Application.Billing;
using Gasoft.Application.Common;
using Gasoft.Application.Inventory;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class BillingService
{
    private async Task<List<CalculatedLine>> BuildLinesAsync(
        long companyId,
        DateOnly documentDate,
        string requestedCurrency,
        IReadOnlyList<CreateDocumentLineRequest> requestedLines,
        bool isSales,
        CancellationToken cancellationToken)
    {
        if (requestedLines is null || requestedLines.Count is < 1 or > 1000)
        {
            throw Validation("lines", "A document must contain 1–1000 lines.");
        }

        var currency = await GetCurrencyAsync(requestedCurrency, cancellationToken);
        var accountIds = requestedLines.Select(line => line.AccountId).Distinct().ToArray();
        var requiredAccountType = isSales ? "Revenue" : "Expense";
        var accounts = await (
            from account in db.ChartOfAccounts
            join accountGroup in db.AccountGroups on account.AccountGroupId equals accountGroup.Id
            where account.CompanyId == companyId && accountIds.Contains(account.Id)
            select new { Account = account, accountGroup.AccountType })
            .ToDictionaryAsync(row => row.Account.Id, cancellationToken);
        if (accounts.Count != accountIds.Length ||
            accounts.Values.Any(row =>
                !row.Account.IsActive || !row.Account.AllowPosting || row.AccountType != requiredAccountType))
        {
            throw Validation("lines", $"Each line must use an active, postable {requiredAccountType.ToLowerInvariant()} account in this company.");
        }

        var taxCodeIds = requestedLines
            .Where(line => line.TaxCodeId.HasValue)
            .Select(line => line.TaxCodeId!.Value)
            .Distinct()
            .ToArray();
        var taxData = new Dictionary<long, decimal>();
        if (taxCodeIds.Length > 0)
        {
            var registrations = await db.TenantTaxRegistrations
                .Where(registration => registration.CompanyId == companyId)
                .Select(registration => registration.JurisdictionId)
                .ToArrayAsync(cancellationToken);
            var rates = await (
                from taxRate in db.TaxRates
                join taxCode in db.TaxCodes on taxRate.TaxCodeId equals taxCode.Id
                where taxCodeIds.Contains(taxCode.Id) &&
                      registrations.Contains(taxCode.JurisdictionId) &&
                      ((taxCode.TenantId == null && taxCode.CompanyId == null) ||
                       (taxCode.TenantId == db.CurrentTenantId && taxCode.CompanyId == companyId)) &&
                      taxRate.ValidFrom <= documentDate &&
                      (taxRate.ValidTo == null || taxRate.ValidTo >= documentDate)
                select new { taxCode.Id, taxRate.RatePercent })
                .ToListAsync(cancellationToken);

            foreach (var taxCodeId in taxCodeIds)
            {
                var matchingRates = rates.Where(rate => rate.Id == taxCodeId).ToArray();
                if (matchingRates.Length != 1)
                {
                    throw Validation("lines", $"Tax code {taxCodeId} must have exactly one rate valid on the document date and a company tax registration.");
                }

                taxData.Add(taxCodeId, matchingRates[0].RatePercent);
            }
        }

        var result = new List<CalculatedLine>(requestedLines.Count);
        for (var index = 0; index < requestedLines.Count; index++)
        {
            var line = requestedLines[index];
            var description = Normalize(line.Description);
            if (description.Length is 0 or > 500 ||
                line.Quantity <= 0 || line.Quantity > 9_999_999_999.999999m ||
                decimal.Round(line.Quantity, 6) != line.Quantity ||
                line.UnitPrice < 0 || line.UnitPrice > MaximumAmount ||
                decimal.Round(line.UnitPrice, 6) != line.UnitPrice)
            {
                throw Validation($"lines[{index}]", "Line description is required; quantity must be positive and quantity/unit price support up to 6 decimal places.");
            }

            var net = decimal.Round(line.Quantity * line.UnitPrice, currency.DecimalPlaces, MidpointRounding.AwayFromZero);
            var rate = line.TaxCodeId is null ? 0 : taxData[line.TaxCodeId.Value];
            var tax = decimal.Round(net * rate / 100m, currency.DecimalPlaces, MidpointRounding.AwayFromZero);
            var gross = net + tax;
            if (net > MaximumAmount || tax > MaximumAmount || gross > MaximumAmount)
            {
                throw Validation($"lines[{index}]", "Calculated line amount exceeds the supported range.");
            }

            result.Add(new CalculatedLine(
                line.AccountId,
                line.TaxCodeId,
                description,
                line.Quantity,
                line.UnitPrice,
                net,
                rate,
                tax,
                gross));
        }

        return result;
    }

    private async Task<Dictionary<long, long>> ValidateTaxAccountsAsync(
        long companyId,
        IEnumerable<long?> lineTaxCodes,
        IReadOnlyList<TaxAccountMapping> mappings,
        string requiredAccountType,
        CancellationToken cancellationToken)
    {
        var requiredIds = lineTaxCodes.Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToHashSet();
        var supplied = mappings?.GroupBy(mapping => mapping.TaxCodeId).ToArray() ?? [];
        if (supplied.Any(group => group.Count() > 1) ||
            !requiredIds.SetEquals(supplied.Select(group => group.Key)))
        {
            throw Validation("taxAccounts", "Provide exactly one tax account mapping for each tax code used on the document.");
        }

        var accountIds = supplied.ToDictionary(group => group.Key, group => group.Single().AccountId);
        if (accountIds.Count == 0)
        {
            return accountIds;
        }

        var accounts = await (
            from account in db.ChartOfAccounts
            join accountGroup in db.AccountGroups on account.AccountGroupId equals accountGroup.Id
            where account.CompanyId == companyId &&
                  accountIds.Values.Contains(account.Id) &&
                  account.AllowPosting && account.IsActive
            select new { account.Id, accountGroup.AccountType })
            .ToDictionaryAsync(row => row.Id, cancellationToken);
        if (accounts.Count != accountIds.Values.Distinct().Count() ||
            accounts.Values.Any(account => account.AccountType != requiredAccountType))
        {
            throw Validation("taxAccounts", $"Tax accounts must be active, postable {requiredAccountType.ToLowerInvariant()} accounts in this company.");
        }

        return accountIds;
    }

    private JournalEntry CreateDocumentJournal(
        long tenantId,
        long companyId,
        long journalId,
        long periodId,
        DateOnly entryDate,
        string description,
        string currencyCode,
        decimal exchangeRate,
        int functionalDecimalPlaces,
        decimal documentTotal,
        IEnumerable<(long AccountId, long? TaxCodeId, decimal Net, decimal Tax, bool IsSales)> sourceLines,
        long controlAccountId,
        IReadOnlyDictionary<long, long> taxAccounts)
    {
        var lines = sourceLines.ToArray();
        var generated = new List<JournalLine>();
        var isSales = lines[0].IsSales;
        var groupedNet = lines.GroupBy(line => line.AccountId)
            .Select(group => (AccountId: group.Key, Amount: group.Sum(line => line.Net)));
        foreach (var item in groupedNet.Where(item => item.Amount > 0))
        {
            generated.Add(CreateJournalLine(
                tenantId, companyId, item.AccountId, currencyCode, exchangeRate,
                debit: isSales ? 0 : item.Amount, credit: isSales ? item.Amount : 0,
                functionalDebit: isSales ? 0 : ConvertToFunctional(item.Amount, exchangeRate, functionalDecimalPlaces),
                functionalCredit: isSales ? ConvertToFunctional(item.Amount, exchangeRate, functionalDecimalPlaces) : 0,
                description: isSales ? "Invoice revenue" : "Bill expense"));
        }

        var groupedTax = lines.Where(line => line.TaxCodeId.HasValue)
            .GroupBy(line => line.TaxCodeId!.Value)
            .Select(group => (TaxCodeId: group.Key, Amount: group.Sum(line => line.Tax)));
        foreach (var item in groupedTax.Where(item => item.Amount > 0))
        {
            var accountId = taxAccounts[item.TaxCodeId];
            var functional = ConvertToFunctional(item.Amount, exchangeRate, functionalDecimalPlaces);
            generated.Add(CreateJournalLine(
                tenantId, companyId, accountId, currencyCode, exchangeRate,
                debit: isSales ? 0 : item.Amount, credit: isSales ? item.Amount : 0,
                functionalDebit: isSales ? 0 : functional,
                functionalCredit: isSales ? functional : 0,
                description: isSales ? "Output tax" : "Input tax"));
        }

        var offsetFunctional = generated.Sum(line => line.FunctionalDebit + line.FunctionalCredit);
        var controlFunctional = offsetFunctional;
        var controlLine = CreateJournalLine(
            tenantId, companyId, controlAccountId, currencyCode, exchangeRate,
            debit: isSales ? documentTotal : 0, credit: isSales ? 0 : documentTotal,
            functionalDebit: isSales ? controlFunctional : 0,
            functionalCredit: isSales ? 0 : controlFunctional,
            description: isSales ? "Customer receivable" : "Supplier payable");
        generated.Insert(0, controlLine);

        var totalDebit = generated.Sum(line => line.FunctionalDebit);
        var totalCredit = generated.Sum(line => line.FunctionalCredit);
        if (totalDebit <= 0 || totalDebit != totalCredit || totalDebit > MaximumAmount)
        {
            throw Validation("lines", "The document cannot be posted because rounded functional-currency amounts are out of range or do not balance.");
        }

        return new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = journalId,
            AccountingPeriodId = periodId,
            EntryDate = entryDate,
            Description = description,
            Status = "Posted",
            TotalDebit = totalDebit,
            TotalCredit = totalCredit,
            Lines = generated
        };
    }

    private List<JournalLine> BuildCreditNoteJournalLines(
        long tenantId,
        long companyId,
        SalesCreditNote note,
        long receivableAccountId,
        IReadOnlyDictionary<long, long> taxAccounts,
        int functionalDecimalPlaces)
    {
        var lines = new List<JournalLine>();
        foreach (var group in note.Lines.GroupBy(line => line.AccountId))
        {
            var amount = group.Sum(line => line.NetAmount);
            if (amount <= 0) continue;
            var functional = ConvertToFunctional(amount, note.SalesInvoice.ExchangeRate, functionalDecimalPlaces);
            lines.Add(CreateJournalLine(
                tenantId, companyId, group.Key, note.SalesInvoice.CurrencyCode, note.SalesInvoice.ExchangeRate,
                debit: amount, credit: 0, functionalDebit: functional, functionalCredit: 0,
                description: "Sales credit revenue"));
        }

        foreach (var group in note.Lines.Where(line => line.TaxCodeId.HasValue)
                     .GroupBy(line => line.TaxCodeId!.Value))
        {
            var amount = group.Sum(line => line.TaxAmount);
            if (amount <= 0) continue;
            var functional = ConvertToFunctional(amount, note.SalesInvoice.ExchangeRate, functionalDecimalPlaces);
            lines.Add(CreateJournalLine(
                tenantId, companyId, taxAccounts[group.Key], note.SalesInvoice.CurrencyCode,
                note.SalesInvoice.ExchangeRate, debit: amount, credit: 0,
                functionalDebit: functional, functionalCredit: 0, description: "Output tax reversal"));
        }

        var offsetFunctional = lines.Sum(line => line.FunctionalDebit);
        lines.Insert(0, CreateJournalLine(
            tenantId, companyId, receivableAccountId, note.SalesInvoice.CurrencyCode,
            note.SalesInvoice.ExchangeRate, debit: 0, credit: note.GrandTotal,
            functionalDebit: 0, functionalCredit: offsetFunctional,
            description: "Customer credit"));
        return lines;
    }

    private static JournalLine CreateJournalLine(
        long tenantId,
        long companyId,
        long accountId,
        string currencyCode,
        decimal exchangeRate,
        decimal debit,
        decimal credit,
        decimal functionalDebit,
        decimal functionalCredit,
        string description) =>
        new()
        {
            TenantId = tenantId,
            CompanyId = companyId,
            AccountId = accountId,
            TransactionCurrencyCode = currencyCode,
            Debit = debit,
            Credit = credit,
            FunctionalDebit = functionalDebit,
            FunctionalCredit = functionalCredit,
            ExchangeRate = exchangeRate,
            Description = description
        };

    private async Task<AccountingPeriod> GetPostingPeriodAsync(
        long companyId,
        long periodId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var period = await db.AccountingPeriods.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == periodId,
            cancellationToken);
        if (period is null || date < period.StartsOn || date > period.EndsOn)
        {
            throw Validation("accountingPeriodId", "Choose an accounting period in this company that contains the document date.");
        }

        var fiscalYear = await db.FiscalYears.SingleAsync(
            item => item.CompanyId == companyId && item.Id == period.FiscalYearId,
            cancellationToken);
        if (fiscalYear.Status != "Open")
        {
            throw new BillingConflictException("Documents cannot be posted in a closed fiscal year.");
        }

        if (period.Status != "Open")
        {
            throw new BillingConflictException("Documents cannot be posted to a closed accounting period.");
        }

        return period;
    }

    private async Task EnsureJournalAsync(long companyId, long journalId, CancellationToken cancellationToken)
    {
        if (!await db.Journals.AnyAsync(
                journal => journal.Id == journalId && journal.CompanyId == companyId && journal.IsActive,
                cancellationToken))
        {
            throw Validation("journalId", "Choose an active journal in this company.");
        }
    }

    private async Task EnsureAccountAsync(
        long companyId,
        long accountId,
        string requiredType,
        CancellationToken cancellationToken)
    {
        var account = await (
            from chartAccount in db.ChartOfAccounts
            join accountGroup in db.AccountGroups on chartAccount.AccountGroupId equals accountGroup.Id
            where chartAccount.Id == accountId && chartAccount.CompanyId == companyId
            select new { chartAccount.AllowPosting, chartAccount.IsActive, accountGroup.AccountType })
            .SingleOrDefaultAsync(cancellationToken);
        if (account is null || !account.AllowPosting || !account.IsActive || account.AccountType != requiredType)
        {
            throw Validation("accountId", $"Choose an active, postable {requiredType.ToLowerInvariant()} account in this company.");
        }
    }

    private async Task<Company> EnsureCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await db.Companies.SingleOrDefaultAsync(
                   company => company.Id == companyId,
                   cancellationToken)
               ?? throw new KeyNotFoundException("Company was not found in this tenant.");
    }

    private async Task<Currency> GetCurrencyAsync(string? code, CancellationToken cancellationToken)
    {
        var normalized = Normalize(code).ToUpperInvariant();
        return await db.Currencies.SingleOrDefaultAsync(
                   currency => currency.Code == normalized,
                   cancellationToken)
               ?? throw Validation("currencyCode", $"Currency '{normalized}' is not configured.");
    }

    private async Task<int> GetCurrencyDecimalPlacesAsync(string code, CancellationToken cancellationToken) =>
        await db.Currencies.Where(currency => currency.Code == code)
            .Select(currency => (int)currency.DecimalPlaces)
            .SingleAsync(cancellationToken);

    private async Task<Dictionary<long, decimal>> GetPostedReceiptTotalsAsync(
        IEnumerable<long> invoiceIds,
        CancellationToken cancellationToken)
    {
        var ids = invoiceIds.Distinct().ToArray();
        if (ids.Length == 0) return [];

        return await (
            from allocation in db.PaymentAllocations
            join payment in db.Payments
                on new { allocation.TenantId, allocation.CompanyId, Id = allocation.PaymentId }
                equals new { payment.TenantId, payment.CompanyId, Id = payment.Id }
            where allocation.SalesInvoiceId.HasValue &&
                  ids.Contains(allocation.SalesInvoiceId.Value) &&
                  payment.Direction == "Receive" &&
                  payment.Status == "Posted"
            group allocation by allocation.SalesInvoiceId!.Value into grouped
            select new { InvoiceId = grouped.Key, Amount = grouped.Sum(item => item.Amount) })
            .ToDictionaryAsync(item => item.InvoiceId, item => item.Amount, cancellationToken);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This document was changed by another request. Reload it and try again.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new BillingConflictException("A document with this number already exists in this company.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private long EnsureTenant() =>
        currentTenant.TenantId ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");

    private static decimal ConvertToFunctional(decimal amount, decimal exchangeRate, int decimalPlaces)
    {
        var functionalAmount = decimal.Round(amount * exchangeRate, decimalPlaces, MidpointRounding.AwayFromZero);
        if (functionalAmount > MaximumAmount)
        {
            throw Validation("amount", "Functional-currency amount exceeds the supported range.");
        }

        return functionalAmount;
    }

    private static void ValidateFunctionalExchangeRate(
        string transactionCurrencyCode,
        string functionalCurrencyCode,
        decimal exchangeRate)
    {
        if (transactionCurrencyCode == functionalCurrencyCode && exchangeRate != 1)
        {
            throw Validation("exchangeRate", "The functional currency exchange rate must be 1.");
        }
    }

    private static void ValidateDocumentHeader(string number, DateOnly date, DateOnly dueDate, decimal exchangeRate)
    {
        if (number.Length is 0 or > 40)
        {
            throw Validation("documentNumber", "Document number is required and must be at most 40 characters.");
        }

        if (dueDate < date)
        {
            throw Validation("dueDate", "Due date must be on or after the document date.");
        }

        var errors = new Dictionary<string, string[]>();
        ValidateExchangeRate(errors, exchangeRate);
        ThrowIfInvalid(errors);
    }

    private static void ValidateExchangeRate(IDictionary<string, string[]> errors, decimal rate)
    {
        if (rate <= 0 || rate > 9_999_999_999.9999999999m)
        {
            errors["exchangeRate"] = ["Exchange rate must be greater than zero and within the supported range."];
        }
    }

    private static void EnsureAmounts(decimal net, decimal tax, decimal gross)
    {
        if (net > MaximumAmount || tax > MaximumAmount || gross > MaximumAmount)
        {
            throw Validation("lines", "Document total exceeds the supported amount range.");
        }
    }

    private static void EnsureDraft(string status)
    {
        if (status != "Draft")
        {
            throw new BillingConflictException("Only draft documents can be changed, posted, or allocated.");
        }
    }

    private static void ThrowIfInvalid(IReadOnlyDictionary<string, string[]> errors)
    {
        if (errors.Count != 0)
        {
            throw new BillingValidationException(errors);
        }
    }

    private static BillingValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static InvoiceDto ToDto(SalesInvoice invoice) =>
        new(invoice.Id, invoice.CompanyId, invoice.CustomerId, invoice.JournalEntryId,
            invoice.InvoiceNumber, invoice.InvoiceDate, invoice.DueDate, invoice.CurrencyCode,
            invoice.ExchangeRate, invoice.NetTotal, invoice.TaxTotal, invoice.GrandTotal,
            invoice.AllocatedTotal, invoice.CreditedTotal, invoice.RefundedTotal, invoice.Status,
            invoice.Lines.OrderBy(line => line.Id).Select(line => new DocumentLineDto(
                line.Id, line.AccountId, line.TaxCodeId, line.Description, line.Quantity,
                line.UnitPrice, line.NetAmount, line.TaxRatePercent, line.TaxAmount, line.GrossAmount)).ToArray());

    private static SalesCreditNoteDto ToDto(
        SalesCreditNote note,
        string invoiceNumber,
        string currencyCode,
        decimal collectedAmount = 0)
    {
        var refundedAmount = note.Refunds
            .Where(refund => refund.Status == "Posted")
            .Sum(refund => refund.Amount);
        var refundableAmount = note.Status == "Posted" && note.ReceivableAccountId is not null
            ? SalesInvoiceBalance.RefundableAmount(
                note.GrandTotal,
                refundedAmount,
                collectedAmount,
                note.SalesInvoice.RefundedTotal)
            : 0;

        return new SalesCreditNoteDto(
            note.Id, note.CompanyId, note.SalesInvoiceId, invoiceNumber, note.CreditNoteNumber,
            note.CreditNoteDate, note.Reason, currencyCode, note.NetTotal, note.TaxTotal,
            note.GrandTotal, note.Status, refundedAmount, refundableAmount,
            note.Lines.OrderBy(line => line.Id).Select(line => new SalesCreditNoteLineDto(
                line.Id, line.SalesInvoiceLineId, line.AccountId, line.TaxCodeId, line.Description,
                line.Quantity, line.UnitPrice, line.NetAmount, line.TaxRatePercent,
                line.TaxAmount, line.GrossAmount)).ToArray());
    }

    private static BillDto ToDto(PurchaseBill bill) =>
        new(bill.Id, bill.CompanyId, bill.SupplierId, bill.JournalEntryId,
            bill.BillNumber, bill.BillDate, bill.DueDate, bill.CurrencyCode,
            bill.ExchangeRate, bill.NetTotal, bill.TaxTotal, bill.GrandTotal,
            bill.AllocatedTotal, bill.Status,
            bill.Lines.OrderBy(line => line.Id).Select(line => new DocumentLineDto(
                line.Id, line.AccountId, line.TaxCodeId, line.Description, line.Quantity,
                line.UnitPrice, line.NetAmount, line.TaxRatePercent, line.TaxAmount, line.GrossAmount)).ToArray());

    private static PaymentDto ToDto(Payment payment) =>
        new(payment.Id, payment.CompanyId, payment.PartyId, payment.JournalEntryId,
            payment.PaymentNumber, payment.Direction, payment.PaymentDate, payment.CurrencyCode,
            payment.ExchangeRate, payment.Amount, payment.AllocatedAmount, payment.Status);

    private sealed record CalculatedLine(
        long AccountId,
        long? TaxCodeId,
        string Description,
        decimal Quantity,
        decimal UnitPrice,
        decimal NetAmount,
        decimal TaxRatePercent,
        decimal TaxAmount,
        decimal GrossAmount);
}
