using Gasoft.Application.Abstractions;
using Gasoft.Application.Inventory;
using Gasoft.Application.Common;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class InventoryService : InventoryServiceBase, IInventoryService
{
    private readonly IStockMovementService stockMovements;

    public InventoryService(
        AppDbContext db,
        ICurrentTenant currentTenant,
        IStockMovementService stockMovements)
        : base(db, currentTenant)
    {
        this.stockMovements = stockMovements;
    }
}
