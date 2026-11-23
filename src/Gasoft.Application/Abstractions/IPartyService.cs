using Gasoft.Application.Parties;
using Gasoft.Application.Common;

namespace Gasoft.Application.Abstractions;

public interface IPartyService
{
    Task<IReadOnlyList<PartyDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<PartyDto> GetOrCreateWalkInCustomerAsync(CancellationToken cancellationToken);
    Task<PagedResultDto<PartyDto>> GetPageAsync(int page, int pageSize, CancellationToken cancellationToken);
    Task<PartyDto?> GetAsync(long partyId, CancellationToken cancellationToken);
    Task<PartyDto> CreateAsync(CreatePartyRequest request, CancellationToken cancellationToken);
    Task<PartyDto?> UpdateAsync(long partyId, UpdatePartyRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<PartyAddressDto>?> GetAddressesAsync(long partyId, CancellationToken cancellationToken);
    Task<PagedResultDto<PartyAddressDto>?> GetAddressesPageAsync(
        long partyId, int page, int pageSize, CancellationToken cancellationToken);
    Task<PartyAddressDto?> GetAddressAsync(long partyId, long addressId, CancellationToken cancellationToken);
    Task<PartyAddressDto?> CreateAddressAsync(
        long partyId,
        CreatePartyAddressRequest request,
        CancellationToken cancellationToken);
    Task<PartyAddressDto?> UpdateAddressAsync(
        long partyId,
        long addressId,
        CreatePartyAddressRequest request,
        CancellationToken cancellationToken);
}
