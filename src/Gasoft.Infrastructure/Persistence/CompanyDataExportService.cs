using System.Collections;
using System.IO.Compression;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gasoft.Application.Abstractions;
using Gasoft.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Gasoft.Infrastructure.Persistence;

public sealed class CompanyDataExportService(AppDbContext db) : ICompanyDataExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<CompanyDataExport?> ExportAsync(
        long companyId,
        CancellationToken cancellationToken)
    {
        var tenantId = db.CurrentTenantId;
        if (!tenantId.HasValue)
        {
            return null;
        }

        var company = await db.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(row => row.Id == companyId, cancellationToken);
        if (company is null)
        {
            return null;
        }

        var exportedTables = new List<ExportedTable>();
        await using var archiveStream = new MemoryStream();
        using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entityTypes = db.Model.GetEntityTypes()
                .Where(entity => entity.ClrType.IsClass &&
                    !entity.ClrType.IsAbstract &&
                    typeof(TenantOwnedEntity).IsAssignableFrom(entity.ClrType) &&
                    entity.GetTableName() is not null)
                .OrderBy(entity => entity.GetTableName(), StringComparer.Ordinal)
                .ToArray();

            foreach (var entityType in entityTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rows = await LoadRowsAsync(
                    entityType.ClrType,
                    tenantId.Value,
                    companyId,
                    cancellationToken);
                var tableName = entityType.GetTableName()!;
                WriteEntityFile(archive, tableName, entityType, rows);
                exportedTables.Add(new ExportedTable(tableName, rows.Count));
            }

            var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
            await using var manifestStream = manifestEntry.Open();
            await JsonSerializer.SerializeAsync(
                manifestStream,
                new
                {
                    formatVersion = 1,
                    exportType = "Gasoft company data export",
                    generatedAtUtc = DateTimeOffset.UtcNow,
                    company = new { company.Id, company.Code, company.LegalName },
                    scope = "Company-scoped records and tenant-level records shared by companies.",
                    excludes = new[]
                    {
                        "Authentication credentials",
                        "Global reference catalogs",
                        "Uploaded files",
                    },
                    dataFiles = exportedTables.Select(table => new
                    {
                        file = $"data/{table.Name}.json",
                        table.RecordCount,
                    }),
                },
                JsonOptions,
                cancellationToken);
        }

        var fileName = $"company-data-{company.Code}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.zip";
        return new CompanyDataExport(archiveStream.ToArray(), fileName);
    }

    private async Task<List<object>> LoadRowsAsync(
        Type entityType,
        long tenantId,
        long companyId,
        CancellationToken cancellationToken)
    {
        var setMethod = typeof(DbContext)
            .GetMethod(nameof(DbContext.Set), Type.EmptyTypes)!
            .MakeGenericMethod(entityType);
        IQueryable query = (IQueryable)setMethod.Invoke(db, null)!;
        var parameter = Expression.Parameter(entityType, "row");
        Expression? predicate = null;

        if (entityType.GetProperty(nameof(TenantOwnedEntity.TenantId)) is { } tenantProperty)
        {
            predicate = EqualProperty(parameter, tenantProperty, tenantId);
        }

        if (entityType == typeof(Company))
        {
            predicate = And(predicate, EqualProperty(parameter, entityType.GetProperty(nameof(Company.Id))!, companyId));
        }
        else if (entityType.GetProperty("CompanyId") is { } companyProperty)
        {
            var companyMatch = EqualProperty(parameter, companyProperty, companyId);
            if (Nullable.GetUnderlyingType(companyProperty.PropertyType) is not null)
            {
                var sharedMatch = Expression.Equal(
                    Expression.Property(parameter, companyProperty),
                    Expression.Constant(null, companyProperty.PropertyType));
                companyMatch = Expression.OrElse(companyMatch, sharedMatch);
            }

            predicate = And(predicate, companyMatch);
        }

        if (predicate is not null)
        {
            var filter = Expression.Lambda(predicate, parameter);
            var where = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Where),
                [entityType],
                query.Expression,
                Expression.Quote(filter));
            query = query.Provider.CreateQuery(where);
        }

        var method = typeof(CompanyDataExportService)
            .GetMethod(nameof(ReadRowsAsync), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(entityType);
        var task = (Task)method.Invoke(null, [query, cancellationToken])!;
        await task;
        return ((IEnumerable)task.GetType().GetProperty("Result")!.GetValue(task)!)
            .Cast<object>()
            .ToList();
    }

    private static async Task<List<TEntity>> ReadRowsAsync<TEntity>(
        IQueryable query,
        CancellationToken cancellationToken) where TEntity : class =>
        await EntityFrameworkQueryableExtensions.ToListAsync(
            ((IQueryable<TEntity>)query).AsNoTracking(),
            cancellationToken);

    private static Expression EqualProperty(ParameterExpression parameter, PropertyInfo property, long value)
    {
        var member = Expression.Property(parameter, property);
        var constant = Expression.Convert(Expression.Constant(value), property.PropertyType);
        return Expression.Equal(member, constant);
    }

    private static Expression And(Expression? left, Expression right) =>
        left is null ? right : Expression.AndAlso(left, right);

    private void WriteEntityFile(
        ZipArchive archive,
        string tableName,
        IEntityType entityType,
        IReadOnlyList<object> rows)
    {
        var entry = archive.CreateEntry($"data/{tableName}.json", CompressionLevel.Optimal);
        using var entryStream = entry.Open();
        using var writer = new Utf8JsonWriter(entryStream);
        writer.WriteStartArray();
        var properties = entityType.GetProperties().ToArray();

        foreach (var row in rows)
        {
            writer.WriteStartObject();
            foreach (var property in properties)
            {
                writer.WritePropertyName(JsonOptions.PropertyNamingPolicy!.ConvertName(property.Name));
                var value = property.PropertyInfo?.GetValue(row) ??
                    db.Entry(row).Property(property.Name).CurrentValue;
                JsonSerializer.Serialize(writer, value, property.ClrType, JsonOptions);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private sealed record ExportedTable(string Name, int RecordCount);
}
