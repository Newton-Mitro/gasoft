using Gasoft.Application.Authentication;

namespace Gasoft.Application.Abstractions;

public interface IAccountService
{
    Task<AccountProfileDto?> GetProfileAsync(long userId, CancellationToken cancellationToken);
    Task<AccountProfileDto?> UpdateProfileAsync(
        long userId,
        string? displayName,
        CancellationToken cancellationToken);
    Task<AccountProfileDto?> UpdateProfileImageAsync(
        long userId,
        string? profileImageUrl,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string[]>?> ChangePasswordAsync(
        long userId,
        string? currentPassword,
        string? newPassword);
}
