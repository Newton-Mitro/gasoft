using Gasoft.Application.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Gasoft.Infrastructure.Persistence;

internal static class PagedQuery
{
    public static async Task<PagedResultDto<TDto>> ToPageAsync<TEntity, TDto>(
        IQueryable<TEntity> query,
        int page,
        int pageSize,
        Expression<Func<TEntity, TDto>> selector,
        CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive and page size must be between 1 and 100.");
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var items = await query
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);
        return new PagedResultDto<TDto>(items, normalizedPage, pageSize, totalCount, totalPages);
    }
}
