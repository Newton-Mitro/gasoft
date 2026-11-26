using Gasoft.Application.Abstractions;

namespace Gasoft.Infrastructure.Persistence;

public sealed partial class JournalEntryService : AccountingServiceBase, IJournalEntryService
{
    public JournalEntryService(AppDbContext db, ICurrentTenant currentTenant)
        : base(db, currentTenant)
    {
    }
}
