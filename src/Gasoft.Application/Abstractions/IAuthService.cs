namespace Gasoft.Application.Abstractions;

public interface IAuthService
{
    Task<AuthenticatedWorkspace?> AuthenticateAsync(
        string email,
        string password,
        string workspaceSlug,
        CancellationToken cancellationToken);
    Task<bool> RequestPasswordResetAsync(
        string? email,
        string? workspaceSlug,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string[]>?> ResetPasswordAsync(
        string? email,
        string? token,
        string? newPassword,
        CancellationToken cancellationToken);
}

public sealed record AuthenticatedWorkspace(string AccessToken, long TenantId, long CompanyId);
