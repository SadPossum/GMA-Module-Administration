namespace Gma.Modules.Administration.Application.Ports;

using Gma.Modules.Administration.Application.Models;

internal interface IAdministrationAuditRepository
{
    Task<IReadOnlyList<AdministrationAuditEntryDetails>> ListAsync(
        AdministrationAuditFilter filter,
        AdministrationAuditCursor? cursor,
        int take,
        CancellationToken cancellationToken);

    Task<AdministrationAuditRetentionBatch> PurgeBeforeAsync(
        DateTimeOffset cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
