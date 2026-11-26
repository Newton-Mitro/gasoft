using Gasoft.Application.Inventory;

namespace Gasoft.Application.Abstractions;

public interface IStockMovementService
{
    Task<StockMovementDto> ReceiveAsync(
        long companyId,
        ReceiveStockRequest request,
        CancellationToken cancellationToken);
    Task<StockMovementDto> IssueAsync(
        long companyId,
        IssueStockRequest request,
        CancellationToken cancellationToken);
}
