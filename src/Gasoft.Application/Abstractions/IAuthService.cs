namespace Gasoft.Application.Abstractions;

public interface IAuthService
{
    Task<IReadOnlyList<AvailableWorkspace>?> GetAvailableWorkspacesAsync(
        string email,
        string password,
        CancellationToken cancellationToken);
    Task<AuthenticatedWorkspace?> AuthenticateAsync(
        string email,
        string password,
        long tenantId,
        CancellationToken cancellationToken);
    Task<bool> RequestPasswordResetAsync(
        string? email,
        CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string[]>?> ResetPasswordAsync(
        string? email,
        string? token,
        string? newPassword,
        CancellationToken cancellationToken);
}

public sealed record AvailableWorkspace(long TenantId, string Name);
public sealed record AuthenticatedWorkspace(string AccessToken, long TenantId, long CompanyId, string WorkspaceSlug);
