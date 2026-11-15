using Gasoft.Application.Parties;

namespace Gasoft.Application.Abstractions;

public interface IPartyService
{
    Task<IReadOnlyList<PartyDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<PartyDto?> GetAsync(long partyId, CancellationToken cancellationToken);
    Task<PartyDto> CreateAsync(CreatePartyRequest request, CancellationToken cancellationToken);
    Task<PartyDto?> UpdateAsync(long partyId, UpdatePartyRequest request, CancellationToken cancellationToken);
}
