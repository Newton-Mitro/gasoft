namespace Gasoft.Application.Abstractions;

public interface ITenantRegistrationService
{
    Task<TenantRegistrationResult> RegisterAsync(
        TenantRegistrationRequest request,
        CancellationToken cancellationToken);
}

public sealed record TenantRegistrationRequest(
    string TenantName,
    string Slug,
    string CompanyCode,
    string LegalName,
    string FunctionalCurrencyCode,
    string OwnerDisplayName,
    string Email,
    string Password);

public sealed record TenantRegistrationResult(long TenantId, long CompanyId, string AccessToken);
