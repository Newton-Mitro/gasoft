using System.Net.Mail;
using System.Text.RegularExpressions;
using Gasoft.Application.Abstractions;
using Gasoft.Application.Companies;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class CompanyAdministrationService(
    AppDbContext db,
    ICurrentTenant currentTenant) : ICompanyAdministrationService
{
    private static readonly Regex CompanyCodePattern =
        new("^[A-Z0-9][A-Z0-9_-]{1,29}$", RegexOptions.CultureInvariant);

    public async Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(CancellationToken cancellationToken) =>
        await db.Companies.AsNoTracking()
            .OrderBy(company => company.LegalName)
            .ThenBy(company => company.Code)
            .Select(company => new CompanyDto(
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

    public async Task<CompanyDto?> GetCompanyAsync(long companyId, CancellationToken cancellationToken)
    {
        var company = await db.Companies.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
        return company is null ? null : ToDto(company);
    }

    public async Task<CompanyDto?> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = currentTenant.TenantId;
        if (tenantId is null) return null;

        var code = request.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        var legalName = request.LegalName?.Trim() ?? string.Empty;
        var currencyCode = request.FunctionalCurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();
        if (!CompanyCodePattern.IsMatch(code))
            errors["code"] = ["Use 2–30 letters, numbers, hyphens, or underscores."];
        if (legalName.Length is 0 or > 250)
            errors["legalName"] = ["Legal name is required and must be at most 250 characters."];
        if (currencyCode.Length != 3 ||
            !await db.Currencies.AnyAsync(item => item.Code == currencyCode, cancellationToken))
            errors["functionalCurrencyCode"] = ["Choose a supported three-letter currency."];
        if (errors.Count != 0) throw new CompanyValidationException(errors);
        if (await db.Companies.AnyAsync(item => item.Code == code, cancellationToken))
            throw CompanyCodeConflict(code);

        var company = new Company
        {
            TenantId = tenantId.Value,
            Code = code,
            LegalName = legalName,
            FunctionalCurrencyCode = currencyCode
        };
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.Companies.Add(company);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await CompanySetupSeeder.SeedLedgerDefaultsAsync(
                db, tenantId.Value, company, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsCompanyCodeConflict(exception))
        {
            throw CompanyCodeConflict(code);
        }
        return ToDto(company);
    }

    public async Task<CompanyDto?> SetCompanyLogoUrlAsync(
        long companyId,
        string logoUrl,
        CancellationToken cancellationToken)
    {
        var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
        if (company is null) return null;
        company.LogoUrl = logoUrl;
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(company);
    }

    public async Task<CompanyDto?> UpdateCompanyAsync(
        long companyId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken)
    {
        var company = await db.Companies.SingleOrDefaultAsync(item => item.Id == companyId, cancellationToken);
        if (company is null) return null;

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
            (!MailAddress.TryCreate(request.Email.Trim(), out var mailAddress) ||
             !string.Equals(mailAddress.Address, request.Email.Trim(), StringComparison.OrdinalIgnoreCase)))
            errors["email"] = ["Enter a valid contact email address."];
        if (!string.IsNullOrWhiteSpace(request.Website) &&
            (!Uri.TryCreate(request.Website.Trim(), UriKind.Absolute, out var website) ||
             website.Scheme is not ("http" or "https")))
            errors["website"] = ["Enter a valid website URL beginning with http:// or https://."];
        if (errors.Count != 0) throw new CompanyValidationException(errors);
        if (currencyCode != company.FunctionalCurrencyCode &&
            await db.JournalEntries.AnyAsync(entry => entry.CompanyId == companyId, cancellationToken))
            throw new CompanyConflictException(
                "Functional currency cannot be changed after journal entries have been created.");

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
        return ToDto(company);
    }

    private static CompanyDto ToDto(Company company) => new(
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
        company.LogoUrl);

    private static CompanyConflictException CompanyCodeConflict(string code) =>
        new($"Company code '{code}' is already in use in this workspace.");

    private static bool IsCompanyCodeConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_Companies_TenantId_Code"
        };

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
}
