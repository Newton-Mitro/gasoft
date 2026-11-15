using Gasoft.Application.Abstractions;
using Gasoft.Application.Billing;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class BillingService(AppDbContext db, ICurrentTenant currentTenant) : IBillingService
{
    private const decimal MaximumAmount = 99_999_999_999_999.999999m;

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
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
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
            await transaction.CommitAsync(cancellationToken);
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

    public async Task<PaymentDto> CreatePaymentAsync(
        long companyId,
        CreatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        await EnsureCompanyAsync(companyId, cancellationToken);
        var errors = new Dictionary<string, string[]>();
        var direction = Normalize(request.Direction);
        if (direction is not ("Receive" or "Pay"))
        {
            errors["direction"] = ["Direction must be Receive or Pay."];
        }

        var paymentNumber = Normalize(request.PaymentNumber).ToUpperInvariant();
        if (paymentNumber.Length is 0 or > 40)
        {
            errors["paymentNumber"] = ["Payment number is required and must be at most 40 characters."];
        }

        if (request.Amount <= 0 || request.Amount > MaximumAmount)
        {
            errors["amount"] = ["Payment amount must be positive and within the supported range."];
        }

        ValidateExchangeRate(errors, request.ExchangeRate);
        ThrowIfInvalid(errors);
        var partyExists = await db.Parties.AnyAsync(
            party => party.Id == request.PartyId &&
                     (direction == "Receive" ? party.IsCustomer : party.IsSupplier),
            cancellationToken);
        if (!partyExists)
        {
            throw Validation("partyId", direction == "Receive"
                ? "Choose a customer in this tenant."
                : "Choose a supplier in this tenant.");
        }

        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        var currency = await GetCurrencyAsync(request.CurrencyCode, cancellationToken);
        ValidateFunctionalExchangeRate(currency.Code, company.FunctionalCurrencyCode, request.ExchangeRate);
        if (decimal.Round(request.Amount, currency.DecimalPlaces) != request.Amount)
        {
            throw Validation("amount", $"Currency {currency.Code} supports {currency.DecimalPlaces} decimal places.");
        }

        var payment = new Payment
        {
            TenantId = tenantId,
            CompanyId = companyId,
            PartyId = request.PartyId,
            PaymentNumber = paymentNumber,
            Direction = direction,
            PaymentDate = request.PaymentDate,
            CurrencyCode = currency.Code,
            ExchangeRate = request.ExchangeRate,
            Amount = request.Amount
        };
        db.Payments.Add(payment);
        await SaveAsync(cancellationToken);
        return ToDto(payment);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetPaymentsAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var payments = await db.Payments.AsNoTracking()
            .Where(payment => payment.CompanyId == companyId)
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.Id)
            .Take(500)
            .ToListAsync(cancellationToken);
        return payments.Select(ToDto).ToArray();
    }

    public async Task<PaymentDto?> GetPaymentAsync(
        long companyId,
        long paymentId,
        CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == paymentId, cancellationToken);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<PaymentDto?> UpdatePaymentAsync(
        long companyId,
        long paymentId,
        UpdatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        var payment = await db.Payments.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == paymentId, cancellationToken);
        if (payment is null) return null;
        EnsureDraft(payment.Status);
        if (payment.AllocatedAmount != 0 || payment.JournalEntryId is not null)
            throw new BillingConflictException("A payment with allocations or a journal entry cannot be edited.");

        var errors = new Dictionary<string, string[]>();
        var direction = Normalize(request.Direction);
        if (direction is not ("Receive" or "Pay"))
            errors["direction"] = ["Direction must be Receive or Pay."];
        var number = Normalize(request.PaymentNumber).ToUpperInvariant();
        if (number.Length is 0 or > 40)
            errors["paymentNumber"] = ["Payment number is required and must be at most 40 characters."];
        if (request.Amount <= 0 || request.Amount > MaximumAmount)
            errors["amount"] = ["Payment amount must be positive and within the supported range."];
        ValidateExchangeRate(errors, request.ExchangeRate);
        ThrowIfInvalid(errors);
        if (!await db.Parties.AnyAsync(
                party => party.Id == request.PartyId &&
                         (direction == "Receive" ? party.IsCustomer : party.IsSupplier),
                cancellationToken))
            throw Validation("partyId", direction == "Receive"
                ? "Choose a customer in this tenant."
                : "Choose a supplier in this tenant.");
        var currency = await GetCurrencyAsync(request.CurrencyCode, cancellationToken);
        ValidateFunctionalExchangeRate(currency.Code, company.FunctionalCurrencyCode, request.ExchangeRate);
        if (decimal.Round(request.Amount, currency.DecimalPlaces) != request.Amount)
            throw Validation("amount", $"Currency {currency.Code} supports {currency.DecimalPlaces} decimal places.");
        payment.PartyId = request.PartyId;
        payment.PaymentNumber = number;
        payment.Direction = direction;
        payment.PaymentDate = request.PaymentDate;
        payment.CurrencyCode = currency.Code;
        payment.ExchangeRate = request.ExchangeRate;
        payment.Amount = request.Amount;
        payment.Revision++;
        await SaveAsync(cancellationToken);
        return ToDto(payment);
    }

    public async Task<PaymentDto?> AllocatePaymentAsync(
        long companyId,
        long paymentId,
        AllocatePaymentRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var hasInvoice = request.SalesInvoiceId.HasValue;
        var hasBill = request.PurchaseBillId.HasValue;
        if (hasInvoice == hasBill)
        {
            throw Validation("target", "Specify exactly one sales invoice or purchase bill.");
        }

        if (request.Amount <= 0 || request.Amount > MaximumAmount)
        {
            throw Validation("amount", "Allocation amount must be positive and within the supported range.");
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var payment = await db.Payments.SingleOrDefaultAsync(
            item => item.Id == paymentId && item.CompanyId == companyId,
            cancellationToken);
        if (payment is null)
        {
            return null;
        }

        EnsureDraft(payment.Status);
        var paymentCurrency = await GetCurrencyAsync(payment.CurrencyCode, cancellationToken);
        if (decimal.Round(request.Amount, paymentCurrency.DecimalPlaces) != request.Amount)
        {
            throw Validation("amount", $"Currency {paymentCurrency.Code} supports {paymentCurrency.DecimalPlaces} decimal places.");
        }
        if (payment.Direction == "Receive" && !hasInvoice || payment.Direction == "Pay" && !hasBill)
        {
            throw Validation("target", "Payment direction does not match the selected document type.");
        }

        if (hasInvoice)
        {
            var invoice = await db.SalesInvoices.SingleOrDefaultAsync(
                item => item.Id == request.SalesInvoiceId && item.CompanyId == companyId,
                cancellationToken);
            if (invoice is null)
            {
                throw Validation("salesInvoiceId", "Invoice was not found in this company.");
            }

            if (invoice.Status != "Posted" || invoice.CustomerId != payment.PartyId ||
                invoice.CurrencyCode != payment.CurrencyCode)
            {
                throw Validation("salesInvoiceId", "Invoice must be posted and match this payment's customer and currency.");
            }

            if (request.Amount > invoice.GrandTotal - invoice.AllocatedTotal ||
                request.Amount > payment.Amount - payment.AllocatedAmount)
            {
                throw Validation("amount", "Allocation cannot exceed the remaining invoice balance or payment amount.");
            }

            invoice.AllocatedTotal += request.Amount;
            invoice.Revision++;
        }
        else
        {
            var bill = await db.PurchaseBills.SingleOrDefaultAsync(
                item => item.Id == request.PurchaseBillId && item.CompanyId == companyId,
                cancellationToken);
            if (bill is null)
            {
                throw Validation("purchaseBillId", "Bill was not found in this company.");
            }

            if (bill.Status != "Posted" || bill.SupplierId != payment.PartyId ||
                bill.CurrencyCode != payment.CurrencyCode)
            {
                throw Validation("purchaseBillId", "Bill must be posted and match this payment's supplier and currency.");
            }

            if (request.Amount > bill.GrandTotal - bill.AllocatedTotal ||
                request.Amount > payment.Amount - payment.AllocatedAmount)
            {
                throw Validation("amount", "Allocation cannot exceed the remaining bill balance or payment amount.");
            }

            bill.AllocatedTotal += request.Amount;
            bill.Revision++;
        }

        payment.AllocatedAmount += request.Amount;
        payment.Revision++;
        db.PaymentAllocations.Add(new PaymentAllocation
        {
            TenantId = payment.TenantId,
            CompanyId = companyId,
            PaymentId = payment.Id,
            SalesInvoiceId = request.SalesInvoiceId,
            PurchaseBillId = request.PurchaseBillId,
            Amount = request.Amount
        });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("The payment or document was allocated concurrently. Reload and retry.");
        }

        return ToDto(payment);
    }

    public async Task<PaymentDto?> PostPaymentAsync(
        long companyId,
        long paymentId,
        PostPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var payment = await db.Payments.SingleOrDefaultAsync(
            item => item.Id == paymentId && item.CompanyId == companyId,
            cancellationToken);
        if (payment is null)
        {
            return null;
        }

        EnsureDraft(payment.Status);
        var period = await GetPostingPeriodAsync(companyId, request.AccountingPeriodId, payment.PaymentDate, cancellationToken);
        await EnsureJournalAsync(companyId, request.JournalId, cancellationToken);
        await EnsureAccountAsync(companyId, request.CashAccountId, "Asset", cancellationToken);
        await EnsureAccountAsync(companyId, request.ControlAccountId,
            payment.Direction == "Receive" ? "Asset" : "Liability", cancellationToken);

        var amount = payment.Amount;
        var functionalAmount = ConvertToFunctional(
            amount,
            payment.ExchangeRate,
            await GetCurrencyDecimalPlacesAsync(company.FunctionalCurrencyCode, cancellationToken));
        var isReceipt = payment.Direction == "Receive";
        var journalEntry = new JournalEntry
        {
            TenantId = tenantId,
            CompanyId = companyId,
            JournalId = request.JournalId,
            AccountingPeriodId = period.Id,
            EntryDate = payment.PaymentDate,
            Description = $"Payment {payment.PaymentNumber}",
            Status = "Posted",
            TotalDebit = functionalAmount,
            TotalCredit = functionalAmount,
            Lines =
            [
                new JournalLine
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    TransactionCurrencyCode = payment.CurrencyCode,
                    AccountId = isReceipt ? request.CashAccountId : request.ControlAccountId,
                    Debit = isReceipt ? amount : 0,
                    Credit = isReceipt ? 0 : amount,
                    FunctionalDebit = isReceipt ? functionalAmount : 0,
                    FunctionalCredit = isReceipt ? 0 : functionalAmount,
                    ExchangeRate = payment.ExchangeRate,
                    Description = isReceipt ? "Cash received" : "Supplier balance settled"
                },
                new JournalLine
                {
                    TenantId = tenantId,
                    CompanyId = companyId,
                    TransactionCurrencyCode = payment.CurrencyCode,
                    AccountId = isReceipt ? request.ControlAccountId : request.CashAccountId,
                    Debit = isReceipt ? 0 : amount,
                    Credit = isReceipt ? amount : 0,
                    FunctionalDebit = isReceipt ? 0 : functionalAmount,
                    FunctionalCredit = isReceipt ? functionalAmount : 0,
                    ExchangeRate = payment.ExchangeRate,
                    Description = isReceipt ? "Customer balance settled" : "Cash paid"
                }
            ]
        };
        db.JournalEntries.Add(journalEntry);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            payment.JournalEntryId = journalEntry.Id;
            payment.Status = "Posted";
            payment.Revision++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This payment was changed by another request. Reload it and try again.");
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            throw new BillingConflictException("This payment was posted concurrently. Reload it before retrying.");
        }

        return ToDto(payment);
    }

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
        if (totalDebit <= 0 || totalDebit != totalCredit)
        {
            throw Validation("lines", "The document cannot be posted because rounded functional-currency amounts do not balance.");
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

    private static decimal ConvertToFunctional(decimal amount, decimal exchangeRate, int decimalPlaces) =>
        decimal.Round(amount * exchangeRate, decimalPlaces, MidpointRounding.AwayFromZero);

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
            invoice.AllocatedTotal, invoice.Status,
            invoice.Lines.OrderBy(line => line.Id).Select(line => new DocumentLineDto(
                line.Id, line.AccountId, line.TaxCodeId, line.Description, line.Quantity,
                line.UnitPrice, line.NetAmount, line.TaxRatePercent, line.TaxAmount, line.GrossAmount)).ToArray());

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
