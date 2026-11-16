using Gasoft.Application.Abstractions;
using Gasoft.Application.Taxes;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class TaxService(AppDbContext db, ICurrentTenant currentTenant) : ITaxService
{
    public async Task<IReadOnlyList<TaxJurisdictionDto>> GetJurisdictionsAsync(CancellationToken cancellationToken) =>
        await db.TaxJurisdictions.AsNoTracking()
            .OrderBy(item => item.CountryCode)
            .ThenBy(item => item.Code)
            .Select(item => new TaxJurisdictionDto(item.Id, item.Code, item.CountryCode))
            .ToListAsync(cancellationToken);

    public Task<PagedResultDto<TaxJurisdictionDto>> GetJurisdictionsPageAsync(
        int page, int pageSize, CancellationToken cancellationToken) =>
        PagedQuery.ToPageAsync(
            db.TaxJurisdictions.AsNoTracking()
                .OrderBy(item => item.CountryCode)
                .ThenBy(item => item.Code)
                .ThenBy(item => item.Id),
            page,
            pageSize,
            item => new TaxJurisdictionDto(item.Id, item.Code, item.CountryCode),
            cancellationToken);

    public async Task<IReadOnlyList<TaxCodeDto>?> GetCompanyTaxCodesAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue || !await CompanyExistsAsync(companyId, cancellationToken)) return null;
        return await GetTaxCodesAsync(tenantId.Value, companyId, cancellationToken);
    }

    public async Task<PagedResultDto<TaxCodeDto>?> GetCompanyTaxCodesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue || !await CompanyExistsAsync(companyId, cancellationToken)) return null;
        var codes = (
            from code in db.TaxCodes.AsNoTracking()
            join jurisdiction in db.TaxJurisdictions.AsNoTracking() on code.JurisdictionId equals jurisdiction.Id
            join category in db.TaxCategories.AsNoTracking() on code.CategoryId equals category.Id
            where (code.TenantId == null && code.CompanyId == null) ||
                  (code.TenantId == tenantId.Value && code.CompanyId == companyId)
            orderby jurisdiction.Code, code.Code, code.Id
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
            });
        var result = await PagedQuery.ToPageAsync(
            codes,
            page,
            pageSize,
            code => new
            {
                code.Id,
                code.JurisdictionId,
                code.JurisdictionCode,
                code.CountryCode,
                code.CategoryId,
                code.CategoryCode,
                code.CategoryName,
                code.Code,
                code.TaxType
            },
            cancellationToken);
        var codeIds = result.Items.Select(code => code.Id).ToArray();
        var rates = await db.TaxRates.AsNoTracking()
            .Where(rate => codeIds.Contains(rate.TaxCodeId))
            .OrderBy(rate => rate.ValidFrom)
            .Select(rate => new TaxRateDto(rate.Id, rate.TaxCodeId, rate.RatePercent, rate.ValidFrom, rate.ValidTo))
            .ToListAsync(cancellationToken);
        var items = result.Items.Select(code => new TaxCodeDto(
            code.Id,
            code.JurisdictionId,
            code.JurisdictionCode,
            code.CountryCode,
            code.CategoryId,
            code.CategoryCode,
            code.CategoryName,
            code.Code,
            code.TaxType,
            rates.Where(rate => rate.TaxCodeId == code.Id).ToArray())).ToArray();
        return new PagedResultDto<TaxCodeDto>(
            items, result.Page, result.PageSize, result.TotalCount, result.TotalPages);
    }

    public async Task<TaxCodeDto?> CreateTaxCodeAsync(
        long companyId,
        TaxCodeRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue || !await CompanyExistsAsync(companyId, cancellationToken)) return null;
        var validation = await ValidateTaxCodeAsync(request, cancellationToken);
        ThrowIfInvalid(validation.Errors);

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
            throw new TaxConflictException("A tax code with this code already exists in the jurisdiction.");
        }
        return (await GetTaxCodesAsync(tenantId.Value, companyId, cancellationToken))
            .Single(item => item.Id == code.Id);
    }

    public async Task<TaxCodeDto?> UpdateTaxCodeAsync(
        long companyId,
        long taxCodeId,
        TaxCodeRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var code = await GetEditableTaxCodeAsync(tenantId, companyId, taxCodeId, cancellationToken);
        if (code is null) return null;
        var validation = await ValidateTaxCodeAsync(request, cancellationToken);
        ThrowIfInvalid(validation.Errors);
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
            throw new TaxConflictException("A tax code with this code already exists in the jurisdiction.");
        }
        return (await GetTaxCodesAsync(tenantId, companyId, cancellationToken))
            .Single(item => item.Id == code.Id);
    }

    public async Task<bool> DeleteTaxCodeAsync(
        long companyId,
        long taxCodeId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var code = await GetEditableTaxCodeAsync(tenantId, companyId, taxCodeId, cancellationToken);
        if (code is null) return false;
        var isUsed = await db.SalesInvoiceLines.AnyAsync(line => line.TaxCodeId == taxCodeId, cancellationToken) ||
                     await db.PurchaseBillLines.AnyAsync(line => line.TaxCodeId == taxCodeId, cancellationToken);
        if (isUsed)
            throw new TaxConflictException("This tax code is used by an invoice or bill and cannot be deleted.");
        var rates = await db.TaxRates.Where(rate => rate.TaxCodeId == taxCodeId).ToListAsync(cancellationToken);
        db.TaxRates.RemoveRange(rates);
        db.TaxCodes.Remove(code);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TaxRateDto?> CreateTaxRateAsync(
        long companyId,
        long taxCodeId,
        TaxRateRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        if (await GetEditableTaxCodeAsync(tenantId, companyId, taxCodeId, cancellationToken) is null) return null;
        ThrowIfInvalid(ValidateTaxRate(request));
        if (await HasOverlappingRateAsync(taxCodeId, request, null, cancellationToken))
            throw new TaxConflictException("This rate period overlaps another rate for the tax code.");
        var rate = new TaxRate
        {
            TaxCodeId = taxCodeId,
            RatePercent = request.RatePercent,
            ValidFrom = request.ValidFrom,
            ValidTo = request.ValidTo
        };
        db.TaxRates.Add(rate);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(rate);
    }

    public async Task<TaxRateDto?> UpdateTaxRateAsync(
        long companyId,
        long taxCodeId,
        long rateId,
        TaxRateRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        if (await GetEditableTaxCodeAsync(tenantId, companyId, taxCodeId, cancellationToken) is null) return null;
        var rate = await db.TaxRates.SingleOrDefaultAsync(
            item => item.Id == rateId && item.TaxCodeId == taxCodeId,
            cancellationToken);
        if (rate is null) throw new TaxNotFoundException("Tax rate was not found.");
        ThrowIfInvalid(ValidateTaxRate(request));
        if (await HasOverlappingRateAsync(taxCodeId, request, rateId, cancellationToken))
            throw new TaxConflictException("This rate period overlaps another rate for the tax code.");
        rate.RatePercent = request.RatePercent;
        rate.ValidFrom = request.ValidFrom;
        rate.ValidTo = request.ValidTo;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(rate);
    }

    public async Task<bool> DeleteTaxRateAsync(
        long companyId,
        long taxCodeId,
        long rateId,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        if (await GetEditableTaxCodeAsync(tenantId, companyId, taxCodeId, cancellationToken) is null) return false;
        var rate = await db.TaxRates.SingleOrDefaultAsync(
            item => item.Id == rateId && item.TaxCodeId == taxCodeId,
            cancellationToken);
        if (rate is null) throw new TaxNotFoundException("Tax rate was not found.");
        db.TaxRates.Remove(rate);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TaxReferenceDto?> GetCompanyTaxReferenceAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue || !await CompanyExistsAsync(companyId, cancellationToken)) return null;
        var jurisdictions = await db.TaxJurisdictions.AsNoTracking()
            .OrderBy(item => item.Code)
            .Select(item => new TaxJurisdictionDto(item.Id, item.Code, item.CountryCode))
            .ToListAsync(cancellationToken);
        var categories = await db.TaxCategories.AsNoTracking()
            .OrderBy(item => item.Code)
            .Select(item => new TaxCategoryDto(item.Id, item.Code, item.Name))
            .ToListAsync(cancellationToken);
        var codes = await GetTaxCodesAsync(tenantId.Value, companyId, cancellationToken);
        return new TaxReferenceDto(jurisdictions, categories, codes);
    }

    public async Task<IReadOnlyList<TaxRegistrationDto>> GetRegistrationsAsync(
        long companyId,
        CancellationToken cancellationToken) =>
        await db.TenantTaxRegistrations.AsNoTracking()
            .Where(item => item.CompanyId == companyId)
            .Join(db.TaxJurisdictions.AsNoTracking(),
                registration => registration.JurisdictionId,
                jurisdiction => jurisdiction.Id,
                (registration, jurisdiction) => new TaxRegistrationDto(
                    registration.Id,
                    registration.CompanyId,
                    registration.JurisdictionId,
                    jurisdiction.Code,
                    jurisdiction.CountryCode,
                    registration.RegistrationNumber))
            .OrderBy(item => item.Code)
            .ToListAsync(cancellationToken);

    public Task<PagedResultDto<TaxRegistrationDto>> GetRegistrationsPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var registrations = db.TenantTaxRegistrations.AsNoTracking()
            .Where(item => item.CompanyId == companyId)
            .Join(db.TaxJurisdictions.AsNoTracking(),
                registration => registration.JurisdictionId,
                jurisdiction => jurisdiction.Id,
                (registration, jurisdiction) => new
                {
                    registration.Id,
                    registration.CompanyId,
                    registration.JurisdictionId,
                    JurisdictionCode = jurisdiction.Code,
                    jurisdiction.CountryCode,
                    registration.RegistrationNumber
                })
            .OrderBy(item => item.JurisdictionCode)
            .ThenBy(item => item.Id);
        return PagedQuery.ToPageAsync(
            registrations,
            page,
            pageSize,
            item => new TaxRegistrationDto(
                item.Id,
                item.CompanyId,
                item.JurisdictionId,
                item.JurisdictionCode,
                item.CountryCode,
                item.RegistrationNumber),
            cancellationToken);
    }

    public async Task<TaxRegistrationDto?> GetRegistrationAsync(
        long companyId,
        long registrationId,
        CancellationToken cancellationToken) =>
        await db.TenantTaxRegistrations.AsNoTracking()
            .Where(item => item.CompanyId == companyId && item.Id == registrationId)
            .Join(db.TaxJurisdictions.AsNoTracking(),
                item => item.JurisdictionId,
                jurisdiction => jurisdiction.Id,
                (item, jurisdiction) => new TaxRegistrationDto(
                    item.Id,
                    item.CompanyId,
                    item.JurisdictionId,
                    jurisdiction.Code,
                    jurisdiction.CountryCode,
                    item.RegistrationNumber))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<CreatedTaxRegistrationDto?> CreateRegistrationAsync(
        long companyId,
        CreateTaxRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (!tenantId.HasValue || !await CompanyExistsAsync(companyId, cancellationToken)) return null;
        var registrationNumber = ValidateRegistrationNumber(request.RegistrationNumber);
        if (!await db.TaxJurisdictions.AnyAsync(
                item => item.Id == request.JurisdictionId, cancellationToken))
            throw Validation("jurisdictionId", "Choose a configured tax jurisdiction.");
        if (await db.TenantTaxRegistrations.AnyAsync(
                item => item.CompanyId == companyId && item.JurisdictionId == request.JurisdictionId,
                cancellationToken))
            throw new TaxConflictException("This company is already registered in that jurisdiction.");
        var registration = new TenantTaxRegistration
        {
            TenantId = tenantId.Value,
            CompanyId = companyId,
            JurisdictionId = request.JurisdictionId,
            RegistrationNumber = registrationNumber
        };
        db.TenantTaxRegistrations.Add(registration);
        await db.SaveChangesAsync(cancellationToken);
        return new CreatedTaxRegistrationDto(
            registration.Id, registration.CompanyId, registration.JurisdictionId, registration.RegistrationNumber);
    }

    public async Task<TaxRegistrationDto?> UpdateRegistrationAsync(
        long companyId,
        long registrationId,
        CreateTaxRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var registration = await db.TenantTaxRegistrations.SingleOrDefaultAsync(
            item => item.CompanyId == companyId && item.Id == registrationId,
            cancellationToken);
        if (registration is null) return null;
        var registrationNumber = ValidateRegistrationNumber(request.RegistrationNumber);
        if (!await db.TaxJurisdictions.AnyAsync(item => item.Id == request.JurisdictionId, cancellationToken))
            throw Validation("jurisdictionId", "Choose a configured tax jurisdiction.");
        if (await db.TenantTaxRegistrations.AnyAsync(
                item => item.CompanyId == companyId &&
                        item.Id != registrationId &&
                        item.JurisdictionId == request.JurisdictionId,
                cancellationToken))
            throw new TaxConflictException("This company is already registered in that jurisdiction.");
        registration.JurisdictionId = request.JurisdictionId;
        registration.RegistrationNumber = registrationNumber;
        await db.SaveChangesAsync(cancellationToken);
        var jurisdiction = await db.TaxJurisdictions.AsNoTracking()
            .SingleAsync(item => item.Id == registration.JurisdictionId, cancellationToken);
        return new TaxRegistrationDto(
            registration.Id,
            registration.CompanyId,
            registration.JurisdictionId,
            jurisdiction.Code,
            jurisdiction.CountryCode,
            registration.RegistrationNumber);
    }

    private async Task<IReadOnlyList<TaxCodeDto>> GetTaxCodesAsync(
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
            }).ToListAsync(cancellationToken);
        var codeIds = codes.Select(code => code.Id).ToArray();
        var rates = await db.TaxRates.AsNoTracking()
            .Where(rate => codeIds.Contains(rate.TaxCodeId))
            .OrderBy(rate => rate.ValidFrom)
            .Select(rate => new TaxRateDto(rate.Id, rate.TaxCodeId, rate.RatePercent, rate.ValidFrom, rate.ValidTo))
            .ToListAsync(cancellationToken);
        return codes.Select(code => new TaxCodeDto(
            code.Id,
            code.JurisdictionId,
            code.JurisdictionCode,
            code.CountryCode,
            code.CategoryId,
            code.CategoryCode,
            code.CategoryName,
            code.Code,
            code.TaxType,
            rates.Where(rate => rate.TaxCodeId == code.Id).ToArray())).ToArray();
    }

    private Task<TaxCode?> GetEditableTaxCodeAsync(
        long tenantId,
        long companyId,
        long taxCodeId,
        CancellationToken cancellationToken) =>
        db.TaxCodes.SingleOrDefaultAsync(
            code => code.Id == taxCodeId &&
                    code.TenantId == tenantId &&
                    code.CompanyId == companyId,
            cancellationToken);

    private async Task<(string Code, string TaxType, Dictionary<string, string[]> Errors)> ValidateTaxCodeAsync(
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

    private Task<bool> HasOverlappingRateAsync(
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

    private Task<bool> CompanyExistsAsync(long companyId, CancellationToken cancellationToken) =>
        db.Companies.AnyAsync(item => item.Id == companyId, cancellationToken);

    private long EnsureTenant() =>
        currentTenant.TenantId ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");

    private static string ValidateRegistrationNumber(string? value)
    {
        var registrationNumber = value?.Trim() ?? string.Empty;
        if (registrationNumber.Length is 0 or > 100)
            throw Validation("registrationNumber", "Registration number is required and must be at most 100 characters.");
        return registrationNumber;
    }

    private static TaxRateDto ToDto(TaxRate rate) =>
        new(rate.Id, rate.TaxCodeId, rate.RatePercent, rate.ValidFrom, rate.ValidTo);

    private static void ThrowIfInvalid(Dictionary<string, string[]> errors)
    {
        if (errors.Count != 0) throw new TaxValidationException(errors);
    }

    private static TaxValidationException Validation(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });

    private static bool IsUniqueConstraintViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
