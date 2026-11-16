namespace Gasoft.Application.Companies;

public sealed record CreateCompanyRequest(string? Code, string? LegalName, string? FunctionalCurrencyCode);

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

public sealed record CompanyDto(
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

public sealed record CompanySetupResultDto(
    int AccountGroupsAdded,
    int AccountsAdded,
    int JournalsAdded,
    bool FiscalYearAdded,
    int AccountingPeriodsAdded,
    int ItemCategoriesAdded,
    int WarehousesAdded);

public sealed class CompanyValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The company request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class CompanyConflictException(string message) : Exception(message);
