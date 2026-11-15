namespace Gasoft.Application.Parties;

public sealed record PartyDto(
    long Id,
    string PartyNo,
    string DisplayName,
    bool IsCustomer,
    bool IsSupplier,
    string? Email,
    string? Phone,
    string? DefaultCurrencyCode);

public sealed record CreatePartyRequest(
    string PartyNo,
    string DisplayName,
    bool IsCustomer,
    bool IsSupplier,
    string? Email,
    string? Phone,
    string? DefaultCurrencyCode);

public sealed record UpdatePartyRequest(
    string PartyNo,
    string DisplayName,
    bool IsCustomer,
    bool IsSupplier,
    string? Email,
    string? Phone,
    string? DefaultCurrencyCode);

public sealed class PartyValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The party request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class PartyConflictException(string message) : Exception(message);
