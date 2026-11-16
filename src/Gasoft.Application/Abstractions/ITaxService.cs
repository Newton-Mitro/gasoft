using Gasoft.Application.Taxes;
using Gasoft.Application.Common;

namespace Gasoft.Application.Abstractions;

public interface ITaxService
{
    Task<IReadOnlyList<TaxJurisdictionDto>> GetJurisdictionsAsync(CancellationToken cancellationToken);
    Task<PagedResultDto<TaxJurisdictionDto>> GetJurisdictionsPageAsync(
        int page, int pageSize, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaxCodeDto>?> GetCompanyTaxCodesAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<TaxCodeDto>?> GetCompanyTaxCodesPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<TaxCodeDto?> CreateTaxCodeAsync(long companyId, TaxCodeRequest request, CancellationToken cancellationToken);
    Task<TaxCodeDto?> UpdateTaxCodeAsync(long companyId, long taxCodeId, TaxCodeRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteTaxCodeAsync(long companyId, long taxCodeId, CancellationToken cancellationToken);
    Task<TaxRateDto?> CreateTaxRateAsync(long companyId, long taxCodeId, TaxRateRequest request, CancellationToken cancellationToken);
    Task<TaxRateDto?> UpdateTaxRateAsync(long companyId, long taxCodeId, long rateId, TaxRateRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteTaxRateAsync(long companyId, long taxCodeId, long rateId, CancellationToken cancellationToken);
    Task<TaxReferenceDto?> GetCompanyTaxReferenceAsync(long companyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<TaxRegistrationDto>> GetRegistrationsAsync(long companyId, CancellationToken cancellationToken);
    Task<PagedResultDto<TaxRegistrationDto>> GetRegistrationsPageAsync(
        long companyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<TaxRegistrationDto?> GetRegistrationAsync(long companyId, long registrationId, CancellationToken cancellationToken);
    Task<CreatedTaxRegistrationDto?> CreateRegistrationAsync(long companyId, CreateTaxRegistrationRequest request, CancellationToken cancellationToken);
    Task<TaxRegistrationDto?> UpdateRegistrationAsync(long companyId, long registrationId, CreateTaxRegistrationRequest request, CancellationToken cancellationToken);
}
