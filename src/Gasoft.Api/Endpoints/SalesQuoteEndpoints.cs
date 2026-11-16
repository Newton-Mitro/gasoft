using Gasoft.Application.Abstractions;
using Gasoft.Application.Billing;

namespace Gasoft.Api.Endpoints;

public static class SalesQuoteEndpoints
{
    public static IEndpointRouteBuilder MapSalesQuoteEndpoints(this IEndpointRouteBuilder app)
    {
        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/sales-quotes")
            .RequireAuthorization();

        companies.MapGet("", async (
            long companyId,
            ISalesQuoteService service,
            CancellationToken cancellationToken) =>
        {
            var quotes = await service.GetQuotesAsync(companyId, cancellationToken);
            return quotes is null ? Results.NotFound() : Results.Ok(quotes);
        });
        companies.MapGet("/page", async (
            long companyId,
            int page,
            int pageSize,
            ISalesQuoteService service,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            var quotes = await service.GetQuotesPageAsync(companyId, page, pageSize, cancellationToken);
            return quotes is null ? Results.NotFound() : Results.Ok(quotes);
        });

        companies.MapGet("/{quoteId:long}", async (
            long companyId,
            long quoteId,
            ISalesQuoteService service,
            CancellationToken cancellationToken) =>
        {
            var quote = await service.GetQuoteAsync(companyId, quoteId, cancellationToken);
            return quote is null ? Results.NotFound() : Results.Ok(quote);
        });

        companies.MapPost("", async (
            long companyId,
            CreateSalesQuoteRequest request,
            ISalesQuoteService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var quote = await service.CreateQuoteAsync(companyId, request, cancellationToken);
                return Results.Created($"/api/v1/companies/{companyId}/sales-quotes/{quote.Id}", quote);
            }
            catch (BillingValidationException exception) { return ValidationProblem(exception); }
            catch (BillingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        });

        companies.MapPut("/{quoteId:long}", async (
            long companyId,
            long quoteId,
            UpdateSalesQuoteRequest request,
            ISalesQuoteService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var quote = await service.UpdateQuoteAsync(companyId, quoteId, request, cancellationToken);
                return quote is null ? Results.NotFound() : Results.Ok(quote);
            }
            catch (BillingValidationException exception) { return ValidationProblem(exception); }
            catch (BillingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        });

        companies.MapPost("/{quoteId:long}/send", async (
            long companyId,
            long quoteId,
            ISalesQuoteService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.SendQuoteAsync(companyId, quoteId, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (SalesQuoteDeliveryException exception) when (exception.Failure == SalesQuoteDeliveryFailure.Unavailable)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Quote delivery is not available",
                    detail: exception.Message);
            }
            catch (SalesQuoteDeliveryException exception) when (exception.Failure == SalesQuoteDeliveryFailure.EmailRejected)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status502BadGateway,
                    title: "Quote email could not be delivered",
                    detail: exception.Message);
            }
            catch (SalesQuoteDeliveryException exception)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status500InternalServerError,
                    title: "Quote email was delivered, but its status could not be updated",
                    detail: exception.Message);
            }
            catch (BillingValidationException exception) { return ValidationProblem(exception); }
            catch (BillingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        })
        .WithName("SendSalesQuote");

        companies.MapPost("/{quoteId:long}/{action}", async (
            long companyId,
            long quoteId,
            string action,
            ISalesQuoteService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.TransitionQuoteAsync(companyId, quoteId, action, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(new { result.Id, result.Status, result.Revision });
            }
            catch (BillingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        });

        companies.MapPost("/{quoteId:long}/convert", async (
            long companyId,
            long quoteId,
            ConvertSalesQuoteRequest request,
            ISalesQuoteService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await service.ConvertQuoteAsync(companyId, quoteId, request, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (BillingValidationException exception) { return ValidationProblem(exception); }
            catch (BillingConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        });

        return app;
    }

    private static IResult ValidationProblem(BillingValidationException exception) =>
        Results.ValidationProblem(exception.Errors);
}
