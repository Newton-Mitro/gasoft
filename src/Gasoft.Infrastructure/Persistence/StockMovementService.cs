using Gasoft.Application.Abstractions;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class StockMovementService : InventoryServiceBase, IStockMovementService
{
    public StockMovementService(AppDbContext db, ICurrentTenant currentTenant)
        : base(db, currentTenant)
    {
    }
}
