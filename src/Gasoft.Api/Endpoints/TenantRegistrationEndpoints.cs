using Gasoft.Application.Abstractions;

namespace Gasoft.Api.Endpoints;

public static class TenantRegistrationEndpoints
{
    public static IEndpointRouteBuilder MapTenantRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/currencies", async (
                ICompanyReadService companyReadService,
                CancellationToken cancellationToken) =>
                Results.Ok(await companyReadService.GetCurrenciesAsync(cancellationToken)))
            .AllowAnonymous()
            .WithName("GetSupportedCurrencies");

        app.MapGet("/api/v1/companies/{companyId:long}", async (
            long companyId,
            ICompanyReadService companyReadService,
            CancellationToken cancellationToken) =>
        {
            var company = await companyReadService.GetCompanyAsync(companyId, cancellationToken);
            return company is null ? Results.NotFound() : Results.Ok(company);
        })
        .RequireAuthorization()
        .WithName("GetCompany");

        app.MapPost("/api/v1/tenants/register", async (
            TenantRegistrationRequest request,
            ITenantRegistrationService registrationService,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await registrationService.RegisterAsync(request, cancellationToken);
                return Results.Created(
                    $"/api/v1/companies/{result.CompanyId}",
                    new TenantRegistrationResponse(result.TenantId, result.CompanyId, result.AccessToken));
            }
            catch (RegistrationValidationException exception)
            {
                return Results.ValidationProblem(
                    exception.Errors.ToDictionary(error => error.Key, error => error.Value));
            }
            catch (RegistrationConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .AllowAnonymous()
        .RequireRateLimiting("public-auth")
        .WithName("RegisterTenant");

        return app;
    }

    public sealed record TenantRegistrationResponse(long TenantId, long CompanyId, string AccessToken);
}
