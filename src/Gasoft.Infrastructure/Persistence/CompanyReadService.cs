using Gasoft.Application.Abstractions;
using Gasoft.Application.Companies;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Infrastructure.Persistence;

public sealed class CompanyReadService(AppDbContext dbContext) : ICompanyReadService
{
    public async Task<IReadOnlyList<CurrencyDto>> GetCurrenciesAsync(CancellationToken cancellationToken) =>
        await dbContext.Currencies
            .AsNoTracking()
            .OrderBy(currency => currency.Code)
            .Select(currency => new CurrencyDto(currency.Code, currency.Name, currency.DecimalPlaces))
            .ToListAsync(cancellationToken);

    public async Task<CompanyDto?> GetCompanyAsync(long companyId, CancellationToken cancellationToken) =>
        await dbContext.Companies
            .AsNoTracking()
            .Where(company => company.Id == companyId)
            .Select(company => new CompanyDto(
                company.Id,
                company.Code,
                company.LegalName,
                company.FunctionalCurrencyCode,
                company.Phone,
                company.Email,
                company.Website,
                company.Address,
                company.CityRegion,
                company.PostalCode,
                company.Country,
                company.TaxId,
                company.LogoUrl))
            .SingleOrDefaultAsync(cancellationToken);
}
