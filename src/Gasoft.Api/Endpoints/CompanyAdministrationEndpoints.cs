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
                    company.FunctionalCurrencyCode,
                    company.Phone,
                    company.Email,
                    company.Website,
                    company.Address,
                    company.CityRegion,
                    company.PostalCode,
                    company.Country,
                    company.TaxId,
                    company.LogoUrl))
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

        app.MapGet("/api/v1/companies/{companyId:long}/logo/{fileName}", async (
            long companyId,
            string fileName,
            AppDbContext db,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var company = await db.Companies.SingleOrDefaultAsync(
                item => item.Id == companyId, cancellationToken);
            if (company is null || company.LogoUrl != ImageUpload.CompanyLogoUrl(companyId, fileName))
                return Results.NotFound();

            var image = ImageUpload.OpenRead(environment, "companies", companyId.ToString(), fileName);
            return image.Stream is null || image.ContentType is null
                ? Results.NotFound()
                : Results.File(image.Stream, image.ContentType);
        })
        .RequireAuthorization()
        .WithName("GetCompanyLogo");

        app.MapPost("/api/v1/companies/{companyId:long}/logo", async (
            long companyId,
            IFormFile image,
            AppDbContext db,
            IWebHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var company = await db.Companies.SingleOrDefaultAsync(
                item => item.Id == companyId, cancellationToken);
            if (company is null) return Results.NotFound(new { error = "Company was not found in this tenant." });

            var savedImage = await ImageUpload.SaveAsync(image, environment, "companies", company.Id.ToString(), cancellationToken);
            if (savedImage.Error is not null)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["image"] = [savedImage.Error] });

            var previousLogoUrl = company.LogoUrl;
            company.LogoUrl = savedImage.Url;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                ImageUpload.Delete(environment, savedImage.Url!);
                throw;
            }

            ImageUpload.Delete(environment, previousLogoUrl);
            return Results.Ok(new CompanyResponse(
                company.Id, company.Code, company.LegalName, company.FunctionalCurrencyCode,
                company.Phone, company.Email, company.Website, company.Address, company.CityRegion,
                company.PostalCode, company.Country, company.TaxId, company.LogoUrl));
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .DisableAntiforgery()
        .WithName("UploadCompanyLogo");

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
            ValidateOptionalLength(errors, "phone", request.Phone, 40);
            ValidateOptionalLength(errors, "email", request.Email, 320);
            ValidateOptionalLength(errors, "website", request.Website, 2000);
            ValidateOptionalLength(errors, "address", request.Address, 250);
            ValidateOptionalLength(errors, "cityRegion", request.CityRegion, 150);
            ValidateOptionalLength(errors, "postalCode", request.PostalCode, 30);
            ValidateOptionalLength(errors, "country", request.Country, 100);
            ValidateOptionalLength(errors, "taxId", request.TaxId, 100);
            if (!string.IsNullOrWhiteSpace(request.Email) &&
                (!System.Net.Mail.MailAddress.TryCreate(request.Email.Trim(), out var mailAddress) ||
                 !string.Equals(mailAddress.Address, request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
                errors["email"] = ["Enter a valid contact email address."];
            if (!string.IsNullOrWhiteSpace(request.Website) &&
                (!Uri.TryCreate(request.Website.Trim(), UriKind.Absolute, out var website) ||
                 website.Scheme is not ("http" or "https")))
                errors["website"] = ["Enter a valid website URL beginning with http:// or https://."];

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
            company.Phone = Clean(request.Phone);
            company.Email = Clean(request.Email);
            company.Website = Clean(request.Website);
            company.Address = Clean(request.Address);
            company.CityRegion = Clean(request.CityRegion);
            company.PostalCode = Clean(request.PostalCode);
            company.Country = Clean(request.Country);
            company.TaxId = Clean(request.TaxId);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new CompanyResponse(
                company.Id, company.Code, company.LegalName, company.FunctionalCurrencyCode,
                company.Phone, company.Email, company.Website, company.Address, company.CityRegion,
                company.PostalCode, company.Country, company.TaxId, company.LogoUrl));
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("UpdateCompany");

        return app;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateOptionalLength(
        IDictionary<string, string[]> errors,
        string field,
        string? value,
        int maxLength)
    {
        if (value?.Trim().Length > maxLength)
            errors[field] = [$"{field} must be at most {maxLength} characters."];
    }

    public sealed record UpdateCompanyRequest(
        string? LegalName,
        string? FunctionalCurrencyCode,
        string? Phone,
        string? Email,
        string? Website,
        string? Address,
        string? CityRegion,
        string? PostalCode,
        string? Country,
        string? TaxId);
    public sealed record CompanyResponse(
        long Id,
        string Code,
        string LegalName,
        string FunctionalCurrencyCode,
        string? Phone,
        string? Email,
        string? Website,
        string? Address,
        string? CityRegion,
        string? PostalCode,
        string? Country,
        string? TaxId,
        string? LogoUrl);
}
