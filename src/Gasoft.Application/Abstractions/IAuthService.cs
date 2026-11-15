namespace Gasoft.Application.Abstractions;

public interface IAuthService
{
    Task<AuthenticatedWorkspace?> AuthenticateAsync(
        string email,
        string password,
        string workspaceSlug,
        CancellationToken cancellationToken);
}

public sealed record AuthenticatedWorkspace(string AccessToken, long TenantId, long CompanyId);
