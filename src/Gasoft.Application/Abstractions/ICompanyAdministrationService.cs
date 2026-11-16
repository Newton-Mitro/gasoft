using Gasoft.Application.Companies;

namespace Gasoft.Application.Abstractions;

public interface ICompanyAdministrationService
{
    Task<IReadOnlyList<CompanyDto>> GetCompaniesAsync(CancellationToken cancellationToken);
    Task<CompanyDto?> GetCompanyAsync(long companyId, CancellationToken cancellationToken);
    Task<CompanyDto?> CreateCompanyAsync(CreateCompanyRequest request, CancellationToken cancellationToken);
    Task<CompanySetupResultDto?> SeedDefaultsAsync(long companyId, CancellationToken cancellationToken);
    Task<CompanyDto?> SetCompanyLogoUrlAsync(
        long companyId,
        string logoUrl,
        CancellationToken cancellationToken);
    Task<CompanyDto?> UpdateCompanyAsync(
        long companyId,
        UpdateCompanyRequest request,
        CancellationToken cancellationToken);
}
