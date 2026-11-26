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

    public async Task<PosSaleDto> CheckoutPosSaleAsync(
        long companyId,
        CheckoutPosSaleRequest request,
        CancellationToken cancellationToken)
    {
        var company = await EnsureCompanyAsync(companyId, cancellationToken);
        if (request.Lines is null || request.Lines.Count is < 1 or > 100)
            throw Validation("lines", "A POS sale must contain 1–100 lines.");
        if (!string.Equals(
                request.CurrencyCode,
                company.FunctionalCurrencyCode,
                StringComparison.OrdinalIgnoreCase) ||
            request.ExchangeRate != 1)
            throw Validation("currencyCode", "POS sales must use the company's functional currency.");
        if (request.ReceivableAccountId == request.SettlementAccountId)
            throw Validation("settlementAccountId", "Choose a settlement account different from the receivable account.");
        var itemIds = request.Lines.Select(line => line.ItemId).Distinct().ToArray();
        var pricedItemIds = await db.CompanyItemPrices
            .Where(price =>
                price.CompanyId == companyId &&
                price.CurrencyCode == company.FunctionalCurrencyCode &&
                price.UnitSellingPrice.HasValue &&
                itemIds.Contains(price.ItemId))
            .Select(price => price.ItemId)
            .ToHashSetAsync(cancellationToken);
        if (pricedItemIds.Count != itemIds.Length)
            throw Validation(
                "lines",
                "Configure a selling price in the selected company's functional currency for every item before checkout.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var invoiceLines = request.Lines.Select(line => new CreateDocumentLineRequest(
            line.RevenueAccountId,
            line.TaxCodeId,
            line.Description,
            line.Quantity,
            line.UnitPrice)).ToArray();
        var invoice = await CreateInvoiceAsync(
            companyId,
            new CreateInvoiceRequest(
                request.PartyId,
                request.InvoiceNumber,
                request.SaleDate,
                request.SaleDate,
                request.CurrencyCode,
                request.ExchangeRate,
                invoiceLines),
            cancellationToken);
        var postedInvoice = await PostInvoiceAsync(
            companyId,
            invoice.Id,
            new PostSalesInvoiceRequest(
                request.JournalId,
                request.AccountingPeriodId,
                request.ReceivableAccountId,
                request.TaxAccounts),
            cancellationToken)
            ?? throw new KeyNotFoundException("The POS invoice could not be found for posting.");

        var payment = await CreatePaymentAsync(
            companyId,
            new CreatePaymentRequest(
                request.PartyId,
                request.PaymentNumber,
                "Receive",
                request.SaleDate,
                request.CurrencyCode,
                request.ExchangeRate,
                postedInvoice.GrandTotal),
            cancellationToken);
        var allocatedPayment = await AllocatePaymentAsync(
            companyId,
            payment.Id,
            new AllocatePaymentRequest(postedInvoice.GrandTotal, postedInvoice.Id, null),
            cancellationToken)
            ?? throw new KeyNotFoundException("The POS payment could not be found for allocation.");
        var postedPayment = await PostPaymentAsync(
            companyId,
            allocatedPayment.Id,
            new PostPaymentRequest(
                request.JournalId,
                request.AccountingPeriodId,
                request.SettlementAccountId,
                request.ReceivableAccountId),
            cancellationToken)
            ?? throw new KeyNotFoundException("The POS payment could not be found for posting.");

        var movementNumber = 0;
        foreach (var lineGroup in request.Lines.GroupBy(line => line.ItemId))
        {
            movementNumber++;
            await inventory.IssueAsync(
                companyId,
                new IssueStockRequest(
                    $"POS-{invoice.Id}-{movementNumber}",
                    lineGroup.Key,
                    request.WarehouseId,
                    null,
                    request.JournalId,
                    request.AccountingPeriodId,
                    request.InventoryAccountId,
                    request.CostOfGoodsSoldAccountId,
                    request.SaleDate,
                    lineGroup.Sum(line => line.Quantity)),
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return new PosSaleDto(postedInvoice, postedPayment);
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

    public async Task<PagedResultDto<PaymentDto>> GetPaymentsPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        await EnsureCompanyAsync(companyId, cancellationToken);
        var result = await PagedQuery.ToPageAsync(
            db.Payments.AsNoTracking()
                .Where(payment => payment.CompanyId == companyId)
                .OrderByDescending(payment => payment.PaymentDate)
                .ThenByDescending(payment => payment.Id),
            page,
            pageSize,
            payment => payment,
            cancellationToken);
        return new PagedResultDto<PaymentDto>(
            result.Items.Select(ToDto).ToArray(), result.Page, result.PageSize, result.TotalCount, result.TotalPages);
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

        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
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

            if (request.Amount > SalesInvoiceBalance.AllocatableAmount(
                    invoice.GrandTotal,
                    invoice.AllocatedTotal,
                    invoice.CreditedTotal,
                    invoice.RefundedTotal) ||
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
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
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
        await using var transaction = db.Database.CurrentTransaction is null
            ? await db.Database.BeginTransactionAsync(cancellationToken)
            : null;
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
        if (functionalAmount <= 0)
        {
            throw Validation("amount", "Payment amount is too small to post in the company's functional currency.");
        }

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
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
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
}
