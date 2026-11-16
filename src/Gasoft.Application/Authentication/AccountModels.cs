namespace Gasoft.Application.Authentication;

public sealed record AccountProfileDto(string DisplayName, string Email, string? ProfileImageUrl);

public sealed class AccountValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The account request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
