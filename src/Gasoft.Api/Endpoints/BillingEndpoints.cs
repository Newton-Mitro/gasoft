using Gasoft.Application.Abstractions;
using Gasoft.Application.Billing;

namespace Gasoft.Api.Endpoints;

public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);

        companies.MapGet("/sales-invoices", async (
            long companyId,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetInvoicesAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/sales-invoices/page", async (
            long companyId,
            IBillingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            try
            {
                return Results.Ok(await service.GetInvoicesPageAsync(companyId, page, pageSize, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/sales-invoices", async (
            long companyId,
            CreateInvoiceRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var invoice = await service.CreateInvoiceAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/sales-invoices/{invoice.Id}", invoice);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/sales-invoices/{invoiceId:long}", async (
            long companyId,
            long invoiceId,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            var invoice = await service.GetInvoiceAsync(companyId, invoiceId, cancellationToken);
            return invoice is null ? Results.NotFound() : Results.Ok(invoice);
        });

        companies.MapPut("/sales-invoices/{invoiceId:long}", async (
            long companyId,
            long invoiceId,
            UpdateInvoiceRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var invoice = await service.UpdateInvoiceAsync(companyId, invoiceId, request, cancellationToken);
                return invoice is null ? Results.NotFound() : Results.Ok(invoice);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/sales-invoices/{invoiceId:long}/post", async (
            long companyId,
            long invoiceId,
            PostSalesInvoiceRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var invoice = await service.PostInvoiceAsync(companyId, invoiceId, request, cancellationToken);
                return invoice is null ? Results.NotFound() : Results.Ok(invoice);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/sales-credit-notes", async (
            long companyId,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetSalesCreditNotesAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/sales-credit-notes/page", async (
            long companyId,
            IBillingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            try
            {
                return Results.Ok(await service.GetSalesCreditNotesPageAsync(companyId, page, pageSize, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapPost("/sales-credit-notes", async (
            long companyId,
            CreateSalesCreditNoteRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var note = await service.CreateSalesCreditNoteAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/sales-credit-notes/{note.Id}", note);
            }
            catch (BillingValidationException exception) { return ValidationProblem(exception); }
            catch (BillingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        });

        companies.MapPost("/sales-credit-notes/{creditNoteId:long}/post", async (
            long companyId,
            long creditNoteId,
            PostSalesCreditNoteRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var note = await service.PostSalesCreditNoteAsync(companyId, creditNoteId, request, cancellationToken);
                return note is null ? Results.NotFound() : Results.Ok(note);
            }
            catch (BillingValidationException exception) { return ValidationProblem(exception); }
            catch (BillingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        });

        companies.MapPost("/sales-credit-notes/{creditNoteId:long}/refunds", async (
            long companyId,
            long creditNoteId,
            CreateCustomerRefundRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var refund = await service.RefundSalesCreditNoteAsync(companyId, creditNoteId, request, cancellationToken);
                return refund is null ? Results.NotFound() : Results.Created(
                    $"/api/v1/companies/{companyId}/sales-credit-notes/{creditNoteId}/refunds/{refund.Id}", refund);
            }
            catch (BillingValidationException exception) { return ValidationProblem(exception); }
            catch (BillingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        });

        companies.MapGet("/purchase-bills", async (
            long companyId,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetBillsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/purchase-bills/page", async (
            long companyId,
            IBillingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            try
            {
                return Results.Ok(await service.GetBillsPageAsync(companyId, page, pageSize, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/purchase-bills", async (
            long companyId,
            CreateBillRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var bill = await service.CreateBillAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/purchase-bills/{bill.Id}", bill);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/purchase-bills/{billId:long}", async (
            long companyId,
            long billId,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            var bill = await service.GetBillAsync(companyId, billId, cancellationToken);
            return bill is null ? Results.NotFound() : Results.Ok(bill);
        });

        companies.MapPut("/purchase-bills/{billId:long}", async (
            long companyId,
            long billId,
            UpdateBillRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var bill = await service.UpdateBillAsync(companyId, billId, request, cancellationToken);
                return bill is null ? Results.NotFound() : Results.Ok(bill);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/purchase-bills/{billId:long}/post", async (
            long companyId,
            long billId,
            PostPurchaseBillRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var bill = await service.PostBillAsync(companyId, billId, request, cancellationToken);
                return bill is null ? Results.NotFound() : Results.Ok(bill);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/payments", async (
            long companyId,
            CreatePaymentRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var payment = await service.CreatePaymentAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/payments/{payment.Id}", payment);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/payments", async (
            long companyId,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetPaymentsAsync(companyId, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });
        companies.MapGet("/payments/page", async (
            long companyId,
            IBillingService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 25) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            try
            {
                return Results.Ok(await service.GetPaymentsPageAsync(companyId, page, pageSize, cancellationToken));
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapGet("/payments/{paymentId:long}", async (
            long companyId,
            long paymentId,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            var payment = await service.GetPaymentAsync(companyId, paymentId, cancellationToken);
            return payment is null ? Results.NotFound() : Results.Ok(payment);
        });

        companies.MapPut("/payments/{paymentId:long}", async (
            long companyId,
            long paymentId,
            UpdatePaymentRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var payment = await service.UpdatePaymentAsync(companyId, paymentId, request, cancellationToken);
                return payment is null ? Results.NotFound() : Results.Ok(payment);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        companies.MapPost("/payments/{paymentId:long}/allocations", async (
            long companyId,
            long paymentId,
            AllocatePaymentRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var payment = await service.AllocatePaymentAsync(companyId, paymentId, request, cancellationToken);
                return payment is null ? Results.NotFound() : Results.Ok(payment);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        });

        companies.MapPost("/payments/{paymentId:long}/post", async (
            long companyId,
            long paymentId,
            PostPaymentRequest request,
            IBillingService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var payment = await service.PostPaymentAsync(companyId, paymentId, request, cancellationToken);
                return payment is null ? Results.NotFound() : Results.Ok(payment);
            }
            catch (BillingValidationException exception)
            {
                return ValidationProblem(exception);
            }
            catch (BillingConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
            catch (KeyNotFoundException exception)
            {
                return Results.NotFound(new { error = exception.Message });
            }
        });

        return app;
    }

    private static IResult ValidationProblem(BillingValidationException exception) =>
        Results.ValidationProblem(exception.Errors.ToDictionary(error => error.Key, error => error.Value));
}
