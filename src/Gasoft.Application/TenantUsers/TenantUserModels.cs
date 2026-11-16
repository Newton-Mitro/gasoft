namespace Gasoft.Application.TenantUsers;

public sealed record CreateTenantUserRequest(
    string? Email,
    string? DisplayName,
    string? TemporaryPassword,
    string? Role);

public sealed record AddExistingTenantUserRequest(string? Email, string? Role);

public sealed record UpdateTenantUserRequest(string? Role, bool IsActive);

public sealed record TenantUserDto(long Id, string Email, string DisplayName, string Role, bool IsActive);

public sealed record TenantManagerDto(long TenantId, string Role);

public sealed class TenantUserValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The tenant user request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}

public sealed class TenantUserConflictException(string message) : Exception(message);

public sealed class TenantUserForbiddenException(string message) : Exception(message);

public sealed class TenantUserNotFoundException(string message) : Exception(message);
