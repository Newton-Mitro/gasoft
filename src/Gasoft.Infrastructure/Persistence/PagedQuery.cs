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
        ValidatePage(page, pageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        return await ToPageAsync(query, totalCount, page, pageSize, selector, cancellationToken);
    }

    public static async Task<PagedResultDto<TDto>> ToPageAsync<TEntity, TCount, TDto>(
        IQueryable<TEntity> query,
        IQueryable<TCount> countQuery,
        int page,
        int pageSize,
        Expression<Func<TEntity, TDto>> selector,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        var totalCount = await countQuery.CountAsync(cancellationToken);
        return await ToPageAsync(query, totalCount, page, pageSize, selector, cancellationToken);
    }

    private static async Task<PagedResultDto<TDto>> ToPageAsync<TEntity, TDto>(
        IQueryable<TEntity> query,
        int totalCount,
        int page,
        int pageSize,
        Expression<Func<TEntity, TDto>> selector,
        CancellationToken cancellationToken)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        var normalizedPage = totalPages == 0 ? 1 : Math.Min(page, totalPages);
        var items = await query
            .Skip((normalizedPage - 1) * pageSize)
            .Take(pageSize)
            .Select(selector)
            .ToListAsync(cancellationToken);
        return new PagedResultDto<TDto>(items, normalizedPage, pageSize, totalCount, totalPages);
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page < 1 || pageSize is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be positive and page size must be between 1 and 100.");
        }
    }
}
