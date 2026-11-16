namespace Gasoft.Application.Taxes;

public sealed record CreateTaxRegistrationRequest(long JurisdictionId, string? RegistrationNumber);
public sealed record TaxCodeRequest(long JurisdictionId, long CategoryId, string? Code, string? TaxType);
public sealed record TaxRateRequest(decimal RatePercent, DateOnly ValidFrom, DateOnly? ValidTo);

public sealed record TaxJurisdictionDto(long Id, string Code, string CountryCode);
public sealed record TaxCategoryDto(long Id, string Code, string Name);
public sealed record TaxRateDto(long Id, long TaxCodeId, decimal RatePercent, DateOnly ValidFrom, DateOnly? ValidTo);
public sealed record TaxCodeDto(
    long Id,
    long JurisdictionId,
    string JurisdictionCode,
    string CountryCode,
    long CategoryId,
    string CategoryCode,
    string CategoryName,
    string Code,
    string TaxType,
    IReadOnlyList<TaxRateDto> Rates);
public sealed record TaxReferenceDto(
    IReadOnlyList<TaxJurisdictionDto> Jurisdictions,
    IReadOnlyList<TaxCategoryDto> Categories,
    IReadOnlyList<TaxCodeDto> Codes);
public sealed record TaxRegistrationDto(
    long Id,
    long CompanyId,
    long JurisdictionId,
    string Code,
    string CountryCode,
    string RegistrationNumber);
public sealed record CreatedTaxRegistrationDto(
    long Id,
    long CompanyId,
    long JurisdictionId,
    string RegistrationNumber);

public sealed class TaxValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The tax request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class TaxConflictException(string message) : Exception(message);

public sealed class TaxNotFoundException(string message) : Exception(message);
