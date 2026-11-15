using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Endpoints;

public static class CompanyAdministrationEndpoints
{
    public static IEndpointRouteBuilder MapCompanyAdministrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/tenant/companies", async (
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var companies = await db.Companies
                .AsNoTracking()
                .OrderBy(company => company.LegalName)
                .ThenBy(company => company.Code)
                .Select(company => new CompanyResponse(
                    company.Id,
                    company.Code,
                    company.LegalName,
                    company.FunctionalCurrencyCode))
                .ToListAsync(cancellationToken);

            return Results.Ok(companies);
        })
        .RequireAuthorization()
        .WithName("GetTenantCompanies");

        app.MapPost("/api/v1/companies/{companyId:long}/setup/defaults", async (
            long companyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var company = await db.Companies.SingleOrDefaultAsync(
                item => item.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            var seeded = await CompanySetupSeeder.SeedAsync(
                db, company.TenantId, company, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Results.Ok(seeded);
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("SeedCompanyDefaults");

        app.MapPut("/api/v1/companies/{companyId:long}", async (
            long companyId,
            UpdateCompanyRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var company = await db.Companies.SingleOrDefaultAsync(
                item => item.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

            var legalName = request.LegalName?.Trim() ?? string.Empty;
            var currencyCode = request.FunctionalCurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
            var errors = new Dictionary<string, string[]>();
            if (legalName.Length is 0 or > 250)
                errors["legalName"] = ["Legal name is required and must be at most 250 characters."];
            if (currencyCode.Length != 3 ||
                !await db.Currencies.AnyAsync(item => item.Code == currencyCode, cancellationToken))
                errors["functionalCurrencyCode"] = ["Choose a supported three-letter currency code."];

            if (errors.Count != 0) return Results.ValidationProblem(errors);
            if (currencyCode != company.FunctionalCurrencyCode &&
                await db.JournalEntries.AnyAsync(entry => entry.CompanyId == companyId, cancellationToken))
            {
                return Results.Conflict(new
                {
                    error = "Functional currency cannot be changed after journal entries have been created."
                });
            }

            company.LegalName = legalName;
            company.FunctionalCurrencyCode = currencyCode;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new CompanyResponse(
                company.Id, company.Code, company.LegalName, company.FunctionalCurrencyCode));
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("UpdateCompany");

        return app;
    }

    public sealed record UpdateCompanyRequest(string? LegalName, string? FunctionalCurrencyCode);
    public sealed record CompanyResponse(long Id, string Code, string LegalName, string FunctionalCurrencyCode);
}
