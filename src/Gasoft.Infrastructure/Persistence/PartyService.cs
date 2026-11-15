using Gasoft.Application.Abstractions;
using Gasoft.Application.Parties;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class PartyService(AppDbContext dbContext, ICurrentTenant currentTenant) : IPartyService
{
    public async Task<IReadOnlyList<PartyDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await dbContext.Parties
            .AsNoTracking()
            .OrderBy(party => party.DisplayName)
            .Select(party => new PartyDto(
                party.Id,
                party.PartyNo,
                party.DisplayName,
                party.IsCustomer,
                party.IsSupplier,
                party.Email,
                party.Phone,
                party.DefaultCurrencyCode))
            .ToListAsync(cancellationToken);
    }

    public async Task<PartyDto?> GetAsync(long partyId, CancellationToken cancellationToken)
    {
        EnsureTenant();
        var party = await dbContext.Parties.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == partyId, cancellationToken);
        return party is null ? null : ToDto(party);
    }

    public async Task<PartyDto> CreateAsync(CreatePartyRequest request, CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var party = new Party
        {
            TenantId = tenantId,
            PartyNo = request.PartyNo.Trim(),
            DisplayName = request.DisplayName.Trim(),
            IsCustomer = request.IsCustomer,
            IsSupplier = request.IsSupplier,
            Email = NormalizeOptional(request.Email),
            Phone = NormalizeOptional(request.Phone),
            DefaultCurrencyCode = NormalizeOptional(request.DefaultCurrencyCode)?.ToUpperInvariant()
        };

        if (party.DefaultCurrencyCode is not null &&
            !await dbContext.Currencies.AnyAsync(
                currency => currency.Code == party.DefaultCurrencyCode,
                cancellationToken))
        {
            throw new CurrencyNotConfiguredException(party.DefaultCurrencyCode);
        }

        dbContext.Parties.Add(party);
        await SaveAsync(cancellationToken);
        return ToDto(party);
    }

    public async Task<PartyDto?> UpdateAsync(
        long partyId,
        UpdatePartyRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var party = await dbContext.Parties.SingleOrDefaultAsync(item => item.Id == partyId, cancellationToken);
        if (party is null) return null;
        if (string.IsNullOrWhiteSpace(request.PartyNo) ||
            request.PartyNo.Trim().Length > 50 ||
            string.IsNullOrWhiteSpace(request.DisplayName) ||
            request.DisplayName.Trim().Length > 200 ||
            (!request.IsCustomer && !request.IsSupplier))
        {
            throw new PartyValidationException(new Dictionary<string, string[]>
            {
                ["party"] = ["A party number, display name, and at least one party role are required; values must be within supported lengths."]
            });
        }

        var currencyCode = NormalizeOptional(request.DefaultCurrencyCode)?.ToUpperInvariant();
        if (currencyCode is not null &&
            !await dbContext.Currencies.AnyAsync(currency => currency.Code == currencyCode, cancellationToken))
            throw new CurrencyNotConfiguredException(currencyCode);

        party.PartyNo = request.PartyNo.Trim();
        party.DisplayName = request.DisplayName.Trim();
        party.IsCustomer = request.IsCustomer;
        party.IsSupplier = request.IsSupplier;
        party.Email = NormalizeOptional(request.Email);
        party.Phone = NormalizeOptional(request.Phone);
        party.DefaultCurrencyCode = currencyCode;
        await SaveAsync(cancellationToken);
        return ToDto(party);
    }

    private long EnsureTenant() =>
        currentTenant.TenantId ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new PartyConflictException("A party with this number already exists in the tenant.");
        }
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static PartyDto ToDto(Party party) => new(
        party.Id,
        party.PartyNo,
        party.DisplayName,
        party.IsCustomer,
        party.IsSupplier,
        party.Email,
        party.Phone,
        party.DefaultCurrencyCode);
}
