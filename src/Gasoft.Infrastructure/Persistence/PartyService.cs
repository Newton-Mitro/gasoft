using Gasoft.Application.Abstractions;
using Gasoft.Application.Parties;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed class PartyService(AppDbContext dbContext, ICurrentTenant currentTenant) : IPartyService
{
    private const string WalkInPartyNo = "WALK-IN";

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

    public async Task<PartyDto> GetOrCreateWalkInCustomerAsync(CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        var existing = await dbContext.Parties.AsNoTracking()
            .SingleOrDefaultAsync(party => party.PartyNo == WalkInPartyNo, cancellationToken);
        if (existing is not null)
            return ToWalkInCustomerDto(existing);

        var walkInCustomer = new Party
        {
            TenantId = tenantId,
            PartyNo = WalkInPartyNo,
            DisplayName = "Walk-in Customer",
            IsCustomer = true,
            IsSupplier = false
        };
        dbContext.Parties.Add(walkInCustomer);
        try
        {
            await SaveAsync(cancellationToken);
        }
        catch (PartyConflictException)
        {
            dbContext.Entry(walkInCustomer).State = EntityState.Detached;
            var concurrent = await dbContext.Parties.AsNoTracking()
                .SingleOrDefaultAsync(party => party.PartyNo == WalkInPartyNo, cancellationToken);
            if (concurrent is not null)
                return ToWalkInCustomerDto(concurrent);
            throw;
        }

        return ToDto(walkInCustomer);
    }

    public async Task<PagedResultDto<PartyDto>> GetPageAsync(
        int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await PagedQuery.ToPageAsync(
            dbContext.Parties.AsNoTracking()
                .OrderBy(party => party.DisplayName)
                .ThenBy(party => party.Id),
            page,
            pageSize,
            party => new PartyDto(
                party.Id,
                party.PartyNo,
                party.DisplayName,
                party.IsCustomer,
                party.IsSupplier,
                party.Email,
                party.Phone,
                party.DefaultCurrencyCode),
            cancellationToken);
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

    public async Task<IReadOnlyList<PartyAddressDto>?> GetAddressesAsync(
        long partyId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        if (!await dbContext.Parties.AnyAsync(party => party.Id == partyId, cancellationToken)) return null;

        return await dbContext.PartyAddresses.AsNoTracking()
            .Where(address => address.PartyId == partyId)
            .OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.AddressType)
            .ThenBy(address => address.Id)
            .Select(address => new PartyAddressDto(
                address.Id,
                address.PartyId,
                address.AddressType,
                address.Line1,
                address.Line2,
                address.City,
                address.Region,
                address.PostalCode,
                address.CountryCode,
                address.IsDefault))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResultDto<PartyAddressDto>?> GetAddressesPageAsync(
        long partyId, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsureTenant();
        if (!await dbContext.Parties.AnyAsync(party => party.Id == partyId, cancellationToken)) return null;
        return await PagedQuery.ToPageAsync(
            dbContext.PartyAddresses.AsNoTracking()
                .Where(address => address.PartyId == partyId)
                .OrderByDescending(address => address.IsDefault)
                .ThenBy(address => address.AddressType)
                .ThenBy(address => address.Id),
            page,
            pageSize,
            address => new PartyAddressDto(
                address.Id,
                address.PartyId,
                address.AddressType,
                address.Line1,
                address.Line2,
                address.City,
                address.Region,
                address.PostalCode,
                address.CountryCode,
                address.IsDefault),
            cancellationToken);
    }

    public async Task<PartyAddressDto?> GetAddressAsync(
        long partyId,
        long addressId,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        return await dbContext.PartyAddresses.AsNoTracking()
            .Where(address => address.PartyId == partyId && address.Id == addressId)
            .Select(address => new PartyAddressDto(
                address.Id,
                address.PartyId,
                address.AddressType,
                address.Line1,
                address.Line2,
                address.City,
                address.Region,
                address.PostalCode,
                address.CountryCode,
                address.IsDefault))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PartyAddressDto?> CreateAddressAsync(
        long partyId,
        CreatePartyAddressRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = EnsureTenant();
        if (!await dbContext.Parties.AnyAsync(party => party.Id == partyId, cancellationToken)) return null;
        var values = ValidateAddress(request);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (request.IsDefault)
        {
            var existingDefaults = await dbContext.PartyAddresses
                .Where(address => address.PartyId == partyId && address.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var address in existingDefaults) address.IsDefault = false;
        }

        var entity = new PartyAddress
        {
            TenantId = tenantId,
            PartyId = partyId,
            AddressType = values.AddressType,
            Line1 = values.Line1,
            Line2 = values.Line2,
            City = values.City,
            Region = values.Region,
            PostalCode = values.PostalCode,
            CountryCode = values.CountryCode,
            IsDefault = request.IsDefault
        };
        dbContext.PartyAddresses.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToAddressDto(entity);
    }

    public async Task<PartyAddressDto?> UpdateAddressAsync(
        long partyId,
        long addressId,
        CreatePartyAddressRequest request,
        CancellationToken cancellationToken)
    {
        EnsureTenant();
        var address = await dbContext.PartyAddresses.SingleOrDefaultAsync(
            item => item.PartyId == partyId && item.Id == addressId,
            cancellationToken);
        if (address is null) return null;
        var values = ValidateAddress(request);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (request.IsDefault)
        {
            var existingDefaults = await dbContext.PartyAddresses
                .Where(item => item.PartyId == partyId && item.Id != addressId && item.IsDefault)
                .ToListAsync(cancellationToken);
            foreach (var existing in existingDefaults) existing.IsDefault = false;
        }
        address.AddressType = values.AddressType;
        address.Line1 = values.Line1;
        address.Line2 = values.Line2;
        address.City = values.City;
        address.Region = values.Region;
        address.PostalCode = values.PostalCode;
        address.CountryCode = values.CountryCode;
        address.IsDefault = request.IsDefault;
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToAddressDto(address);
    }

    private static AddressValues ValidateAddress(CreatePartyAddressRequest request)
    {
        var addressType = request.AddressType?.Trim().ToUpperInvariant() ?? string.Empty;
        var line1 = request.Line1?.Trim() ?? string.Empty;
        var line2 = string.IsNullOrWhiteSpace(request.Line2) ? null : request.Line2.Trim();
        var city = request.City?.Trim() ?? string.Empty;
        var region = string.IsNullOrWhiteSpace(request.Region) ? null : request.Region.Trim();
        var postalCode = string.IsNullOrWhiteSpace(request.PostalCode) ? null : request.PostalCode.Trim();
        var countryCode = request.CountryCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var errors = new Dictionary<string, string[]>();
        if (addressType is not ("BILLING" or "SHIPPING" or "OTHER"))
            errors["addressType"] = ["Choose Billing, Shipping, or Other."];
        if (line1.Length is 0 or > 200)
            errors["line1"] = ["Address line 1 is required and must be at most 200 characters."];
        if (line2?.Length > 200)
            errors["line2"] = ["Address line 2 must be at most 200 characters."];
        if (city.Length is 0 or > 100)
            errors["city"] = ["City is required and must be at most 100 characters."];
        if (region?.Length > 100)
            errors["region"] = ["Region must be at most 100 characters."];
        if (postalCode?.Length > 20)
            errors["postalCode"] = ["Postal code must be at most 20 characters."];
        if (countryCode.Length != 2 || !countryCode.All(char.IsAsciiLetter))
            errors["countryCode"] = ["Country code must be a two-letter ISO code."];
        if (errors.Count != 0) throw new PartyValidationException(errors);
        return new AddressValues(addressType, line1, line2, city, region, postalCode, countryCode);
    }

    private long EnsureTenant() =>
        currentTenant.TenantId ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required.");

    private sealed record AddressValues(
        string AddressType,
        string Line1,
        string? Line2,
        string City,
        string? Region,
        string? PostalCode,
        string CountryCode);

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

    private static PartyDto ToWalkInCustomerDto(Party party)
    {
        if (!party.IsCustomer)
            throw new PartyConflictException("The reserved WALK-IN party number is already used by a non-customer.");
        return ToDto(party);
    }

    private static PartyAddressDto ToAddressDto(PartyAddress address) => new(
        address.Id,
        address.PartyId,
        address.AddressType,
        address.Line1,
        address.Line2,
        address.City,
        address.Region,
        address.PostalCode,
        address.CountryCode,
        address.IsDefault);
}
