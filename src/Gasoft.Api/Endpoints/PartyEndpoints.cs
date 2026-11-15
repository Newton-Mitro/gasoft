using Gasoft.Application.Abstractions;
using Gasoft.Application.Parties;
using Gasoft.Domain.Entities;
using Gasoft.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Gasoft.Api.Endpoints;

public static class PartyEndpoints
{
    public static IEndpointRouteBuilder MapPartyEndpoints(this IEndpointRouteBuilder app)
    {
        var parties = app.MapGroup("/api/v1/parties").RequireAuthorization();

        parties.MapGet("/", async (IPartyService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAllAsync(cancellationToken)))
            .WithName("GetParties");

        parties.MapGet("/{partyId:long}", async (
            long partyId,
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            var party = await service.GetAsync(partyId, cancellationToken);
            return party is null ? Results.NotFound(new { error = "Party was not found in this tenant." }) : Results.Ok(party);
        })
        .WithName("GetParty");

        parties.MapPost("/", async (
            CreatePartyRequest request,
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.PartyNo) ||
                string.IsNullOrWhiteSpace(request.DisplayName) ||
                (!request.IsCustomer && !request.IsSupplier))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["party"] = ["A party number, display name, and at least one party role are required."]
                });
            }

            try
            {
                return Results.Created(
                    "/api/v1/parties",
                    await service.CreateAsync(request, cancellationToken));
            }
            catch (CurrencyNotConfiguredException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["defaultCurrencyCode"] = [exception.Message]
                });
            }
            catch (PartyConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .WithName("CreateParty");

        parties.MapPut("/{partyId:long}", async (
            long partyId,
            UpdatePartyRequest request,
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var party = await service.UpdateAsync(partyId, request, cancellationToken);
                return party is null ? Results.NotFound(new { error = "Party was not found in this tenant." }) : Results.Ok(party);
            }
            catch (PartyValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
            catch (CurrencyNotConfiguredException exception)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["defaultCurrencyCode"] = [exception.Message]
                });
            }
            catch (PartyConflictException exception)
            {
                return Results.Conflict(new { error = exception.Message });
            }
        })
        .WithName("UpdateParty");

        parties.MapGet("/{partyId:long}/addresses", async (
            long partyId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (!await db.Parties.AnyAsync(party => party.Id == partyId, cancellationToken))
                return Results.NotFound(new { error = "Party was not found in this tenant." });

            var addresses = await db.PartyAddresses.AsNoTracking()
                .Where(address => address.PartyId == partyId)
                .OrderByDescending(address => address.IsDefault)
                .ThenBy(address => address.AddressType)
                .ThenBy(address => address.Id)
                .Select(address => new PartyAddressResponse(
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
            return Results.Ok(addresses);
        })
        .WithName("GetPartyAddresses");

        parties.MapGet("/{partyId:long}/addresses/{addressId:long}", async (
            long partyId,
            long addressId,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var address = await db.PartyAddresses.AsNoTracking()
                .Where(item => item.PartyId == partyId && item.Id == addressId)
                .Select(item => new PartyAddressResponse(
                    item.Id, item.PartyId, item.AddressType, item.Line1, item.Line2, item.City,
                    item.Region, item.PostalCode, item.CountryCode, item.IsDefault))
                .SingleOrDefaultAsync(cancellationToken);
            return address is null ? Results.NotFound(new { error = "Party address was not found." }) : Results.Ok(address);
        });

        parties.MapPost("/{partyId:long}/addresses", async (
            long partyId,
            CreatePartyAddressRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var party = await db.Parties.SingleOrDefaultAsync(item => item.Id == partyId, cancellationToken);
            if (party is null) return Results.NotFound(new { error = "Party was not found in this tenant." });

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
            if (errors.Count != 0) return Results.ValidationProblem(errors);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (request.IsDefault)
            {
                var existingDefaults = await db.PartyAddresses
                    .Where(address => address.PartyId == partyId && address.IsDefault)
                    .ToListAsync(cancellationToken);
                foreach (var address in existingDefaults) address.IsDefault = false;
            }

            var entity = new PartyAddress
            {
                TenantId = db.CurrentTenantId
                    ?? throw new UnauthorizedAccessException("A tenant-scoped access token is required."),
                PartyId = partyId,
                AddressType = addressType,
                Line1 = line1,
                Line2 = line2,
                City = city,
                Region = region,
                PostalCode = postalCode,
                CountryCode = countryCode,
                IsDefault = request.IsDefault
            };
            db.PartyAddresses.Add(entity);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var response = new PartyAddressResponse(
                entity.Id,
                entity.PartyId,
                entity.AddressType,
                entity.Line1,
                entity.Line2,
                entity.City,
                entity.Region,
                entity.PostalCode,
                entity.CountryCode,
                entity.IsDefault);
            return Results.Created($"/api/v1/parties/{partyId}/addresses/{entity.Id}", response);
        })
        .WithName("CreatePartyAddress");

        parties.MapPut("/{partyId:long}/addresses/{addressId:long}", async (
            long partyId,
            long addressId,
            CreatePartyAddressRequest request,
            AppDbContext db,
            CancellationToken cancellationToken) =>
        {
            var address = await db.PartyAddresses.SingleOrDefaultAsync(
                item => item.PartyId == partyId && item.Id == addressId, cancellationToken);
            if (address is null) return Results.NotFound(new { error = "Party address was not found." });

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
            if (errors.Count != 0) return Results.ValidationProblem(errors);

            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            if (request.IsDefault)
            {
                var existingDefaults = await db.PartyAddresses
                    .Where(item => item.PartyId == partyId && item.Id != addressId && item.IsDefault)
                    .ToListAsync(cancellationToken);
                foreach (var existing in existingDefaults) existing.IsDefault = false;
            }
            address.AddressType = addressType;
            address.Line1 = line1;
            address.Line2 = line2;
            address.City = city;
            address.Region = region;
            address.PostalCode = postalCode;
            address.CountryCode = countryCode;
            address.IsDefault = request.IsDefault;
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Results.Ok(new PartyAddressResponse(
                address.Id, address.PartyId, address.AddressType, address.Line1, address.Line2, address.City,
                address.Region, address.PostalCode, address.CountryCode, address.IsDefault));
        })
        .WithName("UpdatePartyAddress");

        return app;
    }

    public sealed record CreatePartyAddressRequest(
        string? AddressType,
        string? Line1,
        string? Line2,
        string? City,
        string? Region,
        string? PostalCode,
        string? CountryCode,
        bool IsDefault);

    public sealed record PartyAddressResponse(
        long Id,
        long PartyId,
        string AddressType,
        string Line1,
        string? Line2,
        string City,
        string? Region,
        string? PostalCode,
        string CountryCode,
        bool IsDefault);
}
