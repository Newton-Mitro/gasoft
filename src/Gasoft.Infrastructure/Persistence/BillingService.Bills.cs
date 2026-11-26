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
    public async Task<IReadOnlyList<BillDto>> GetBillsAsync(long companyId, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var bills = await db.PurchaseBills.AsNoTracking()
            .Include(bill => bill.Lines)
            .Where(bill => bill.CompanyId == companyId)
            .OrderByDescending(bill => bill.BillDate)
            .ThenByDescending(bill => bill.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
        return bills.Select(ToDto).ToArray();
    }

    public async Task<PagedResultDto<BillDto>> GetBillsPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var result = await PagedQuery.ToPageAsync(
            db.PurchaseBills.AsNoTracking()
                .Where(bill => bill.CompanyId == companyId)
                .OrderByDescending(bill => bill.BillDate)
                .ThenByDescending(bill => bill.Id)
                .Include(bill => bill.Lines),
            page,
            pageSize,
            bill => bill,
            cancellationToken);
        return new PagedResultDto<BillDto>(
            result.Items.Select(ToDto).ToArray(), result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    public async Task<BillDto> CreateBillAsync(
        long companyId,
        CreateBillRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        if (!await db.Parties.AnyAsync(
                party => party.Id == request.PartyId && party.IsSupplier,
                cancellationToken))
        {
            throw Validation("partyId", "Choose a supplier in this tenant.");
        }

        var billNumber = Normalize(request.BillNumber).ToUpperInvariant();
        ValidateDocumentHeader(billNumber, request.BillDate, request.DueDate, request.ExchangeRate);
        var currency = await GetCurrencyAsync(request.CurrencyCode, cancellationToken);
        ValidateFunctionalExchangeRate(currency.Code, company.FunctionalCurrencyCode, request.ExchangeRate);
        var lines = await BuildLinesAsync(
            companyId, request.BillDate, request.CurrencyCode, request.Lines, false, cancellationToken);
        var bill = new PurchaseBill
        {
            TenantId = tenantId,
            CompanyId = companyId,
            SupplierId = request.PartyId,
            BillNumber = billNumber,
            BillDate = request.BillDate,
            DueDate = request.DueDate,
            CurrencyCode = currency.Code,
            ExchangeRate = request.ExchangeRate,
            NetTotal = lines.Sum(line => line.NetAmount),
            TaxTotal = lines.Sum(line => line.TaxAmount),
            GrandTotal = lines.Sum(line => line.GrossAmount),
            Lines = lines.Select(line => new PurchaseBillLine
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
        EnsureAmounts(bill.NetTotal, bill.TaxTotal, bill.GrandTotal);
        db.PurchaseBills.Add(bill);
        await SaveAsync(cancellationToken);
        return ToDto(bill);
    }

    public async Task<BillDto?> UpdateBillAsync(
        long companyId,
        long billId,
        UpdateBillRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        var bill = await db.PurchaseBills.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == billId, cancellationToken);
        if (bill is null) return null;
        EnsureDraft(bill.Status);
        if (bill.AllocatedTotal != 0 || bill.JournalEntryId is not null)
            throw new BillingConflictException("A bill with allocations or a journal entry cannot be edited.");
        if (!await db.Parties.AnyAsync(
                party => party.Id == request.PartyId && party.IsSupplier, cancellationToken))
            throw Validation("partyId", "Choose a supplier in this tenant.");

        var number = Normalize(request.BillNumber).ToUpperInvariant();
        ValidateDocumentHeader(number, request.BillDate, request.DueDate, request.ExchangeRate);
        var currency = await GetCurrencyAsync(request.CurrencyCode, cancellationToken);
        ValidateFunctionalExchangeRate(currency.Code, company.FunctionalCurrencyCode, request.ExchangeRate);
        var lines = await BuildLinesAsync(
            companyId, request.BillDate, request.CurrencyCode, request.Lines, false, cancellationToken);
        bill.SupplierId = request.PartyId;
        bill.BillNumber = number;
        bill.BillDate = request.BillDate;
        bill.DueDate = request.DueDate;
        bill.CurrencyCode = currency.Code;
        bill.ExchangeRate = request.ExchangeRate;
        bill.NetTotal = lines.Sum(line => line.NetAmount);
        bill.TaxTotal = lines.Sum(line => line.TaxAmount);
        bill.GrandTotal = lines.Sum(line => line.GrossAmount);
        bill.Lines.Clear();
        foreach (var line in lines)
            bill.Lines.Add(new PurchaseBillLine
            {
                TenantId = bill.TenantId,
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
        EnsureAmounts(bill.NetTotal, bill.TaxTotal, bill.GrandTotal);
        bill.Revision++;
        await SaveAsync(cancellationToken);
        return ToDto(bill);
    }

    public async Task<BillDto?> GetBillAsync(
        long companyId,
        long billId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var bill = await db.PurchaseBills.AsNoTracking()
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == billId, cancellationToken);
        return bill is null ? null : ToDto(bill);
    }

    public async Task<BillDto?> PostBillAsync(
        long companyId,
        long billId,
        PostPurchaseBillRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var bill = await db.PurchaseBills
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == billId, cancellationToken);
        if (bill is null)
        {
            return null;
        }

        EnsureDraft(bill.Status);
        var period = await GetPostingPeriodAsync(companyId, request.AccountingPeriodId, bill.BillDate, cancellationToken);
        await EnsureJournalAsync(companyId, request.JournalId, cancellationToken);
        await EnsureAccountAsync(companyId, request.PayableAccountId, "Liability", cancellationToken);
        var taxAccounts = await ValidateTaxAccountsAsync(companyId, bill.Lines.Select(line => line.TaxCodeId), request.TaxAccounts, "Asset", cancellationToken);

        var journalEntry = CreateDocumentJournal(
            tenantId, companyId, request.JournalId, period.Id, bill.BillDate,
            $"Purchase bill {bill.BillNumber}", bill.CurrencyCode, bill.ExchangeRate,
            await GetCurrencyDecimalPlacesAsync(company.FunctionalCurrencyCode, cancellationToken),
            bill.GrandTotal,
            bill.Lines.Select(line => (line.AccountId, line.TaxCodeId, line.NetAmount, line.TaxAmount, false)),
            request.PayableAccountId, taxAccounts);
        db.JournalEntries.Add(journalEntry);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            bill.JournalEntryId = journalEntry.Id;
            bill.Status = "Posted";
            bill.Revision++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This bill was changed by another request. Reload it and try again.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BillingConflictException("This bill was posted concurrently. Reload it before retrying.");
        }

        return ToDto(bill);
    }
}
