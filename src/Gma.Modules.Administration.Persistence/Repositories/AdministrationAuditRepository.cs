namespace Gma.Modules.Administration.Persistence.Repositories;

using Gma.Framework.Administration;
using Gma.Modules.Administration.Application.Models;
using Gma.Modules.Administration.Application.Ports;
using Gma.Modules.Administration.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

internal sealed class AdministrationAuditRepository(AdminDbContext dbContext)
    : IAdministrationAuditRepository
{
    public async Task<IReadOnlyList<AdministrationAuditEntryDetails>> ListAsync(
        AdministrationAuditFilter filter,
        AdministrationAuditCursor? cursor,
        int take,
        CancellationToken cancellationToken)
    {
        IQueryable<AdminAuditEntry> query = dbContext.AuditEntries.AsNoTracking();

        if (filter.TenantId is not null)
        {
            query = query.Where(entry => entry.TenantId == filter.TenantId);
        }

        if (filter.ActorId is not null)
        {
            query = query.Where(entry => entry.ActorId == filter.ActorId);
        }

        if (filter.Operation is not null)
        {
            query = query.Where(entry => entry.Operation == filter.Operation);
        }

        if (filter.Permission is not null)
        {
            query = query.Where(entry => entry.Permission == filter.Permission);
        }

        if (filter.Outcome.HasValue)
        {
            string resultName = AdminAuditResults.ToWireName(filter.Outcome.Value);
            query = query.Where(entry => entry.Result == resultName);
        }

        if (filter.ErrorCode is not null)
        {
            query = query.Where(entry => entry.ErrorCode == filter.ErrorCode);
        }

        if (filter.FromUtc.HasValue)
        {
            query = query.Where(entry => entry.CreatedAtUtc >= filter.FromUtc.Value);
        }

        if (filter.ToUtc.HasValue)
        {
            query = query.Where(entry => entry.CreatedAtUtc < filter.ToUtc.Value);
        }

        if (cursor.HasValue)
        {
            DateTimeOffset createdAtUtc = cursor.Value.CreatedAtUtc;
            Guid id = cursor.Value.Id;
            query = query.Where(entry =>
                entry.CreatedAtUtc < createdAtUtc ||
                (entry.CreatedAtUtc == createdAtUtc && entry.Id.CompareTo(id) < 0));
        }

        var entries = await query
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .ThenByDescending(entry => entry.Id)
            .Take(take)
            .Select(entry => new
            {
                entry.Id,
                entry.ActorId,
                entry.TenantId,
                entry.Operation,
                entry.Permission,
                entry.Result,
                entry.ErrorCode,
                entry.CreatedAtUtc
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return entries
            .Select(entry => new AdministrationAuditEntryDetails(
                entry.Id,
                entry.ActorId,
                entry.TenantId,
                entry.Operation,
                entry.Permission,
                AdminAuditResults.Parse(entry.Result),
                entry.ErrorCode,
                entry.CreatedAtUtc))
            .ToArray();
    }

    public async Task<AdministrationAuditRetentionBatch> PurgeBeforeAsync(
        DateTimeOffset cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        Guid[] candidates = await dbContext.AuditEntries
            .AsNoTracking()
            .Where(entry => entry.CreatedAtUtc < cutoffUtc)
            .OrderBy(entry => entry.CreatedAtUtc)
            .ThenBy(entry => entry.Id)
            .Select(entry => entry.Id)
            .Take(batchSize + 1)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        bool hasMore = candidates.Length > batchSize;
        Guid[] idsToDelete = candidates.Take(batchSize).ToArray();
        if (idsToDelete.Length == 0)
        {
            return new AdministrationAuditRetentionBatch(0, HasMore: false);
        }

        int deletedCount = await dbContext.AuditEntries
            .Where(entry => idsToDelete.Contains(entry.Id))
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AdministrationAuditRetentionBatch(deletedCount, hasMore);
    }
}
