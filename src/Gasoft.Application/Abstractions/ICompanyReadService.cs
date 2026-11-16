using Gasoft.Application.Companies;

namespace Gasoft.Application.Abstractions;

public interface ICompanyReadService
{
    Task<IReadOnlyList<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken);
    Task<CompanyDto?> GetCompanyAsync(long companyId, CancellationToken cancellationToken);
}

public sealed record CurrencyDto(string Code, string Name, byte DecimalPlaces);
