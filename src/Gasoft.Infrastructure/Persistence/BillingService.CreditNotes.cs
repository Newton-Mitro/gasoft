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
    public async Task<IReadOnlyList<SalesCreditNoteDto>> GetSalesCreditNotesAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var notes = await db.SalesCreditNotes.AsNoTracking()
            .Include(note => note.Lines)
            .Include(note => note.SalesInvoice)
            .Include(note => note.Refunds)
            .Where(note => note.CompanyId == companyId)
            .OrderByDescending(note => note.CreditNoteDate)
            .ThenByDescending(note => note.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
        var collectedByInvoice = await GetPostedReceiptTotalsAsync(
            notes.Select(note => note.SalesInvoiceId), cancellationToken);
        return notes.Select(note => ToDto(
            note,
            note.SalesInvoice.InvoiceNumber,
            note.SalesInvoice.CurrencyCode,
            collectedByInvoice.GetValueOrDefault(note.SalesInvoiceId))).ToArray();
    }

    public async Task<PagedResultDto<SalesCreditNoteDto>> GetSalesCreditNotesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var result = await PagedQuery.ToPageAsync(
            db.SalesCreditNotes.AsNoTracking()
                .Where(note => note.CompanyId == companyId)
                .OrderByDescending(note => note.CreditNoteDate)
                .ThenByDescending(note => note.Id)
                .Include(note => note.Lines)
                .Include(note => note.SalesInvoice)
                .Include(note => note.Refunds),
            page,
            pageSize,
            note => note,
            cancellationToken);
        var collectedByInvoice = await GetPostedReceiptTotalsAsync(
            result.Items.Select(note => note.SalesInvoiceId), cancellationToken);
        return new PagedResultDto<SalesCreditNoteDto>(
            result.Items.Select(note => ToDto(
                note,
                note.SalesInvoice.InvoiceNumber,
                note.SalesInvoice.CurrencyCode,
                collectedByInvoice.GetValueOrDefault(note.SalesInvoiceId))).ToArray(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    public async Task<SalesCreditNoteDto> CreateSalesCreditNoteAsync(
        long companyId,
        CreateSalesCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(companyId, cancellationToken);
        var invoice = await db.SalesInvoices.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == request.SalesInvoiceId,
                cancellationToken);
        if (invoice is null) throw new KeyNotFoundException("Sales invoice was not found.");
        if (invoice.Status != "Posted")
            throw new BillingConflictException("Only a posted sales invoice can be credited.");

        var number = Normalize(request.CreditNoteNumber).ToUpperInvariant();
        var reason = Normalize(request.Reason);
        if (number.Length is 0 or > 40 || request.CreditNoteDate < invoice.InvoiceDate ||
            request.CreditNoteDate > DateOnly.FromDateTime(DateTime.UtcNow) || reason.Length is 0 or > 500)
            throw Validation("creditNote", "Provide a unique credit note number, a valid date on or after the invoice date, and a reason.");
        if (request.Lines is null || request.Lines.Count is < 1 or > 1000)
            throw Validation("lines", "A credit note must contain 1–1000 lines.");
        if (await db.SalesCreditNotes.AnyAsync(
                item => item.CompanyId == companyId && item.CreditNoteNumber == number, cancellationToken))
            throw new BillingConflictException("A credit note with this number already exists.");

        var duplicateLineIds = request.Lines.GroupBy(line => line.SalesInvoiceLineId)
            .Any(group => group.Count() > 1);
        if (duplicateLineIds)
            throw Validation("lines", "Each invoice line can appear only once on a credit note.");

        var lineIds = request.Lines.Select(line => line.SalesInvoiceLineId).ToArray();
        var alreadyCredited = await (
            from creditLine in db.SalesCreditNoteLines
            join credit in db.SalesCreditNotes on new { creditLine.TenantId, creditLine.CompanyId, Id = creditLine.SalesCreditNoteId }
                equals new { credit.TenantId, credit.CompanyId, Id = credit.Id }
            where lineIds.Contains(creditLine.SalesInvoiceLineId) && credit.Status == "Posted"
            group creditLine by creditLine.SalesInvoiceLineId into grouped
            select new { InvoiceLineId = grouped.Key, Quantity = grouped.Sum(line => line.Quantity) })
            .ToDictionaryAsync(item => item.InvoiceLineId, item => item.Quantity, cancellationToken);
        var currencyDecimalPlaces = await GetCurrencyDecimalPlacesAsync(invoice.CurrencyCode, cancellationToken);
        var invoiceLines = invoice.Lines.ToDictionary(line => line.Id);
        var note = new SalesCreditNote
        {
            TenantId = tenantId,
            CompanyId = companyId,
            SalesInvoiceId = invoice.Id,
            SalesInvoice = invoice,
            CreditNoteNumber = number,
            CreditNoteDate = request.CreditNoteDate,
            Reason = reason
        };
        foreach (var (requested, index) in request.Lines.Select((line, index) => (line, index)))
        {
            if (!invoiceLines.TryGetValue(requested.SalesInvoiceLineId, out var source))
                throw Validation($"lines[{index}]", "Choose a line from the selected sales invoice.");
            var creditedQuantity = alreadyCredited.GetValueOrDefault(source.Id);
            if (requested.Quantity <= 0 || decimal.Round(requested.Quantity, 6) != requested.Quantity ||
                requested.Quantity > source.Quantity - creditedQuantity)
                throw Validation($"lines[{index}]", "Credit quantity must be positive, support up to 6 decimals, and not exceed the uncredited invoice quantity.");
            var net = decimal.Round(
                requested.Quantity * source.UnitPrice,
                currencyDecimalPlaces,
                MidpointRounding.AwayFromZero);
            var tax = decimal.Round(
                net * source.TaxRatePercent / 100m,
                currencyDecimalPlaces,
                MidpointRounding.AwayFromZero);
            var gross = net + tax;
            if (gross <= 0)
                throw Validation($"lines[{index}]", "Credit note line must have a positive amount.");
            note.Lines.Add(new SalesCreditNoteLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                SalesInvoiceLineId = source.Id,
                AccountId = source.AccountId,
                TaxCodeId = source.TaxCodeId,
                Description = source.Description,
                Quantity = requested.Quantity,
                UnitPrice = source.UnitPrice,
                NetAmount = net,
                TaxRatePercent = source.TaxRatePercent,
                TaxAmount = tax,
                GrossAmount = gross
            });
        }
        note.NetTotal = note.Lines.Sum(line => line.NetAmount);
        note.TaxTotal = note.Lines.Sum(line => line.TaxAmount);
        note.GrandTotal = note.Lines.Sum(line => line.GrossAmount);
        EnsureAmounts(note.NetTotal, note.TaxTotal, note.GrandTotal);
        db.SalesCreditNotes.Add(note);
        await SaveAsync(cancellationToken);
        return ToDto(note, invoice.InvoiceNumber, invoice.CurrencyCode);
    }

    public async Task<SalesCreditNoteDto?> PostSalesCreditNoteAsync(
        long companyId,
        long creditNoteId,
        PostSalesCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var note = await db.SalesCreditNotes.Include(item => item.Lines)
            .Include(item => item.SalesInvoice).ThenInclude(invoice => invoice.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == creditNoteId, cancellationToken);
        if (note is null) return null;
        EnsureDraft(note.Status);
        var invoice = note.SalesInvoice;
        if (invoice.Status != "Posted")
            throw new BillingConflictException("The source invoice is no longer posted.");
        var period = await GetPostingPeriodAsync(companyId, request.AccountingPeriodId, note.CreditNoteDate, cancellationToken);
        await EnsureJournalAsync(companyId, request.JournalId, cancellationToken);
        await EnsureAccountAsync(companyId, request.ReceivableAccountId, "Asset", cancellationToken);
        var taxAccounts = await ValidateTaxAccountsAsync(companyId, note.Lines.Select(line => line.TaxCodeId),
            request.TaxAccounts, "Liability", cancellationToken);

        var credited = await (
            from creditLine in db.SalesCreditNoteLines
            join credit in db.SalesCreditNotes on new { creditLine.TenantId, creditLine.CompanyId, Id = creditLine.SalesCreditNoteId }
                equals new { credit.TenantId, credit.CompanyId, Id = credit.Id }
            where credit.SalesInvoiceId == invoice.Id && credit.Status == "Posted" &&
                  credit.Id != note.Id
            group creditLine by creditLine.SalesInvoiceLineId into grouped
            select new { InvoiceLineId = grouped.Key, Quantity = grouped.Sum(line => line.Quantity) })
            .ToDictionaryAsync(item => item.InvoiceLineId, item => item.Quantity, cancellationToken);
        foreach (var line in note.Lines)
        {
            var source = invoice.Lines.Single(item => item.Id == line.SalesInvoiceLineId);
            if (line.Quantity > source.Quantity - credited.GetValueOrDefault(source.Id))
                throw new BillingConflictException("The invoice line has already been credited by another credit note.");
        }
        if (note.GrandTotal > SalesInvoiceBalance.CreditableAmount(invoice.GrandTotal, invoice.CreditedTotal))
            throw new BillingConflictException("Credit note exceeds the remaining uncredited amount on its invoice.");

        var functionalDigits = await GetCurrencyDecimalPlacesAsync(company.FunctionalCurrencyCode, cancellationToken);
        var journalLines = BuildCreditNoteJournalLines(
            tenantId, companyId, note, request.ReceivableAccountId, taxAccounts, functionalDigits);
        var debit = journalLines.Sum(line => line.FunctionalDebit);
        var creditAmount = journalLines.Sum(line => line.FunctionalCredit);
        if (debit <= 0 || debit != creditAmount || debit > MaximumAmount)
            throw Validation("lines", "The credit note cannot be posted because rounded functional amounts are out of range or do not balance.");

        var journalEntry = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = request.JournalId,
            AccountingPeriodId = period.Id,
            EntryDate = note.CreditNoteDate,
            Description = $"Sales credit note {note.CreditNoteNumber}",
            Status = "Posted",
            TotalDebit = debit,
            TotalCredit = creditAmount,
            Lines = journalLines
        };
        db.JournalEntries.Add(journalEntry);
        invoice.CreditedTotal += note.GrandTotal;
        invoice.Revision++;
        note.Status = "Posted";
        note.ReceivableAccountId = request.ReceivableAccountId;
        note.Revision++;
        Dictionary<long, decimal> collectedByInvoice;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            note.JournalEntryId = journalEntry.Id;
            await db.SaveChangesAsync(cancellationToken);
            collectedByInvoice = await GetPostedReceiptTotalsAsync([invoice.Id], cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("The invoice or credit note changed concurrently. Reload before retrying.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BillingConflictException("The credit note was posted concurrently. Reload before retrying.");
        }

        return ToDto(
            note,
            invoice.InvoiceNumber,
            invoice.CurrencyCode,
            collectedByInvoice.GetValueOrDefault(invoice.Id));
    }

    public async Task<CustomerRefundDto?> RefundSalesCreditNoteAsync(
        long companyId,
        long creditNoteId,
        CreateCustomerRefundRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var note = await db.SalesCreditNotes
            .FromSqlInterpolated($"""
                SELECT * FROM "SalesCreditNotes"
                WHERE "CompanyId" = {companyId} AND "Id" = {creditNoteId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
        if (note is null) return null;
        if (note.Status != "Posted" || note.ReceivableAccountId is null)
            throw new BillingConflictException("Only a posted credit note can be refunded.");
        if (string.IsNullOrWhiteSpace(request.RefundNumber) || request.RefundNumber.Trim().Length > 40)
            throw Validation("refundNumber", "A refund number of up to 40 characters is required.");
        if (request.Amount <= 0)
            throw Validation("amount", "Refund amount must be greater than zero.");
        if (await db.CustomerRefunds.AnyAsync(
                refund => refund.CompanyId == companyId && refund.RefundNumber == request.RefundNumber.Trim(),
                cancellationToken))
            throw new BillingConflictException("A refund with this number already exists.");

        var invoice = await db.SalesInvoices.SingleAsync(
            item => item.CompanyId == companyId && item.Id == note.SalesInvoiceId,
            cancellationToken);
        var currencyDigits = await GetCurrencyDecimalPlacesAsync(invoice.CurrencyCode, cancellationToken);
        if (decimal.Round(request.Amount, currencyDigits) != request.Amount)
            throw Validation("amount", $"Refund amount cannot have more than {currencyDigits} decimal places.");
        var alreadyRefunded = await db.CustomerRefunds
            .Where(refund => refund.CompanyId == companyId && refund.SalesCreditNoteId == note.Id && refund.Status == "Posted")
            .SumAsync(refund => (decimal?)refund.Amount, cancellationToken) ?? 0;
        var collectedByInvoice = await GetPostedReceiptTotalsAsync([invoice.Id], cancellationToken);
        var refundable = SalesInvoiceBalance.RefundableAmount(
            note.GrandTotal,
            alreadyRefunded,
            collectedByInvoice.GetValueOrDefault(invoice.Id),
            invoice.RefundedTotal);
        if (request.Amount > refundable)
            throw Validation("amount", $"Refund amount exceeds the remaining posted amount collected for this invoice: {refundable} {invoice.CurrencyCode}.");

        var period = await GetPostingPeriodAsync(companyId, request.AccountingPeriodId, request.RefundDate, cancellationToken);
        await EnsureJournalAsync(companyId, request.JournalId, cancellationToken);
        await EnsureAccountAsync(companyId, request.CashAccountId, "Asset", cancellationToken);
        if (request.CashAccountId == note.ReceivableAccountId.Value)
            throw Validation("cashAccountId", "Choose a cash account different from the credit note's receivable account.");
        await EnsureAccountAsync(companyId, note.ReceivableAccountId.Value, "Asset", cancellationToken);

        var functionalAmount = ConvertToFunctional(
            request.Amount,
            invoice.ExchangeRate,
            await GetCurrencyDecimalPlacesAsync(company.FunctionalCurrencyCode, cancellationToken));
        if (functionalAmount <= 0)
            throw Validation("amount", "Refund amount is too small to post in the company's functional currency.");

        var journalEntry = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = request.JournalId,
            AccountingPeriodId = period.Id,
            EntryDate = request.RefundDate,
            Description = $"Customer refund {request.RefundNumber.Trim()}",
            Status = "Posted",
            TotalDebit = functionalAmount,
            TotalCredit = functionalAmount,
            Lines =
            [
                new JournalLine
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    TransactionCurrencyCode = invoice.CurrencyCode,
                    AccountId = note.ReceivableAccountId.Value,
                    Debit = request.Amount,
                    FunctionalDebit = functionalAmount,
                    ExchangeRate = invoice.ExchangeRate,
                    Description = $"Credit note {note.CreditNoteNumber} refunded"
                },
                new JournalLine
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    TransactionCurrencyCode = invoice.CurrencyCode,
                    AccountId = request.CashAccountId,
                    Credit = request.Amount,
                    FunctionalCredit = functionalAmount,
                    ExchangeRate = invoice.ExchangeRate,
                    Description = "Customer refund paid"
                }
            ]
        };
        var refund = new CustomerRefund
        {
            TenantId = tenantId,
            CompanyId = companyId,
            SalesCreditNoteId = note.Id,
            JournalEntry = journalEntry,
            RefundNumber = request.RefundNumber.Trim(),
            RefundDate = request.RefundDate,
            Amount = request.Amount,
            FunctionalAmount = functionalAmount,
            Status = "Posted"
        };
        db.CustomerRefunds.Add(refund);
        invoice.RefundedTotal += request.Amount;
        invoice.Revision++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This credit note was refunded concurrently. Reload it and retry.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BillingConflictException("A refund with this number was posted concurrently.");
        }

        return new CustomerRefundDto(
            refund.Id, refund.CompanyId, refund.SalesCreditNoteId, journalEntry.Id,
            refund.RefundNumber, refund.RefundDate, refund.Amount, refund.FunctionalAmount, refund.Status);
    }
}
