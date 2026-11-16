using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Api.Endpoints;

public static class TaxEndpoints
{
    public static IEndpointRouteBuilder MapTaxEndpoints(this IEndpointRouteBuilder app)
    {
        var jurisdictions = app.MapGroup("/api/v1/tax-jurisdictions")
            .RequireAuthorization();

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

        var companyTaxCodes = app.MapGroup("/api/v1/companies/{companyId:long}/tax-codes")
            .RequireAuthorization();

        companyTaxCodes.MapGet("", async (
            long companyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue ||
                !await db.Companies.AnyAsync(item => item.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this tenant." });

            var codes = await GetCompanyTaxCodesAsync(db, tenantId.Value, companyId, cancellationToken);
            return Results.Ok(codes);
        })
        .WithName("GetCompanyTaxCodes");

        companyTaxCodes.MapPost("", async (
            long companyId,
            TaxCodeRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue ||
                !await db.Companies.AnyAsync(item => item.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this tenant." });

            var validation = await ValidateTaxCodeAsync(db, request, cancellationToken);
            if (validation.Errors.Count != 0) return Results.ValidationProblem(validation.Errors);

            var code = new TaxCode
            {
                TenantId = tenantId.Value,
                CompanyId = companyId,
                JurisdictionId = request.JurisdictionId,
                CategoryId = request.CategoryId,
                Code = validation.Code,
                TaxType = validation.TaxType
            };
            db.TaxCodes.Add(code);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                return Results.Conflict(new { error = "A tax code with this code already exists in the jurisdiction." });
            }

            var result = await GetCompanyTaxCodeAsync(db, tenantId.Value, companyId, code.Id, cancellationToken);
            return Results.Created($"/api/v1/companies/{companyId}/tax-codes/{code.Id}", result);
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("CreateCompanyTaxCode");

        companyTaxCodes.MapPut("/{taxCodeId:long}", async (
            long companyId,
            long taxCodeId,
            TaxCodeRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue) return Results.Unauthorized();
            var code = await GetEditableTaxCodeAsync(db, tenantId.Value, companyId, taxCodeId, cancellationToken);
            if (code is null) return Results.NotFound(new { error = "Tax code was not found in this company." });

            var validation = await ValidateTaxCodeAsync(db, request, cancellationToken);
            if (validation.Errors.Count != 0) return Results.ValidationProblem(validation.Errors);

            code.JurisdictionId = request.JurisdictionId;
            code.CategoryId = request.CategoryId;
            code.Code = validation.Code;
            code.TaxType = validation.TaxType;
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
            {
                return Results.Conflict(new { error = "A tax code with this code already exists in the jurisdiction." });
            }

            return Results.Ok(await GetCompanyTaxCodeAsync(db, tenantId.Value, companyId, code.Id, cancellationToken));
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("UpdateCompanyTaxCode");

        companyTaxCodes.MapDelete("/{taxCodeId:long}", async (
            long companyId,
            long taxCodeId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue) return Results.Unauthorized();
            var code = await GetEditableTaxCodeAsync(db, tenantId.Value, companyId, taxCodeId, cancellationToken);
            if (code is null) return Results.NotFound(new { error = "Tax code was not found in this company." });

            var isUsed = await db.SalesInvoiceLines.AnyAsync(line => line.TaxCodeId == taxCodeId, cancellationToken) ||
                         await db.PurchaseBillLines.AnyAsync(line => line.TaxCodeId == taxCodeId, cancellationToken);
            if (isUsed)
                return Results.Conflict(new { error = "This tax code is used by an invoice or bill and cannot be deleted." });

            var rates = await db.TaxRates.Where(rate => rate.TaxCodeId == taxCodeId).ToListAsync(cancellationToken);
            db.TaxRates.RemoveRange(rates);
            db.TaxCodes.Remove(code);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("DeleteCompanyTaxCode");

        companyTaxCodes.MapPost("/{taxCodeId:long}/rates", async (
            long companyId,
            long taxCodeId,
            TaxRateRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue) return Results.Unauthorized();
            var code = await GetEditableTaxCodeAsync(db, tenantId.Value, companyId, taxCodeId, cancellationToken);
            if (code is null) return Results.NotFound(new { error = "Tax code was not found in this company." });

            var errors = ValidateTaxRate(request);
            if (errors.Count != 0) return Results.ValidationProblem(errors);
            if (await HasOverlappingRateAsync(db, taxCodeId, request, null, cancellationToken))
                return Results.Conflict(new { error = "This rate period overlaps another rate for the tax code." });

            var rate = new TaxRate
            {
                TaxCodeId = taxCodeId,
                RatePercent = request.RatePercent,
                ValidFrom = request.ValidFrom,
                ValidTo = request.ValidTo
            };
            db.TaxRates.Add(rate);
            await db.SaveChangesAsync(cancellationToken);
            return Results.Created(
                $"/api/v1/companies/{companyId}/tax-codes/{taxCodeId}/rates/{rate.Id}",
                ToTaxRateResponse(rate));
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("CreateCompanyTaxRate");

        companyTaxCodes.MapPut("/{taxCodeId:long}/rates/{rateId:long}", async (
            long companyId,
            long taxCodeId,
            long rateId,
            TaxRateRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue) return Results.Unauthorized();
            var code = await GetEditableTaxCodeAsync(db, tenantId.Value, companyId, taxCodeId, cancellationToken);
            if (code is null) return Results.NotFound(new { error = "Tax code was not found in this company." });
            var rate = await db.TaxRates.SingleOrDefaultAsync(
                item => item.Id == rateId && item.TaxCodeId == taxCodeId, cancellationToken);
            if (rate is null) return Results.NotFound(new { error = "Tax rate was not found." });

            var errors = ValidateTaxRate(request);
            if (errors.Count != 0) return Results.ValidationProblem(errors);
            if (await HasOverlappingRateAsync(db, taxCodeId, request, rateId, cancellationToken))
                return Results.Conflict(new { error = "This rate period overlaps another rate for the tax code." });

            rate.RatePercent = request.RatePercent;
            rate.ValidFrom = request.ValidFrom;
            rate.ValidTo = request.ValidTo;
            await db.SaveChangesAsync(cancellationToken);
            return Results.Ok(ToTaxRateResponse(rate));
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("UpdateCompanyTaxRate");

        companyTaxCodes.MapDelete("/{taxCodeId:long}/rates/{rateId:long}", async (
            long companyId,
            long taxCodeId,
            long rateId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue) return Results.Unauthorized();
            var code = await GetEditableTaxCodeAsync(db, tenantId.Value, companyId, taxCodeId, cancellationToken);
            if (code is null) return Results.NotFound(new { error = "Tax code was not found in this company." });
            var rate = await db.TaxRates.SingleOrDefaultAsync(
                item => item.Id == rateId && item.TaxCodeId == taxCodeId, cancellationToken);
            if (rate is null) return Results.NotFound(new { error = "Tax rate was not found." });

            db.TaxRates.Remove(rate);
            await db.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole("Owner"))
        .WithName("DeleteCompanyTaxRate");

        app.MapGet("/api/v1/companies/{companyId:long}/tax-reference", async (
            long companyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tenantId = db.CurrentTenantId;
            if (!tenantId.HasValue ||
                !await db.Companies.AnyAsync(item => item.Id == companyId, cancellationToken))
                return Results.NotFound(new { error = "Company was not found in this tenant." });

            var jurisdictions = await db.TaxJurisdictions.AsNoTracking()
                .OrderBy(item => item.Code)
                .Select(item => new { item.Id, item.Code, item.CountryCode })
                .ToListAsync(cancellationToken);
            var categories = await db.TaxCategories.AsNoTracking()
                .OrderBy(item => item.Code)
                .Select(item => new { item.Id, item.Code, item.Name })
                .ToListAsync(cancellationToken);
            var codes = await GetCompanyTaxCodesAsync(db, tenantId.Value, companyId, cancellationToken);

            return Results.Ok(new { jurisdictions, categories, codes });
        })
        .WithName("GetCompanyTaxReference");

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

    private static async Task<IReadOnlyList<TaxCodeResponse>> GetCompanyTaxCodesAsync(
        AppDbContext db,
        long tenantId,
        long companyId,
        CancellationToken cancellationToken)
    {
        var codes = await (
            from code in db.TaxCodes.AsNoTracking()
            join jurisdiction in db.TaxJurisdictions.AsNoTracking() on code.JurisdictionId equals jurisdiction.Id
            join category in db.TaxCategories.AsNoTracking() on code.CategoryId equals category.Id
            where (code.TenantId == null && code.CompanyId == null) ||
                  (code.TenantId == tenantId && code.CompanyId == companyId)
            orderby jurisdiction.Code, code.Code
            select new
            {
                code.Id,
                code.JurisdictionId,
                JurisdictionCode = jurisdiction.Code,
                CountryCode = jurisdiction.CountryCode,
                code.CategoryId,
                CategoryCode = category.Code,
                CategoryName = category.Name,
                code.Code,
                code.TaxType
            })
            .ToListAsync(cancellationToken);

        var codeIds = codes.Select(code => code.Id).ToArray();
        var rates = await db.TaxRates.AsNoTracking()
            .Where(rate => codeIds.Contains(rate.TaxCodeId))
            .OrderBy(rate => rate.ValidFrom)
            .Select(rate => new TaxRateResponse(
                rate.Id,
                rate.TaxCodeId,
                rate.RatePercent,
                rate.ValidFrom,
                rate.ValidTo))
            .ToListAsync(cancellationToken);

        return codes.Select(code => new TaxCodeResponse(
            code.Id,
            code.JurisdictionId,
            code.JurisdictionCode,
            code.CountryCode,
            code.CategoryId,
            code.CategoryCode,
            code.CategoryName,
            code.Code,
            code.TaxType,
            rates.Where(rate => rate.TaxCodeId == code.Id).ToArray()))
            .ToArray();
    }

    private static async Task<TaxCodeResponse> GetCompanyTaxCodeAsync(
        AppDbContext db,
        long tenantId,
        long companyId,
        long taxCodeId,
        CancellationToken cancellationToken) =>
        (await GetCompanyTaxCodesAsync(db, tenantId, companyId, cancellationToken))
        .Single(code => code.Id == taxCodeId);

    private static Task<TaxCode?> GetEditableTaxCodeAsync(
        AppDbContext db,
        long tenantId,
        long companyId,
        long taxCodeId,
        CancellationToken cancellationToken) =>
        db.TaxCodes.SingleOrDefaultAsync(
            code => code.Id == taxCodeId &&
                    code.TenantId == tenantId &&
                    code.CompanyId == companyId,
            cancellationToken);

    private static async Task<(string Code, string TaxType, Dictionary<string, string[]> Errors)> ValidateTaxCodeAsync(
        AppDbContext db,
        TaxCodeRequest request,
        CancellationToken cancellationToken)
    {
        var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var taxType = request.TaxType?.Trim() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();
        if (code.Length is 0 or > 40)
            errors["code"] = ["Tax code is required and must be at most 40 characters."];
        if (taxType.Length is 0 or > 40)
            errors["taxType"] = ["Tax type is required and must be at most 40 characters."];
        if (!await db.TaxJurisdictions.AnyAsync(item => item.Id == request.JurisdictionId, cancellationToken))
            errors["jurisdictionId"] = ["Choose a supported tax jurisdiction."];
        if (!await db.TaxCategories.AnyAsync(item => item.Id == request.CategoryId, cancellationToken))
            errors["categoryId"] = ["Choose a supported tax category."];
        return (code, taxType, errors);
    }

    private static Dictionary<string, string[]> ValidateTaxRate(TaxRateRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.RatePercent is < 0 or > 100 ||
            decimal.Round(request.RatePercent, 6) != request.RatePercent)
            errors["ratePercent"] = ["Enter a rate between 0 and 100 with at most 6 decimal places."];
        if (request.ValidFrom == default)
            errors["validFrom"] = ["A valid start date is required."];
        if (request.ValidTo.HasValue && request.ValidTo.Value < request.ValidFrom)
            errors["validTo"] = ["The end date must be on or after the start date."];
        return errors;
    }

    private static Task<bool> HasOverlappingRateAsync(
        AppDbContext db,
        long taxCodeId,
        TaxRateRequest request,
        long? exceptRateId,
        CancellationToken cancellationToken) =>
        db.TaxRates.AnyAsync(rate =>
            rate.TaxCodeId == taxCodeId &&
            (!exceptRateId.HasValue || rate.Id != exceptRateId.Value) &&
            rate.ValidFrom <= (request.ValidTo ?? DateOnly.MaxValue) &&
            (!rate.ValidTo.HasValue || rate.ValidTo.Value >= request.ValidFrom),
            cancellationToken);

    private static TaxRateResponse ToTaxRateResponse(TaxRate rate) =>
        new(rate.Id, rate.TaxCodeId, rate.RatePercent, rate.ValidFrom, rate.ValidTo);

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    public sealed record CreateTaxRegistrationRequest(long JurisdictionId, string? RegistrationNumber);
    public sealed record TaxJurisdictionResponse(long Id, string Code, string CountryCode);
    public sealed record TaxCodeRequest(long JurisdictionId, long CategoryId, string? Code, string? TaxType);
    public sealed record TaxRateRequest(decimal RatePercent, DateOnly ValidFrom, DateOnly? ValidTo);
    public sealed record TaxRateResponse(
        long Id,
        long TaxCodeId,
        decimal RatePercent,
        DateOnly ValidFrom,
        DateOnly? ValidTo);
    public sealed record TaxCodeResponse(
        long Id,
        long JurisdictionId,
        string JurisdictionCode,
        string CountryCode,
        long CategoryId,
        string CategoryCode,
        string CategoryName,
        string Code,
        string TaxType,
        IReadOnlyList<TaxRateResponse> Rates);
}
