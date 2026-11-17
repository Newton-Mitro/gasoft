using Gasoft.Application.Abstractions;
using Gasoft.Application.Taxes;

namespace Gasoft.Api.Endpoints;

public static class TaxEndpoints
{
    public static IEndpointRouteBuilder MapTaxEndpoints(this IEndpointRouteBuilder app)
    {
        var jurisdictions = app.MapGroup("/api/v1/tax-jurisdictions")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);
        jurisdictions.MapGet("", async (ITaxService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetJurisdictionsAsync(cancellationToken)))
            .WithName("GetTaxJurisdictions");
        jurisdictions.MapGet("/page", async (
            int page,
            int pageSize,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            return Results.Ok(await service.GetJurisdictionsPageAsync(page, pageSize, cancellationToken));
        })
        .WithName("GetTaxJurisdictionsPage");

        var companyTaxCodes = app.MapGroup("/api/v1/companies/{companyId:long}/tax-codes")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);
        companyTaxCodes.MapGet("", async (
            long companyId,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            var codes = await service.GetCompanyTaxCodesAsync(companyId, cancellationToken);
            return codes is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(codes);
        })
        .WithName("GetCompanyTaxCodes");
        companyTaxCodes.MapGet("/page", async (
            long companyId,
            int page,
            int pageSize,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            var result = await service.GetCompanyTaxCodesPageAsync(companyId, page, pageSize, cancellationToken);
            return result is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(result);
        })
        .WithName("GetCompanyTaxCodesPage");

        companyTaxCodes.MapPost("", async (
            long companyId,
            TaxCodeRequest request,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var code = await service.CreateTaxCodeAsync(companyId, request, cancellationToken);
                return code is null
                    ? Results.NotFound(new { error = "Company was not found in this tenant." })
                    : Results.Created($"/api/v1/companies/{companyId}/tax-codes/{code.Id}", code);
            }
            catch (TaxValidationException exception) { return ValidationProblem(exception); }
            catch (TaxConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("CreateCompanyTaxCode");

        companyTaxCodes.MapPut("/{taxCodeId:long}", async (
            long companyId,
            long taxCodeId,
            TaxCodeRequest request,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var code = await service.UpdateTaxCodeAsync(companyId, taxCodeId, request, cancellationToken);
                return code is null
                    ? Results.NotFound(new { error = "Tax code was not found in this company." })
                    : Results.Ok(code);
            }
            catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
            catch (TaxValidationException exception) { return ValidationProblem(exception); }
            catch (TaxConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("UpdateCompanyTaxCode");

        companyTaxCodes.MapDelete("/{taxCodeId:long}", async (
            long companyId,
            long taxCodeId,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return await service.DeleteTaxCodeAsync(companyId, taxCodeId, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound(new { error = "Tax code was not found in this company." });
            }
            catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
            catch (TaxConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("DeleteCompanyTaxCode");

        companyTaxCodes.MapPost("/{taxCodeId:long}/rates", async (
            long companyId,
            long taxCodeId,
            TaxRateRequest request,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var rate = await service.CreateTaxRateAsync(companyId, taxCodeId, request, cancellationToken);
                return rate is null
                    ? Results.NotFound(new { error = "Tax code was not found in this company." })
                    : Results.Created(
                        $"/api/v1/companies/{companyId}/tax-codes/{taxCodeId}/rates/{rate.Id}",
                        rate);
            }
            catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
            catch (TaxValidationException exception) { return ValidationProblem(exception); }
            catch (TaxConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("CreateCompanyTaxRate");

        companyTaxCodes.MapPut("/{taxCodeId:long}/rates/{rateId:long}", async (
            long companyId,
            long taxCodeId,
            long rateId,
            TaxRateRequest request,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var rate = await service.UpdateTaxRateAsync(companyId, taxCodeId, rateId, request, cancellationToken);
                return rate is null
                    ? Results.NotFound(new { error = "Tax code was not found in this company." })
                    : Results.Ok(rate);
            }
            catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
            catch (TaxNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
            catch (TaxValidationException exception) { return ValidationProblem(exception); }
            catch (TaxConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("UpdateCompanyTaxRate");

        companyTaxCodes.MapDelete("/{taxCodeId:long}/rates/{rateId:long}", async (
            long companyId,
            long taxCodeId,
            long rateId,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return await service.DeleteTaxRateAsync(companyId, taxCodeId, rateId, cancellationToken)
                    ? Results.NoContent()
                    : Results.NotFound(new { error = "Tax code was not found in this company." });
            }
            catch (UnauthorizedAccessException) { return Results.Unauthorized(); }
            catch (TaxNotFoundException exception) { return Results.NotFound(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("DeleteCompanyTaxRate");

        app.MapGet("/api/v1/companies/{companyId:long}/tax-reference", async (
            long companyId,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            var reference = await service.GetCompanyTaxReferenceAsync(companyId, cancellationToken);
            return reference is null
                ? Results.NotFound(new { error = "Company was not found in this tenant." })
                : Results.Ok(reference);
        })
        .WithName("GetCompanyTaxReference");

        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/tax-registrations")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);
        companies.MapGet("", async (
            long companyId,
            ITaxService service,
            CancellationToken cancellationToken) =>
                Results.Ok(await service.GetRegistrationsAsync(companyId, cancellationToken)));
        companies.MapGet("/page", async (
            long companyId,
            int page,
            int pageSize,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            return Results.Ok(await service.GetRegistrationsPageAsync(companyId, page, pageSize, cancellationToken));
        });

        companies.MapGet("/{registrationId:long}", async (
            long companyId,
            long registrationId,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            var registration = await service.GetRegistrationAsync(companyId, registrationId, cancellationToken);
            return registration is null ? Results.NotFound() : Results.Ok(registration);
        });

        companies.MapPost("", async (
            long companyId,
            CreateTaxRegistrationRequest request,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var registration = await service.CreateRegistrationAsync(companyId, request, cancellationToken);
                return registration is null
                    ? Results.NotFound(new { error = "Company was not found in this tenant." })
                    : Results.Created(
                        $"/api/v1/companies/{companyId}/tax-registrations/{registration.Id}",
                        registration);
            }
            catch (TaxValidationException exception) { return ValidationProblem(exception); }
            catch (TaxConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"));

        companies.MapPut("/{registrationId:long}", async (
            long companyId,
            long registrationId,
            CreateTaxRegistrationRequest request,
            ITaxService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var registration = await service.UpdateRegistrationAsync(
                    companyId, registrationId, request, cancellationToken);
                return registration is null ? Results.NotFound() : Results.Ok(registration);
            }
            catch (TaxValidationException exception) { return ValidationProblem(exception); }
            catch (TaxConflictException exception) { return Results.Conflict(new { error = exception.Message }); }
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"));

        return app;
    }

    private static IResult ValidationProblem(TaxValidationException exception) =>
        Results.ValidationProblem(exception.Errors);
}
