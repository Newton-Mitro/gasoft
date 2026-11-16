using System.Globalization;
using System.Net.Mail;
using System.Text;
using System.Text.Encodings.Web;
using Gasoft.Application.Abstractions;
using Gasoft.Application.Billing;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class SalesQuoteService(
    AppDbContext db,
    ICurrentTenant currentTenant,
    IEmailSender emailSender,
    IBillingService billing,
    ILogger<SalesQuoteService> logger) : ISalesQuoteService
{
    private const decimal MaximumAmount = 99_999_999_999_999.999999m;

    public async Task<IReadOnlyList<SalesQuoteDto>?> GetQuotesAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        if (!await CompanyExistsAsync(companyId, cancellationToken)) return null;
        var quotes = await db.SalesQuotes.AsNoTracking().Include(quote => quote.Lines)
            .Where(quote => quote.CompanyId == companyId)
            .OrderByDescending(quote => quote.QuoteDate)
            .ThenByDescending(quote => quote.Id)
            .ToListAsync(cancellationToken);
        return quotes.Select(ToDto).ToArray();
    }

    public async Task<PagedResultDto<SalesQuoteDto>?> GetQuotesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await CompanyExistsAsync(companyId, cancellationToken)) return null;
        var result = await PagedQuery.ToPageAsync(
            db.SalesQuotes.AsNoTracking()
                .Where(quote => quote.CompanyId == companyId)
                .OrderByDescending(quote => quote.QuoteDate)
                .ThenByDescending(quote => quote.Id)
                .Include(quote => quote.Lines),
            page,
            pageSize,
            quote => quote,
            cancellationToken);
        return new PagedResultDto<SalesQuoteDto>(
            result.Items.Select(ToDto).ToArray(), result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    public async Task<SalesQuoteDto?> GetQuoteAsync(
        long companyId,
        long quoteId,
        CancellationToken cancellationToken)
    {
        var quote = await db.SalesQuotes.AsNoTracking().Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == quoteId, cancellationToken);
        return quote is null ? null : ToDto(quote);
    }

    public async Task<SalesQuoteDto> CreateQuoteAsync(
        long companyId,
        CreateSalesQuoteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var quote = await BuildQuoteAsync(
                companyId,
                request.CustomerId,
                request.QuoteNumber,
                request.QuoteDate,
                request.ValidUntil,
                request.CurrencyCode,
                request.ExchangeRate,
                request.Lines,
                cancellationToken);
            db.SalesQuotes.Add(quote);
            await db.SaveChangesAsync(cancellationToken);
            return ToDto(quote);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This quote was changed by another request. Reload it before retrying.");
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new BillingConflictException("A quote with this number already exists.");
        }
    }

    public async Task<SalesQuoteDto?> UpdateQuoteAsync(
        long companyId,
        long quoteId,
        UpdateSalesQuoteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var quote = await db.SalesQuotes.Include(item => item.Lines)
                .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == quoteId, cancellationToken);
            if (quote is null) return null;
            if (quote.Status != "Draft") throw new BillingConflictException("Only draft quotes can be edited.");
            var updated = await BuildQuoteAsync(
                companyId,
                request.CustomerId,
                request.QuoteNumber,
                request.QuoteDate,
                request.ValidUntil,
                request.CurrencyCode,
                request.ExchangeRate,
                request.Lines,
                cancellationToken,
                quoteId);
            quote.CustomerId = updated.CustomerId;
            quote.QuoteNumber = updated.QuoteNumber;
            quote.QuoteDate = updated.QuoteDate;
            quote.ValidUntil = updated.ValidUntil;
            quote.CurrencyCode = updated.CurrencyCode;
            quote.ExchangeRate = updated.ExchangeRate;
            quote.NetTotal = updated.NetTotal;
            quote.TaxTotal = updated.TaxTotal;
            quote.GrandTotal = updated.GrandTotal;
            db.SalesQuoteLines.RemoveRange(quote.Lines);
            quote.Lines = updated.Lines;
            quote.Revision++;
            await db.SaveChangesAsync(cancellationToken);
            return ToDto(quote);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This quote was changed by another request. Reload it before retrying.");
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new BillingConflictException("A quote with this number already exists.");
        }
    }

    public async Task<SalesQuoteStatusDto?> SendQuoteAsync(
        long companyId,
        long quoteId,
        CancellationToken cancellationToken)
    {
        if (!emailSender.CanSendEmail)
            throw new SalesQuoteDeliveryException(
                SalesQuoteDeliveryFailure.Unavailable,
                "Outgoing email is not configured. Contact your administrator.");

        await using var transaction = await db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var quote = await db.SalesQuotes
            .FromSqlInterpolated($"""
                SELECT * FROM "SalesQuotes"
                WHERE "CompanyId" = {companyId} AND "Id" = {quoteId}
                FOR UPDATE
                """)
            .SingleOrDefaultAsync(cancellationToken);
        if (quote is null) return null;
        if (quote.Status != "Draft") throw new BillingConflictException("Only draft quotes can be emailed.");
        if (quote.ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BillingConflictException("This quote has expired and cannot be sent.");

        var customer = await db.Parties.AsNoTracking()
            .SingleOrDefaultAsync(party => party.Id == quote.CustomerId && party.IsCustomer, cancellationToken);
        if (customer is null) throw new BillingConflictException("The quote customer is no longer available.");
        if (!MailAddress.TryCreate(customer.Email, out var recipient))
            throw Validation("customerEmail", "Add a valid email address to the quote's customer before sending.");
        var company = await db.Companies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
        if (company is null) throw new KeyNotFoundException("Company was not found.");
        var quoteLines = await db.SalesQuoteLines.AsNoTracking()
            .Where(line => line.SalesQuoteId == quoteId && line.CompanyId == companyId)
            .OrderBy(line => line.Id)
            .ToListAsync(cancellationToken);

        var encoder = HtmlEncoder.Default;
        var companyName = encoder.Encode(company.LegalName);
        var customerName = encoder.Encode(customer.DisplayName);
        var quoteNumber = encoder.Encode(quote.QuoteNumber);
        var currency = encoder.Encode(quote.CurrencyCode);
        var lineRows = new StringBuilder();
        var plainLines = new StringBuilder();
        foreach (var line in quoteLines)
        {
            var description = encoder.Encode(line.Description);
            var quantity = line.Quantity.ToString("0.######", CultureInfo.InvariantCulture);
            var amount = line.GrossAmount.ToString("N2", CultureInfo.InvariantCulture);
            lineRows.Append("<tr><td>").Append(description)
                .Append("</td><td>").Append(quantity)
                .Append("</td><td>").Append(amount).Append(' ').Append(currency)
                .Append("</td></tr>");
            plainLines.AppendLine($"- {line.Description} | {quantity} | {amount} {quote.CurrencyCode}");
        }

        var validUntil = quote.ValidUntil.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var netTotal = quote.NetTotal.ToString("N2", CultureInfo.InvariantCulture);
        var taxTotal = quote.TaxTotal.ToString("N2", CultureInfo.InvariantCulture);
        var grandTotal = quote.GrandTotal.ToString("N2", CultureInfo.InvariantCulture);
        var htmlBody = $"""
            <p>Hello {customerName},</p>
            <p>{companyName} has prepared quote <strong>{quoteNumber}</strong>, valid through {validUntil}.</p>
            <table border="1" cellpadding="6" cellspacing="0">
              <thead><tr><th>Description</th><th>Quantity</th><th>Amount</th></tr></thead>
              <tbody>{lineRows}</tbody>
            </table>
            <p>Subtotal: {netTotal} {currency}<br>
            Tax: {taxTotal} {currency}<br>
            <strong>Total: {grandTotal} {currency}</strong></p>
            <p>Please contact {companyName} if you have questions about this quote.</p>
            """;
        var textBody = $"""
            Hello {customer.DisplayName},

            {company.LegalName} has prepared quote {quote.QuoteNumber}, valid through {validUntil}.

            {plainLines}
            Subtotal: {netTotal} {quote.CurrencyCode}
            Tax: {taxTotal} {quote.CurrencyCode}
            Total: {grandTotal} {quote.CurrencyCode}

            Please contact {company.LegalName} if you have questions about this quote.
            """;

        try
        {
            await emailSender.SendEmailAsync(
                recipient.Address,
                $"Your sales quote from {company.LegalName}",
                htmlBody,
                textBody,
                cancellationToken);
        }
        catch (SmtpException exception)
        {
            logger.LogError(exception, "Could not deliver sales quote {QuoteId} for company {CompanyId}.", quoteId, companyId);
            throw new SalesQuoteDeliveryException(
                SalesQuoteDeliveryFailure.EmailRejected,
                "The email service rejected or could not deliver this message. The quote remains a draft.",
                exception);
        }

        quote.Status = "Sent";
        quote.SentAt = DateTimeOffset.UtcNow;
        quote.SentTo = recipient.Address;
        quote.Revision++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.LogError(exception, "Quote email was delivered but quote {QuoteId} status could not be updated.", quoteId);
            throw new SalesQuoteDeliveryException(
                SalesQuoteDeliveryFailure.StatusUpdateFailed,
                "Reload the quote before taking further action to avoid sending a duplicate.",
                exception);
        }
        return ToStatusDto(quote);
    }

    public async Task<SalesQuoteStatusDto?> TransitionQuoteAsync(
        long companyId,
        long quoteId,
        string action,
        CancellationToken cancellationToken)
    {
        var quote = await db.SalesQuotes.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == quoteId,
            cancellationToken);
        if (quote is null) return null;
        var nextStatus = (action, quote.Status) switch
        {
            ("accept", "Sent") => "Accepted",
            ("decline", "Sent") => "Declined",
            ("cancel", "Draft" or "Sent" or "Accepted") => "Cancelled",
            _ => null
        };
        if (nextStatus is null)
            throw new BillingConflictException($"Cannot {action} a quote in {quote.Status} status.");
        if ((nextStatus is "Sent" or "Accepted") && quote.ValidUntil < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BillingConflictException("This quote has expired and cannot be sent or accepted.");
        quote.Status = nextStatus;
        quote.Revision++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This quote was changed by another request. Reload it before retrying.");
        }
        return ToStatusDto(quote);
    }

    public async Task<ConvertedSalesQuoteDto?> ConvertQuoteAsync(
        long companyId,
        long quoteId,
        ConvertSalesQuoteRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var quote = await db.SalesQuotes.Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.CompanyId == companyId && item.Id == quoteId, cancellationToken);
        if (quote is null) return null;
        if (quote.Status != "Accepted")
            throw new BillingConflictException("Only accepted quotes can be converted to an invoice.");
        if (DateOnly.FromDateTime(DateTime.UtcNow) > quote.ValidUntil)
            throw new BillingConflictException("This quote has expired and cannot be converted.");
        if (request.InvoiceDate < quote.QuoteDate || request.InvoiceDate > quote.ValidUntil ||
            request.DueDate < request.InvoiceDate || string.IsNullOrWhiteSpace(request.InvoiceNumber))
            throw Validation(
                "invoiceDate",
                "Choose an invoice date within the quote validity period and a due date on or after it.");

        try
        {
            var invoice = await billing.CreateInvoiceAsync(companyId, new CreateInvoiceRequest(
                quote.CustomerId,
                request.InvoiceNumber.Trim(),
                request.InvoiceDate,
                request.DueDate,
                quote.CurrencyCode,
                quote.ExchangeRate,
                quote.Lines.OrderBy(line => line.Id).Select(line => new CreateDocumentLineRequest(
                    line.AccountId, line.TaxCodeId, line.Description, line.Quantity, line.UnitPrice)).ToArray()),
                cancellationToken);
            quote.SalesInvoiceId = invoice.Id;
            quote.Status = "Converted";
            quote.Revision++;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ConvertedSalesQuoteDto(quote.Id, invoice.Id, quote.Status);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new BillingConflictException("This quote was converted or changed by another request.");
        }
    }

    private async Task<SalesQuote> BuildQuoteAsync(
        long companyId,
        long customerId,
        string quoteNumber,
        DateOnly quoteDate,
        DateOnly validUntil,
        string currencyCode,
        decimal exchangeRate,
        IReadOnlyList<CreateDocumentLineRequest> requestedLines,
        CancellationToken cancellationToken,
        long? ignoreQuoteId = null)
    {
        if (!await CompanyExistsAsync(companyId, cancellationToken))
            throw new KeyNotFoundException("Company was not found.");
        var number = quoteNumber?.Trim() ?? string.Empty;
        var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
        if (company is null) throw new KeyNotFoundException("Company was not found.");
        var currency = currencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (number.Length is 0 or > 40 || quoteDate == default || validUntil < quoteDate ||
            currency.Length != 3 || exchangeRate <= 0 || exchangeRate > 9_999_999_999.9999999999m ||
            (currency == company.FunctionalCurrencyCode && exchangeRate != 1) ||
            !await db.Parties.AnyAsync(party => party.Id == customerId && party.IsCustomer, cancellationToken) ||
            !await db.Currencies.AnyAsync(item => item.Code == currency, cancellationToken))
            throw Validation("quote", "Enter a valid customer, quote number, dates, currency, and positive exchange rate.");
        number = number.ToUpperInvariant();
        if (requestedLines is null || requestedLines.Count is < 1 or > 1000)
            throw Validation("lines", "A quote must contain 1–1000 lines.");
        if (await db.SalesQuotes.AnyAsync(
                item => item.CompanyId == companyId && item.QuoteNumber == number && item.Id != ignoreQuoteId,
                cancellationToken))
            throw new BillingConflictException("A quote with this number already exists.");

        var digits = await db.Currencies.Where(item => item.Code == currency)
            .Select(item => item.DecimalPlaces).SingleAsync(cancellationToken);
        var accountIds = requestedLines.Select(line => line.AccountId).Distinct().ToArray();
        var accounts = await (from account in db.ChartOfAccounts
                              join accountGroup in db.AccountGroups on account.AccountGroupId equals accountGroup.Id
                              where account.CompanyId == companyId && accountIds.Contains(account.Id)
                              select new { account.Id, account.IsActive, account.AllowPosting, accountGroup.AccountType })
            .ToDictionaryAsync(row => row.Id, cancellationToken);
        if (accounts.Count != accountIds.Length || accounts.Values.Any(account =>
                !account.IsActive || !account.AllowPosting || account.AccountType != "Revenue"))
            throw Validation("lines", "Each quote line must use an active, postable revenue account in this company.");

        var taxIds = requestedLines.Where(line => line.TaxCodeId.HasValue)
            .Select(line => line.TaxCodeId!.Value).Distinct().ToArray();
        var taxRates = new Dictionary<long, decimal>();
        if (taxIds.Length > 0)
        {
            var registrations = await db.TenantTaxRegistrations.Where(item => item.CompanyId == companyId)
                .Select(item => item.JurisdictionId).ToArrayAsync(cancellationToken);
            var rows = await (from rate in db.TaxRates
                              join code in db.TaxCodes on rate.TaxCodeId equals code.Id
                              where taxIds.Contains(code.Id) && registrations.Contains(code.JurisdictionId) &&
                                    ((code.TenantId == null && code.CompanyId == null) ||
                                     (code.TenantId == currentTenant.TenantId && code.CompanyId == companyId)) &&
                                    rate.ValidFrom <= quoteDate && (rate.ValidTo == null || rate.ValidTo >= quoteDate)
                              select new { code.Id, rate.RatePercent }).ToListAsync(cancellationToken);
            foreach (var taxId in taxIds)
            {
                var matching = rows.Where(row => row.Id == taxId).ToArray();
                if (matching.Length != 1)
                    throw Validation("lines", $"Tax code {taxId} must have exactly one effective rate and an active company registration.");
                taxRates.Add(taxId, matching[0].RatePercent);
            }
        }

        var tenantId = currentTenant.TenantId
            ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");
        var quote = new SalesQuote
        {
            TenantId = tenantId,
            CompanyId = companyId,
            CustomerId = customerId,
            QuoteNumber = number,
            QuoteDate = quoteDate,
            ValidUntil = validUntil,
            CurrencyCode = currency,
            ExchangeRate = exchangeRate,
            Status = "Draft"
        };
        foreach (var (line, index) in requestedLines.Select((line, index) => (line, index)))
        {
            var description = line.Description?.Trim() ?? string.Empty;
            if (description.Length is 0 or > 500 || line.Quantity <= 0 || line.Quantity > 9_999_999_999.999999m ||
                decimal.Round(line.Quantity, 6) != line.Quantity || line.UnitPrice < 0 ||
                line.UnitPrice > MaximumAmount || decimal.Round(line.UnitPrice, 6) != line.UnitPrice)
                throw Validation($"lines[{index}]", "Line description is required; quantity and unit price must be valid to 6 decimal places.");
            var net = decimal.Round(line.Quantity * line.UnitPrice, digits, MidpointRounding.AwayFromZero);
            var rate = line.TaxCodeId.HasValue ? taxRates[line.TaxCodeId.Value] : 0m;
            var tax = decimal.Round(net * rate / 100m, digits, MidpointRounding.AwayFromZero);
            var gross = net + tax;
            if (net > MaximumAmount || tax > MaximumAmount || gross > MaximumAmount)
                throw Validation($"lines[{index}]", "Calculated line amount exceeds the supported range.");
            quote.Lines.Add(new SalesQuoteLine
            {
                TenantId = quote.TenantId,
                CompanyId = companyId,
                AccountId = line.AccountId,
                TaxCodeId = line.TaxCodeId,
                Description = description,
                Quantity = line.Quantity,
                UnitPrice = line.UnitPrice,
                NetAmount = net,
                TaxRatePercent = rate,
                TaxAmount = tax,
                GrossAmount = gross
            });
        }
        quote.NetTotal = quote.Lines.Sum(line => line.NetAmount);
        quote.TaxTotal = quote.Lines.Sum(line => line.TaxAmount);
        quote.GrandTotal = quote.NetTotal + quote.TaxTotal;
        if (quote.NetTotal > MaximumAmount || quote.TaxTotal > MaximumAmount || quote.GrandTotal > MaximumAmount)
            throw Validation("lines", "Quote total exceeds the supported amount range.");
        return quote;
    }

    private Task<bool> CompanyExistsAsync(long companyId, CancellationToken cancellationToken) =>
        db.Companies.AnyAsync(company => company.Id == companyId, cancellationToken);

    private static SalesQuoteDto ToDto(SalesQuote quote) => new(
        quote.Id, quote.CompanyId, quote.CustomerId, quote.SalesInvoiceId, quote.QuoteNumber,
        quote.QuoteDate, quote.ValidUntil, quote.CurrencyCode, quote.ExchangeRate,
        quote.NetTotal, quote.TaxTotal, quote.GrandTotal, quote.Status, quote.SentAt, quote.SentTo,
        quote.Lines.OrderBy(line => line.Id).Select(line => new DocumentLineDto(
            line.Id, line.AccountId, line.TaxCodeId, line.Description, line.Quantity,
            line.UnitPrice, line.NetAmount, line.TaxRatePercent, line.TaxAmount, line.GrossAmount)).ToArray());

    private static SalesQuoteStatusDto ToStatusDto(SalesQuote quote) =>
        new(quote.Id, quote.Status, quote.SentAt, quote.SentTo, quote.Revision);

    private static BillingValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
