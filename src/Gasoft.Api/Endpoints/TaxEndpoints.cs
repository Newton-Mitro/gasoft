using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Endpoints;

public static class TaxEndpoints
{
    public static IEndpointRouteBuilder MapTaxEndpoints(this IEndpointRouteBuilder app)
    {
        var jurisdictions = app.MapGroup("/api/v1/tax-jurisdictions")
            .RequireAuthorization(policy => policy.RequireRole("Owner"));

        jurisdictions.MapGet("", async (
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var rows = await db.TaxJurisdictions.AsNoTracking()
                .OrderBy(item => item.CountryCode)
                .ThenBy(item => item.Code)
                .Select(item => new TaxJurisdictionResponse(item.Id, item.Code, item.CountryCode))
                .ToListAsync(cancellationToken);
            return Results.Ok(rows);
        })
        .WithName("GetTaxJurisdictions");

        jurisdictions.MapGet("/{jurisdictionId:long}", async (
            long jurisdictionId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var row = await db.TaxJurisdictions.AsNoTracking()
                .Where(item => item.Id == jurisdictionId)
                .Select(item => new TaxJurisdictionResponse(item.Id, item.Code, item.CountryCode))
                .SingleOrDefaultAsync(cancellationToken);
            return row is null ? Results.NotFound() : Results.Ok(row);
        })
        .WithName("GetTaxJurisdiction");

        jurisdictions.MapPost("", async (
            TaxJurisdictionRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            var countryCode = request.CountryCode?.Trim().ToUpperInvariant() ?? string.Empty;
            var errors = ValidateTaxJurisdiction(code, countryCode);
            if (errors.Count != 0) return Results.ValidationProblem(errors);
            if (await db.TaxJurisdictions.AnyAsync(item => item.Code == code, cancellationToken))
                return Results.Conflict(new { error = "A tax jurisdiction with this code already exists." });

            var jurisdiction = new TaxJurisdiction { Code = code, CountryCode = countryCode };
            db.TaxJurisdictions.Add(jurisdiction);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created(
                $"/api/v1/tax-jurisdictions/{jurisdiction.Id}",
                new TaxJurisdictionResponse(jurisdiction.Id, jurisdiction.Code, jurisdiction.CountryCode));
        })
        .WithName("CreateTaxJurisdiction");

        jurisdictions.MapPut("/{jurisdictionId:long}", async (
            long jurisdictionId,
            TaxJurisdictionRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var jurisdiction = await db.TaxJurisdictions.SingleOrDefaultAsync(
                item => item.Id == jurisdictionId, cancellationToken);
            if (jurisdiction is null) return Results.NotFound();

            var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            var countryCode = request.CountryCode?.Trim().ToUpperInvariant() ?? string.Empty;
            var errors = ValidateTaxJurisdiction(code, countryCode);
            if (errors.Count != 0) return Results.ValidationProblem(errors);
            if (await db.TaxJurisdictions.AnyAsync(
                    item => item.Id != jurisdictionId && item.Code == code, cancellationToken))
                return Results.Conflict(new { error = "A tax jurisdiction with this code already exists." });

            jurisdiction.Code = code;
            jurisdiction.CountryCode = countryCode;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(new TaxJurisdictionResponse(
                jurisdiction.Id, jurisdiction.Code, jurisdiction.CountryCode));
        })
        .WithName("UpdateTaxJurisdiction");

        jurisdictions.MapDelete("/{jurisdictionId:long}", async (
            long jurisdictionId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var jurisdiction = await db.TaxJurisdictions.SingleOrDefaultAsync(
                item => item.Id == jurisdictionId, cancellationToken);
            if (jurisdiction is null) return Results.NotFound();

            var hasTaxCodes = await db.TaxCodes.AnyAsync(
                item => item.JurisdictionId == jurisdictionId, cancellationToken);
            var hasRegistrations = await db.TenantTaxRegistrations.IgnoreQueryFilters().AnyAsync(
                item => item.JurisdictionId == jurisdictionId, cancellationToken);
            if (hasTaxCodes || hasRegistrations)
                return Results.Conflict(new
                {
                    error = "This jurisdiction is in use by tax codes or company registrations and cannot be deleted."
                });

            db.TaxJurisdictions.Remove(jurisdiction);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .WithName("DeleteTaxJurisdiction");

        app.MapGet("/api/v1/tax-reference", async (
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var jurisdictions = await db.TaxJurisdictions.AsNoTracking()
                .OrderBy(item => item.Code)
                .Select(item => new { item.Id, item.Code, item.CountryCode })
                .ToListAsync(cancellationToken);
            var categories = await db.TaxCategories.AsNoTracking()
                .OrderBy(item => item.Code)
                .Select(item => new { item.Id, item.Code, item.Name })
                .ToListAsync(cancellationToken);
            var codes = await db.TaxCodes.AsNoTracking()
                .OrderBy(item => item.Code)
                .Select(item => new
                {
                    item.Id,
                    item.JurisdictionId,
                    item.CategoryId,
                    item.Code,
                    item.TaxType
                })
                .ToListAsync(cancellationToken);
            var rates = await db.TaxRates.AsNoTracking()
                .OrderBy(rate => rate.ValidFrom)
                .Select(rate => new { rate.Id, rate.TaxCodeId, rate.RatePercent, rate.ValidFrom, rate.ValidTo })
                .ToListAsync(cancellationToken);

            return Results.Ok(new
            {
                jurisdictions,
                categories,
                codes = codes.Select(code => new
                {
                    code.Id,
                    code.JurisdictionId,
                    code.CategoryId,
                    code.Code,
                    code.TaxType,
                    rates = rates.Where(rate => rate.TaxCodeId == code.Id)
                        .Select(rate => new { rate.Id, rate.RatePercent, rate.ValidFrom, rate.ValidTo })
                        .ToArray()
                }).ToArray()
            });
        })
        .RequireAuthorization()
        .WithName("GetTaxReference");

        var companies = app.MapGroup("/api/v1/companies/{companyId:long}/tax-registrations")
            .RequireAuthorization();

        companies.MapGet("", async (
            long companyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var registrations = await db.TenantTaxRegistrations.AsNoTracking()
                .Where(item => item.CompanyId == companyId)
                .Join(db.TaxJurisdictions.AsNoTracking(),
                    registration => registration.JurisdictionId,
                    jurisdiction => jurisdiction.Id,
                    (registration, jurisdiction) => new
                    {
                        registration.Id,
                        registration.CompanyId,
                        registration.JurisdictionId,
                        jurisdiction.Code,
                        jurisdiction.CountryCode,
                        registration.RegistrationNumber
                    })
                .OrderBy(item => item.Code)
                .ToListAsync(cancellationToken);
            return Results.Ok(registrations);
        });

        companies.MapGet("/{registrationId:long}", async (
            long companyId,
            long registrationId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var registration = await db.TenantTaxRegistrations.AsNoTracking()
                .Where(item => item.CompanyId == companyId && item.Id == registrationId)
                .Join(db.TaxJurisdictions.AsNoTracking(),
                    item => item.JurisdictionId,
                    jurisdiction => jurisdiction.Id,
                    (item, jurisdiction) => new
                    {
                        item.Id,
                        item.CompanyId,
                        item.JurisdictionId,
                        jurisdiction.Code,
                        jurisdiction.CountryCode,
                        item.RegistrationNumber
                    })
                .SingleOrDefaultAsync(cancellationToken);
            return registration is null ? Results.NotFound() : Results.Ok(registration);
        });

        companies.MapPost("", async (
            long companyId,
            CreateTaxRegistrationRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue ||
                !await db.Companies.AnyAsync(item => item.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this tenant." });

            var registrationNumber = request.RegistrationNumber?.Trim() ?? string.Empty;
            if (registrationNumber.Length is 0 or > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["registrationNumber"] = ["Registration number is required and must be at most 100 characters."]
                });
            if (!await db.TaxJurisdictions.AnyAsync(
                    item => item.Id == request.JurisdictionId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["jurisdictionId"] = ["Choose a configured tax jurisdiction."]
                });
            if (await db.TenantTaxRegistrations.AnyAsync(
                    item => item.CompanyId == companyId && item.JurisdictionId == request.JurisdictionId,
                    cancellationToken))
                return Results.Conflict(new { error = "This company is already registered in that jurisdiction." });

            var registration = new TenantTaxRegistration
            {
                TenantId = tenantId.Value,
                CompanyId = companyId,
                JurisdictionId = request.JurisdictionId,
                RegistrationNumber = registrationNumber
            };
            db.TenantTaxRegistrations.Add(registration);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created(
                $"/api/v1/companies/{companyId}/tax-registrations/{registration.Id}",
                new { registration.Id, registration.CompanyId, registration.JurisdictionId, registration.RegistrationNumber });
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"));

        companies.MapPut("/{registrationId:long}", async (
            long companyId,
            long registrationId,
            CreateTaxRegistrationRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var registration = await db.TenantTaxRegistrations.SingleOrDefaultAsync(
                item => item.CompanyId == companyId && item.Id == registrationId,
                cancellationToken);
            if (registration is null) return Results.NotFound();

            var registrationNumber = request.RegistrationNumber?.Trim() ?? string.Empty;
            if (registrationNumber.Length is 0 or > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["registrationNumber"] = ["Registration number is required and must be at most 100 characters."]
                });
            if (!await db.TaxJurisdictions.AnyAsync(item => item.Id == request.JurisdictionId, cancellationToken))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["jurisdictionId"] = ["Choose a configured tax jurisdiction."]
                });
            if (await db.TenantTaxRegistrations.AnyAsync(
                    item => item.CompanyId == companyId && item.Id != registrationId &&
                            item.JurisdictionId == request.JurisdictionId,
                    cancellationToken))
                return Results.Conflict(new { error = "This company is already registered in that jurisdiction." });

            registration.JurisdictionId = request.JurisdictionId;
            registration.RegistrationNumber = registrationNumber;
            await db.SaveChangesAsync(cancellationToken);
            var response = await db.TaxJurisdictions.AsNoTracking()
                .Where(item => item.Id == registration.JurisdictionId)
                .Select(item => new
                {
                    registration.Id,
                    registration.CompanyId,
                    registration.JurisdictionId,
                    item.Code,
                    item.CountryCode,
                    registration.RegistrationNumber
                })
                .SingleAsync(cancellationToken);
            return Results.Ok(response);
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"));

        return app;
    }

    public sealed record CreateTaxRegistrationRequest(long JurisdictionId, string? RegistrationNumber);
    public sealed record TaxJurisdictionRequest(string? Code, string? CountryCode);
    public sealed record TaxJurisdictionResponse(long Id, string Code, string CountryCode);

    private static Dictionary<string, string[]> ValidateTaxJurisdiction(string code, string countryCode)
    {
        var errors = new Dictionary<string, string[]>();
        if (code.Length is 0 or > 40)
            errors["code"] = ["Jurisdiction code is required and must be at most 40 characters."];
        if (countryCode.Length != 2 || !countryCode.All(char.IsAsciiLetter))
            errors["countryCode"] = ["Enter a two-letter country code."];
        return errors;
    }
}
