namespace Gma.Modules.Administration.Application.Handlers;

using Gma.Framework.Cqrs;
using Gma.Framework.Results;
using Gma.Modules.Administration.Application.Models;
using Gma.Modules.Administration.Application.Ports;
using Gma.Modules.Administration.Application.Queries;
using Microsoft.Extensions.Options;

internal sealed class ListAdministrationAuditEntriesQueryHandler(
    IAdministrationAuditRepository repository,
    IOptions<AdministrationAuditOptions> options)
    : IQueryHandler<ListAdministrationAuditEntriesQuery, AdministrationAuditPage>
{
    public async Task<Result<AdministrationAuditPage>> HandleAsync(
        ListAdministrationAuditEntriesQuery query,
        CancellationToken cancellationToken)
    {
        Result<AdministrationAuditFilter> filter = AdministrationAuditFilter.Create(
            query.TenantId,
            query.ActorId,
            query.Operation,
            query.Permission,
            query.Result,
            query.ErrorCode,
            query.FromUtc,
            query.ToUtc);
        if (filter.IsFailure)
        {
            return Result.Failure<AdministrationAuditPage>(filter.Error);
        }

        if (!AdministrationAuditCursorCodec.TryDecode(query.Cursor, out AdministrationAuditCursor? cursor))
        {
            return Result.Failure<AdministrationAuditPage>(
                AdministrationApplicationErrors.AuditCursorInvalid);
        }

        AdministrationAuditOptions configured = options.Value;
        int limit = Math.Clamp(
            query.Limit ?? configured.DefaultPageSize,
            1,
            configured.MaxPageSize);
        IReadOnlyList<AdministrationAuditEntryDetails> candidates = await repository.ListAsync(
            filter.Value,
            cursor,
            limit + 1,
            cancellationToken).ConfigureAwait(false);
        bool hasMore = candidates.Count > limit;
        AdministrationAuditEntryDetails[] items = candidates.Take(limit).ToArray();
        string? nextCursor = hasMore
            ? AdministrationAuditCursorCodec.Encode(new AdministrationAuditCursor(
                items[^1].CreatedAtUtc,
                items[^1].Id))
            : null;

        return Result.Success(new AdministrationAuditPage(items, nextCursor, hasMore, limit));
    }
}
