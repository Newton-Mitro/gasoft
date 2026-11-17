using Gasoft.Application.Abstractions;
using Gasoft.Application.Parties;

namespace Gasoft.Api.Endpoints;

public static class PartyEndpoints
{
    public static IEndpointRouteBuilder MapPartyEndpoints(this IEndpointRouteBuilder app)
    {
        var parties = app.MapGroup("/api/v1/parties")
            .RequireAuthorization()
            .AddEndpointFilterFactory(RequestShapeValidationEndpointFilter.Create);

        parties.MapGet("/", async (IPartyService service, CancellationToken cancellationToken) =>
                Results.Ok(await service.GetAllAsync(cancellationToken)))
            .WithName("GetParties");

        parties.MapGet("/page", async (
            int page,
            int pageSize,
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            return Results.Ok(await service.GetPageAsync(page, pageSize, cancellationToken));
        })
        .WithName("GetPartiesPage");

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
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            var addresses = await service.GetAddressesAsync(partyId, cancellationToken);
            return addresses is null
                ? Results.NotFound(new { error = "Party was not found in this tenant." })
                : Results.Ok(addresses);
        })
        .WithName("GetPartyAddresses");
        parties.MapGet("/{partyId:long}/addresses/page", async (
            long partyId,
            int page,
            int pageSize,
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            if (page < 1 || pageSize is < 1 or > 100)
                return Results.BadRequest(new { error = "Page must be positive and page size must be between 1 and 100." });
            var result = await service.GetAddressesPageAsync(partyId, page, pageSize, cancellationToken);
            return result is null
                ? Results.NotFound(new { error = "Party was not found in this tenant." })
                : Results.Ok(result);
        });

        parties.MapGet("/{partyId:long}/addresses/{addressId:long}", async (
            long partyId,
            long addressId,
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            var address = await service.GetAddressAsync(partyId, addressId, cancellationToken);
            return address is null ? Results.NotFound(new { error = "Party address was not found." }) : Results.Ok(address);
        });

        parties.MapPost("/{partyId:long}/addresses", async (
            long partyId,
            CreatePartyAddressRequest request,
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var address = await service.CreateAddressAsync(partyId, request, cancellationToken);
                return address is null
                    ? Results.NotFound(new { error = "Party was not found in this tenant." })
                    : Results.Created($"/api/v1/parties/{partyId}/addresses/{address.Id}", address);
            }
            catch (PartyValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
        })
        .WithName("CreatePartyAddress");

        parties.MapPut("/{partyId:long}/addresses/{addressId:long}", async (
            long partyId,
            long addressId,
            CreatePartyAddressRequest request,
            IPartyService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var address = await service.UpdateAddressAsync(partyId, addressId, request, cancellationToken);
                return address is null ? Results.NotFound(new { error = "Party address was not found." }) : Results.Ok(address);
            }
            catch (PartyValidationException exception)
            {
                return Results.ValidationProblem(exception.Errors);
            }
        })
        .WithName("UpdatePartyAddress");

        return app;
    }
}
