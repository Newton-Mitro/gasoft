using Gasoft.Application.Abstractions;
using Gasoft.Application.Authentication;
using Microsoft.AspNetCore.Identity;

namespace Gasoft.Infrastructure.Authentication;

public sealed class AccountService(UserManager<ApplicationUser> userManager) : IAccountService
{
    public async Task<AccountProfileDto?> GetProfileAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        return user is null ? null : ToProfile(user);
    }

    public async Task<AccountProfileDto?> UpdateProfileAsync(
        long userId,
        string? displayName,
        CancellationToken cancellationToken)
    {
        var normalizedName = displayName?.Trim() ?? string.Empty;
        if (normalizedName.Length is 0 or > 200)
            throw Validation("displayName", "Display name is required and must be at most 200 characters.");
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;

        user.DisplayName = normalizedName;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new AccountValidationException(new Dictionary<string, string[]>
            {
                ["displayName"] = result.Errors.Select(error => error.Description).ToArray()
            });
        return ToProfile(user);
    }

    public async Task<AccountProfileDto?> UpdateProfileImageAsync(
        long userId,
        string? profileImageUrl,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;
        user.ProfileImageUrl = profileImageUrl;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new AccountValidationException(new Dictionary<string, string[]>
            {
                ["image"] = result.Errors.Select(error => error.Description).ToArray()
            });
        return ToProfile(user);
    }

    public async Task<IReadOnlyDictionary<string, string[]>?> ChangePasswordAsync(
        long userId,
        string? currentPassword,
        string? newPassword)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return null;
        var result = await userManager.ChangePasswordAsync(
            user,
            currentPassword ?? string.Empty,
            newPassword ?? string.Empty);
        if (result.Succeeded) return null;
        return result.Errors
            .Select(error => error.Code == "PasswordMismatch"
                ? ("currentPassword", "Your current password is incorrect.")
                : ("newPassword", error.Description))
            .GroupBy(error => error.Item1)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Item2).ToArray());
    }

    private static AccountProfileDto ToProfile(ApplicationUser user) =>
        new(user.DisplayName ?? string.Empty, user.Email ?? string.Empty, user.ProfileImageUrl);

    private static AccountValidationException Validation(string key, string message) =>
        new(new Dictionary<string, string[]> { [key] = [message] });
}
