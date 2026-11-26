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
    public async Task<IReadOnlyList<InvoiceDto>> GetInvoicesAsync(long companyId, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var invoices = await db.SalesInvoices.AsNoTracking()
            .Include(invoice => invoice.Lines)
            .Where(invoice => invoice.CompanyId == companyId)
            .OrderByDescending(invoice => invoice.InvoiceDate)
            .ThenByDescending(invoice => invoice.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
        return invoices.Select(ToDto).ToArray();
    }

    public async Task<PagedResultDto<InvoiceDto>> GetInvoicesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var result = await PagedQuery.ToPageAsync(
            db.SalesInvoices.AsNoTracking()
                .Where(invoice => invoice.CompanyId == companyId)
                .OrderByDescending(invoice => invoice.InvoiceDate)
                .ThenByDescending(invoice => invoice.Id)
                .Include(invoice => invoice.Lines),
            page,
            pageSize,
            invoice => invoice,
            cancellationToken);
        return new PagedResultDto<InvoiceDto>(
            result.Items.Select(ToDto).ToArray(), result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(
        long companyId,
        CreateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        if (!await db.Parties.AnyAsync(
                party => party.Id == request.PartyId && party.IsCustomer,
                cancellationToken))
        {
            throw Validation("partyId", "Choose a customer in this tenant.");
        }

        var invoiceNumber = Normalize(request.InvoiceNumber).ToUpperInvariant();
        ValidateDocumentHeader(invoiceNumber, request.InvoiceDate, request.DueDate, request.ExchangeRate);
        var currency = await GetCurrencyAsync(request.CurrencyCode, cancellationToken);
        ValidateFunctionalExchangeRate(currency.Code, company.FunctionalCurrencyCode, request.ExchangeRate);
        var lines = await BuildLinesAsync(
            companyId, request.InvoiceDate, request.CurrencyCode, request.Lines, true, cancellationToken);
        var invoice = new SalesInvoice
        {
            TenantId = tenantId,
            CompanyId = companyId,
            CustomerId = request.PartyId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = request.InvoiceDate,
            DueDate = request.DueDate,
            CurrencyCode = currency.Code,
            ExchangeRate = request.ExchangeRate,
            NetTotal = lines.Sum(line => line.NetAmount),
            TaxTotal = lines.Sum(line => line.TaxAmount),
            GrandTotal = lines.Sum(line => line.GrossAmount),
            Lines = lines.Select(line => new SalesInvoiceLine
            {
                TenantId = tenantId,
                CompanyId = companyId,
                AccountId = line.AccountId,
                TaxCodeId = line.TaxCodeId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                NetAmount = line.NetAmount,
                TaxRatePercent = line.TaxRatePercent,
                TaxAmount = line.TaxAmount,
                GrossAmount = line.GrossAmount
            }).ToList()
        };
        EnsureAmounts(invoice.NetTotal, invoice.TaxTotal, invoice.GrandTotal);
        db.SalesInvoices.Add(invoice);
        await SaveAsync(cancellationToken);
        return ToDto(invoice);
    }

    public async Task<InvoiceDto?> UpdateInvoiceAsync(
        long companyId,
        long invoiceId,
        UpdateInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        var invoice = await db.SalesInvoices.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == invoiceId, cancellationToken);
        if (invoice is null) return null;
        EnsureDraft(invoice.Status);
        if (invoice.AllocatedTotal != 0 || invoice.JournalEntryId is not null)
            throw new BillingConflictException("An invoice with allocations or a journal entry cannot be edited.");
        if (!await db.Parties.AnyAsync(
                party => party.Id == request.PartyId && party.IsCustomer, cancellationToken))
            throw Validation("partyId", "Choose a customer in this tenant.");

        var number = Normalize(request.InvoiceNumber).ToUpperInvariant();
        ValidateDocumentHeader(number, request.InvoiceDate, request.DueDate, request.ExchangeRate);
        var currency = await GetCurrencyAsync(request.CurrencyCode, cancellationToken);
        ValidateFunctionalExchangeRate(currency.Code, company.FunctionalCurrencyCode, request.ExchangeRate);
        var lines = await BuildLinesAsync(
            companyId, request.InvoiceDate, request.CurrencyCode, request.Lines, true, cancellationToken);
        invoice.CustomerId = request.PartyId;
        invoice.InvoiceNumber = number;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.DueDate = request.DueDate;
        invoice.CurrencyCode = currency.Code;
        invoice.ExchangeRate = request.ExchangeRate;
        invoice.NetTotal = lines.Sum(line => line.NetAmount);
        invoice.TaxTotal = lines.Sum(line => line.TaxAmount);
        invoice.GrandTotal = lines.Sum(line => line.GrossAmount);
        invoice.Lines.Clear();
        foreach (var line in lines)
            invoice.Lines.Add(new SalesInvoiceLine
            {
                TenantId = invoice.TenantId,
                CompanyId = companyId,
                AccountId = line.AccountId,
                TaxCodeId = line.TaxCodeId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                NetAmount = line.NetAmount,
                TaxRatePercent = line.TaxRatePercent,
                TaxAmount = line.TaxAmount,
                GrossAmount = line.GrossAmount
            });
        EnsureAmounts(invoice.NetTotal, invoice.TaxTotal, invoice.GrandTotal);
        invoice.Revision++;
        await SaveAsync(cancellationToken);
        return ToDto(invoice);
    }

    public async Task<InvoiceDto?> GetInvoiceAsync(
        long companyId,
        long invoiceId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var invoice = await db.SalesInvoices.AsNoTracking()
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == invoiceId, cancellationToken);
        return invoice is null ? null : ToDto(invoice);
    }

    public async Task<InvoiceDto?> PostInvoiceAsync(
        long companyId,
        long invoiceId,
        PostSalesInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
        var invoice = await db.SalesInvoices
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == invoiceId, cancellationToken);
        if (invoice is null)
        {
            return null;
        }

        EnsureDraft(invoice.Status);
        var period = await GetPostingPeriodAsync(companyId, request.AccountingPeriodId, invoice.InvoiceDate, cancellationToken);
        await EnsureJournalAsync(companyId, request.JournalId, cancellationToken);
        await EnsureAccountAsync(companyId, request.ReceivableAccountId, "Asset", cancellationToken);
        var taxAccounts = await ValidateTaxAccountsAsync(companyId, invoice.Lines.Select(line => line.TaxCodeId), request.TaxAccounts, "Liability", cancellationToken);

        var journalEntry = CreateDocumentJournal(
            tenantId, companyId, request.JournalId, period.Id, invoice.InvoiceDate,
            $"Sales invoice {invoice.InvoiceNumber}", invoice.CurrencyCode, invoice.ExchangeRate,
            await GetCurrencyDecimalPlacesAsync(company.FunctionalCurrencyCode, cancellationToken),
            invoice.GrandTotal,
            invoice.Lines.Select(line => (line.AccountId, line.TaxCodeId, line.NetAmount, line.TaxAmount, true)),
            request.ReceivableAccountId, taxAccounts);
        db.JournalEntries.Add(journalEntry);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            invoice.JournalEntryId = journalEntry.Id;
            invoice.Status = "Posted";
            invoice.Revision++;
            await db.SaveChangesAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This invoice was changed by another request. Reload it and try again.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BillingConflictException("This invoice was posted concurrently. Reload it before retrying.");
        }

        return ToDto(invoice);
    }
}
